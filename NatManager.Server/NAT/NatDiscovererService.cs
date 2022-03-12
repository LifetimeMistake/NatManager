using NatManager.Server.Configuration;
using NatManager.Server.Extensions;
using NatManager.Server.NAT;
using NatManager.Server.NAT.EventArgs;
using NatManager.Server.Networking;
using NatManager.Shared;
using Open.Nat;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.Server.NAT
{
    public class NatDiscovererService : IDaemonService
    {
        private const uint DEFAULT_HEARTBEAT_FREQUENCY = 3000;
        private const uint DEFAULT_DISCOVERY_TIMEOUT = 5000;
        private const string CONFIG_KEY_HEARTBEAT_FREQUENCY = "natmanagerd.natwatchdog.heartbeatFrequency";
        private const string CONFIG_KEY_DISCOVERY_TIMEOUT = "natmanagerd.natwatchdog.discoveryTimeout";
        private uint heartbeatFrequency;
        private uint discoveryTimeout;

        private IDaemon? daemon;
        private ServiceState serviceState;

        private NatDeviceConnection? natDevice;
        private SemaphoreSlim natDeviceLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource? serviceCancellationToken;
        private Thread? mapperThread;

         public IDaemon? Daemon { get { return daemon; } set { daemon = value; } }
        public ServiceState State { get { return serviceState; } }

        public event EventHandler<NatDeviceFoundEventArgs>? NatDeviceFound;
        public event EventHandler<NatDeviceLostEventArgs>? NatDeviceLost;

        public NatDiscovererService(IDaemon daemon)
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
            configManager.ConfigEntryUpdated += ConfigManager_ConfigEntryUpdated;
            serviceCancellationToken = new CancellationTokenSource();

            mapperThread = new Thread(async () => await WatchdogJob(serviceCancellationToken.Token));
            mapperThread.IsBackground = true;
            mapperThread.Start();

            serviceState = ServiceState.Running;
        }

        public async Task StopAsync()
        {
            serviceCancellationToken?.Cancel();
            serviceCancellationToken = null;
            mapperThread = null;

            if(daemon != null)
            {
                ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
                configManager.ConfigEntryUpdated -= ConfigManager_ConfigEntryUpdated;
            }

            await natDeviceLock.WaitAsync();
            natDevice = null;
            natDeviceLock.Release();

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

        private void ConfigManager_ConfigEntryUpdated(object? sender, System.EventArgs e)
        {
            RetrieveConfig().FireAndForgetSafeAsync(daemon?.GetErrorHandler());
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

            if (!await configManager.ConfigEntryExistsAsync(CONFIG_KEY_DISCOVERY_TIMEOUT))
            {
                await configManager.SetConfigValueAsync(CONFIG_KEY_DISCOVERY_TIMEOUT, DEFAULT_DISCOVERY_TIMEOUT);
                discoveryTimeout = DEFAULT_DISCOVERY_TIMEOUT;
            }
            else
            {
                discoveryTimeout = await configManager.GetConfigValueUIntAsync(CONFIG_KEY_DISCOVERY_TIMEOUT);
            }
        }

        private async Task WatchdogJob(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if(!await NatDeviceResponsiveAsync())
                    {
                        if(natDevice != null)
                        {
                            NatDeviceLost?.Invoke(this, new NatDeviceLostEventArgs(natDevice));
                            await natDeviceLock.WaitAsync();
                            natDevice = null;
                            natDeviceLock.Release();
                        }

                        NatDeviceConnection? discoveredNatDevice = await NatDeviceConnection.DiscoverDeviceAsync(PortMapper.Upnp, (int)discoveryTimeout);

                        if (discoveredNatDevice != null)
                        {
                            NatDeviceFound?.Invoke(this, new NatDeviceFoundEventArgs(discoveredNatDevice));
                            await natDeviceLock.WaitAsync();
                            natDevice = discoveredNatDevice;
                            natDeviceLock.Release();
                        }
                    }
                }
                catch(Exception ex)
                {
                    daemon?.GetErrorHandler().HandleException(ex);
                }

                await Task.Delay((int)heartbeatFrequency);
            }
        }

        public async Task<NatDeviceConnection?> GetCurrentNatDeviceAsync()
        {
            try
            {
                await natDeviceLock.WaitAsync();
                return natDevice;
            }
            finally
            {
                natDeviceLock.Release();
            }
        }

        public async Task<bool> NatDeviceResponsiveAsync()
        {
            NatDeviceConnection natDevice;
            try
            {
                await natDeviceLock.WaitAsync();
                if (this.natDevice == null)
                    return false;

                natDevice = this.natDevice;
            }
            finally
            {
                natDeviceLock.Release();
            }

            return await NetworkInfoProvider.HostUpAsync(natDevice.GetInternalIP());
        }
    }
}
