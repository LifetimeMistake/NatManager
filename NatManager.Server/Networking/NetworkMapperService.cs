using NatManager.Server.Configuration;
using NatManager.Server.Extensions;
using NatManager.Server.NAT;
using NatManager.Server.Networking.EventArgs;
using NatManager.Shared;
using NatManager.Shared.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.Server.Networking
{
    public class NetworkMapperService : IDaemonService
    {
        private const uint DEFAULT_HEARTBEAT_FREQUENCY = 10000;
        private const uint DEFAULT_DISCOVERY_FREQUENCY = 30000;
        private const uint DEFAULT_DEGREE_OF_PARALLELISM = 30;
        private const string CONFIG_KEY_HEARTBEAT_FREQUENCY = "natmanagerd.networkwatchdog.heartbeatFrequency";
        private const string CONFIG_KEY_DISCOVERY_FREQUENCY = "natmanagerd.networkwatchdog.discoveryFrequency";
        private const string CONFIG_KEY_DEGREE_OF_PARALLELISM = "natmanagerd.networkwatchdog.degreeOfParallelism";

        private uint heartbeatFrequency;
        private uint discoveryFrequency;
        private uint degreeOfParallelism;

        private IDaemon? daemon;
        private ServiceState serviceState;

        public IDaemon? Daemon { get { return daemon; } set { daemon = value; } }
        public ServiceState State { get { return serviceState; } }

        private CancellationTokenSource? serviceCancellationToken;
        private CancellationTokenSource? discoveryCancellationToken;
        private NatDeviceConnection? natDevice;
        private List<NetworkHost> aliveHosts = new List<NetworkHost>();
        private SemaphoreSlim natDeviceLock = new SemaphoreSlim(1, 1);
        private SemaphoreSlim aliveHostsLock = new SemaphoreSlim(1, 1);
        private Thread? watchdogThread;
        private Thread? mapperThread;

        public event EventHandler<NetworkHostFoundEventArgs>? NetworkHostFound;
        public event EventHandler<NetworkHostLostEventArgs>? NetworkHostLost;

        public NetworkMapperService(IDaemon daemon)
        {
            this.daemon = daemon;
            serviceState = ServiceState.Stopped;
        }

        public async Task StartAsync()
        {
            await StopAsync();
            ThrowIfNotReady(false);
            await RetrieveConfig();

            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            NatDiscovererService natDiscovererService = await daemon.GetServiceAsync<NatDiscovererService>();

            natDiscovererService.NatDeviceFound += NatDiscovererService_NatDeviceFound;
            natDiscovererService.NatDeviceLost += NatDiscovererService_NatDeviceLost;
            configManager.ConfigEntryUpdated += ConfigManager_ConfigEntryUpdated;

            try
            {
                await natDeviceLock.WaitAsync();
                natDevice = await natDiscovererService.GetCurrentNatDeviceAsync();
            }
            finally
            {
                natDeviceLock.Release();
            }

            serviceCancellationToken = new CancellationTokenSource();

            watchdogThread = new Thread(async () => await WatchdogJob(serviceCancellationToken.Token));
            watchdogThread.IsBackground = true;
            mapperThread = new Thread(async () => await MapperJob(serviceCancellationToken.Token));
            mapperThread.IsBackground = true;

            mapperThread.Start();
            watchdogThread.Start();

            serviceState = ServiceState.Running;
        }

        public async Task StopAsync()
        {
            serviceCancellationToken?.Cancel();
            serviceCancellationToken = null;

            discoveryCancellationToken?.Cancel();
            discoveryCancellationToken = null;

            watchdogThread = null;
            mapperThread = null;

            if(daemon != null)
            {
                ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
                NatDiscovererService natDiscovererService = await daemon.GetServiceAsync<NatDiscovererService>();

                natDiscovererService.NatDeviceFound -= NatDiscovererService_NatDeviceFound;
                natDiscovererService.NatDeviceLost -= NatDiscovererService_NatDeviceLost;
                configManager.ConfigEntryUpdated -= ConfigManager_ConfigEntryUpdated;
            }

            await ClearHostsListAsync(true);

            try
            {
                await natDeviceLock.WaitAsync();
                natDevice = null;
            }
            finally
            {
                natDeviceLock.Release();
            }

            serviceState = ServiceState.Stopped;
        }

        [MemberNotNull(nameof(daemon))]
        public void ThrowIfNotReady(bool checkState = true)
        {
            if (daemon == null)
                throw new InvalidOperationException("Requested service is not attached to a daemon instance");

            if (serviceState != ServiceState.Running && checkState)
                throw new InvalidOperationException("Requested service or it's dependency is not available");
        }

        private async Task RetrieveConfig()
        {
            ThrowIfNotReady(false);
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            if (!await configManager.ConfigEntryExistsAsync(CONFIG_KEY_HEARTBEAT_FREQUENCY))
            {
                await configManager.SetConfigValueAsync(CONFIG_KEY_HEARTBEAT_FREQUENCY, DEFAULT_HEARTBEAT_FREQUENCY);
                heartbeatFrequency = DEFAULT_HEARTBEAT_FREQUENCY;
            }
            else
            {
                heartbeatFrequency = await configManager.GetConfigValueUIntAsync(CONFIG_KEY_HEARTBEAT_FREQUENCY);
            }

            if (!await configManager.ConfigEntryExistsAsync(CONFIG_KEY_DISCOVERY_FREQUENCY))
            {
                await configManager.SetConfigValueAsync(CONFIG_KEY_DISCOVERY_FREQUENCY, DEFAULT_DISCOVERY_FREQUENCY);
                discoveryFrequency = DEFAULT_DISCOVERY_FREQUENCY;
            }
            else
            {
                discoveryFrequency = await configManager.GetConfigValueUIntAsync(CONFIG_KEY_DISCOVERY_FREQUENCY);
            }

            if(!await configManager.ConfigEntryExistsAsync(CONFIG_KEY_DEGREE_OF_PARALLELISM))
            {
                await configManager.SetConfigValueAsync(CONFIG_KEY_DEGREE_OF_PARALLELISM, DEFAULT_DEGREE_OF_PARALLELISM);
                degreeOfParallelism = DEFAULT_DEGREE_OF_PARALLELISM;
            }
            else
            {
                degreeOfParallelism = await configManager.GetConfigValueUIntAsync(CONFIG_KEY_DEGREE_OF_PARALLELISM);
            }
        }

        private async Task ClearHostsListAsync(bool announceEvents)
        {
            try
            {
                await aliveHostsLock.WaitAsync();
                if (announceEvents)
                {
                    foreach (NetworkHost host in aliveHosts)
                        NetworkHostLost?.Invoke(this, new NetworkHostLostEventArgs(host));
                }

                aliveHosts.Clear();
            }
            finally
            {
                aliveHostsLock.Release();
            }
        }

        private void NatDiscovererService_NatDeviceFound(object? sender, NAT.EventArgs.NatDeviceFoundEventArgs e)
        {
            discoveryCancellationToken?.Cancel();
            discoveryCancellationToken = null;

            Task.Run(async () =>
            {
                await ClearHostsListAsync(true);

                try
                {
                    await natDeviceLock.WaitAsync();
                    natDevice = e.NatDevice;
                }
                finally
                {
                    natDeviceLock.Release();
                }
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void NatDiscovererService_NatDeviceLost(object? sender, NAT.EventArgs.NatDeviceLostEventArgs e)
        {
            discoveryCancellationToken?.Cancel();
            discoveryCancellationToken = null;

            Task.Run(async () =>
            {
                await ClearHostsListAsync(true);

                try
                {
                    await natDeviceLock.WaitAsync();
                    natDevice = null;
                }
                finally
                {
                    natDeviceLock.Release();
                }
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void ConfigManager_ConfigEntryUpdated(object? sender, System.EventArgs e)
        {
            RetrieveConfig().FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private async Task WatchdogJob(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if(natDevice != null)
                        await VerifyHostsAlive(cancellationToken);
                }
                catch (Exception ex)
                {
                    if (daemon == null)
                        return;

                    daemon.GetErrorHandler().HandleException(ex);
                }

                await Task.Delay((int)heartbeatFrequency);
            }
        }

        private async Task VerifyHostsAlive(CancellationToken cancellationToken)
        {
            ConcurrentQueue<NetworkHost> lostHosts = new ConcurrentQueue<NetworkHost>();
            NetworkInterface? hostInterfaceInformation;
            try
            {
                await natDeviceLock.WaitAsync();
                if (natDevice == null)
                    throw new NullReferenceException(nameof(natDevice));

                hostInterfaceInformation = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(i => i.GetIPProperties().UnicastAddresses
                    .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Any(a => a.Address.Equals(natDevice.GetClientInternalIP())));
            }
            catch(Exception ex)
            {
                hostInterfaceInformation = null;
                daemon?.GetErrorHandler().HandleException(ex);
            }
            finally
            {
                natDeviceLock.Release();
            }

            try
            {
                await aliveHostsLock.WaitAsync();
                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = (int)degreeOfParallelism;
                parallelOptions.CancellationToken = cancellationToken;

                await Parallel.ForEachAsync(aliveHosts, parallelOptions, async (host, token) =>
                {
                    PhysicalAddress? physicalAddress;
                    if (hostInterfaceInformation != null && hostInterfaceInformation.GetIPProperties().UnicastAddresses.Any(a => a.Address.Equals(host.IPAddress)))
                    {
                        physicalAddress = hostInterfaceInformation.GetPhysicalAddress();
                    }
                    else
                    {
                        if (!await NetworkInfoProvider.HostUpAsync(host.IPAddress))
                        {
                            lostHosts.Enqueue(host);
                            return;
                        }
                        physicalAddress = await NetworkInfoProvider.ResolveMACAddressFromIPAsync(host.IPAddress);
                    }

                    if (physicalAddress != null && !PhysicalAddressHelper.AddressEqual(physicalAddress, host.PhysicalAddress))
                    {
                        lostHosts.Enqueue(host);
                        return;
                    } 
                });

                while (lostHosts.TryDequeue(out NetworkHost? host))
                {
                    aliveHosts.Remove(host);
                    NetworkHostLost?.Invoke(this, new NetworkHostLostEventArgs(host));
                }
            }
            finally
            {
                aliveHostsLock.Release();
            }
        }

        private async Task MapperJob(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (natDevice != null)
                        await DiscoverNetworkHosts(cancellationToken);
                }
                catch (Exception ex)
                {
                    if (daemon == null)
                        return;

                    daemon.GetErrorHandler().HandleException(ex);
                }

                await Task.Delay((int)discoveryFrequency);
            }
        }

        private async Task DiscoverNetworkHosts(CancellationToken cancellationToken)
        {
            IPAddress? subnetMask, startIP, endIP;
            NetworkInterface? hostInterfaceInformation;
            try
            {
                await natDeviceLock.WaitAsync();
                if (natDevice == null)
                    throw new NullReferenceException(nameof(natDevice));

                subnetMask = NetworkInfoProvider.GetSubnetMaskFromInterfaceAddress(natDevice.GetClientInternalIP());
                if (subnetMask == null)
                    throw new NullReferenceException("Could not find the network interface the NAT device is on.");

                hostInterfaceInformation = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(i => i.GetIPProperties().UnicastAddresses
                    .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Any(a => a.Address.Equals(natDevice.GetClientInternalIP())));

                if (hostInterfaceInformation == null)
                    throw new NullReferenceException("Could not find the network interface the NAT device is on.");

                startIP = NetworkInfoProvider.GetNetworkAddress(natDevice.GetInternalIP(), subnetMask);
                endIP = NetworkInfoProvider.GetBroadcastAddress(natDevice.GetInternalIP(), subnetMask);
            }
            finally
            {
                natDeviceLock.Release();
            }

            CancellationTokenSource discoveryToken = new CancellationTokenSource();
            discoveryCancellationToken = discoveryToken;

            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = (int)degreeOfParallelism;
            parallelOptions.CancellationToken = discoveryToken.Token;
            await Parallel.ForEachAsync(NetworkInfoProvider.EnumerateIPRange(startIP, endIP), parallelOptions, async (address, token) =>
            {
                NetworkHost host;

                if (hostInterfaceInformation.GetIPProperties().UnicastAddresses.Any(a => a.Address.Equals(address)))
                {
                    PhysicalAddress physicalAddress = hostInterfaceInformation.GetPhysicalAddress();
                    string? hostname = await NetworkInfoProvider.GetDeviceHostnameAsync(address);
                    host = new NetworkHost(address, physicalAddress, hostname);
                }
                else
                {
                    if (!await NetworkInfoProvider.HostUpAsync(address))
                        return;

                    PhysicalAddress? physicalAddress = await NetworkInfoProvider.ResolveMACAddressFromIPAsync(address);
                    if (physicalAddress == null)
                        return;

                    string? hostname = await NetworkInfoProvider.GetDeviceHostnameAsync(address);
                    host = new NetworkHost(address, physicalAddress, hostname);
                }

                try
                {
                    await aliveHostsLock.WaitAsync();
                    if (aliveHosts.Any(h => PhysicalAddressHelper.AddressEqual(h.PhysicalAddress, host.PhysicalAddress)))
                        return;

                    aliveHosts.Add(host);
                }
                finally
                {
                    aliveHostsLock.Release();
                }

                NetworkHostFound?.Invoke(this, new NetworkHostFoundEventArgs(host));
            });

            discoveryCancellationToken = null;
        }

        public async Task<NetworkHost[]> GetAliveHostsAsync()
        {
            if (serviceState != ServiceState.Running)
                throw new InvalidOperationException("Requested service or it's dependency is not available");

            try
            {
                await aliveHostsLock.WaitAsync();
                NetworkHost[] hosts = new NetworkHost[aliveHosts.Count];
                aliveHosts.CopyTo(hosts);
                return hosts;
            }
            finally
            {
                aliveHostsLock.Release();
            }
        }

        public async Task<NetworkHost?> GetHostByMAC(PhysicalAddress physicalAddress)
        {
            if (serviceState != ServiceState.Running)
                throw new InvalidOperationException("Requested service or it's dependency is not available");

            if (physicalAddress == null)
                throw new ArgumentNullException(nameof(physicalAddress));

            try
            {
                await aliveHostsLock.WaitAsync();
                return aliveHosts.FirstOrDefault(h => PhysicalAddressHelper.AddressEqual(h.PhysicalAddress, physicalAddress));

            }
            finally
            {
                aliveHostsLock.Release();
            }
        }

        public async Task<NetworkHost?> GetHostByIP(IPAddress address)
        {
            if (serviceState != ServiceState.Running)
                throw new InvalidOperationException("Requested service or it's dependency is not available");

            if (address == null)
                throw new ArgumentNullException(nameof(address));

            try
            {
                await aliveHostsLock.WaitAsync();
                return aliveHosts.FirstOrDefault(h => h.IPAddress.Equals(address));
            }
            finally
            {
                aliveHostsLock.Release();
            }
        }
    }
}
