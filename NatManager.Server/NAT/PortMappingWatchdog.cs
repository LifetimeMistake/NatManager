using NatManager.Server.Configuration;
using NatManager.Server.Extensions;
using NatManager.Server.Networking;
using NatManager.Server.PortMapping;
using NatManager.Server.Users;
using NatManager.Shared;
using NatManager.Shared.Networking;
using NatManager.Shared.PortMapping;
using Open.Nat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.Server.NAT
{
    public class PortMappingWatchdog : IDaemonService
    {
        private const uint DEFAULT_UPDATE_FREQUENCY = 10000;
        private const string CONFIG_KEY_UPDATE_FREQUENCY = "natmanagerd.mappingwatchdog.updateFrequency";
        private uint updateFrequency;

        private IDaemon? daemon;
        private ServiceState serviceState;

        private CancellationTokenSource? serviceCancellationToken;
        private readonly Dictionary<Guid, ManagedMapping> managedMappings = new Dictionary<Guid, ManagedMapping>();
        private SemaphoreSlim managedMappingsLock = new SemaphoreSlim(1, 1);
        private readonly List<NetworkHost> aliveHosts = new List<NetworkHost>();
        private SemaphoreSlim aliveHostsLock = new SemaphoreSlim(1, 1);
        private NatDeviceConnection? natDevice;
        private SemaphoreSlim natDeviceLock = new SemaphoreSlim(1, 1);
        private Thread? watchdogThread;

         public IDaemon? Daemon { get { return daemon; } set { daemon = value; } }
        public ServiceState State { get { return serviceState; } }

        public PortMappingWatchdog(IDaemon daemon)
        {
            this.daemon = daemon;
            serviceState = ServiceState.Stopped;
        }

        public async Task StartAsync()
        {
            await StopAsync();
            ThrowIfNotReady(false);
            await RetrieveConfig();

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            MappingManager mappingManager = await daemon.GetServiceAsync<MappingManager>();
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            NetworkMapperService networkMapperService = await daemon.GetServiceAsync<NetworkMapperService>();
            NatDiscovererService natDiscovererService = await daemon.GetServiceAsync<NatDiscovererService>();

            try
            {
                await managedMappingsLock.WaitAsync();
                foreach (ManagedMapping mapping in await mappingManager.GetAllMappingsAsync(userManager.RootUserId))
                    managedMappings.Add(mapping.Id, mapping);
            }
            finally
            {
                managedMappingsLock.Release();
            }

            try
            {
                await aliveHostsLock.WaitAsync();
                aliveHosts.AddRange(await networkMapperService.GetAliveHostsAsync());
            }
            finally
            {
                aliveHostsLock.Release();
            }

            try
            {
                await natDeviceLock.WaitAsync();
                natDevice = await natDiscovererService.GetCurrentNatDeviceAsync();
            }
            finally
            {
                natDeviceLock.Release();
            }

            natDiscovererService.NatDeviceFound += NatDiscovererService_NatDeviceFound;
            natDiscovererService.NatDeviceLost += NatDiscovererService_NatDeviceLost;
            networkMapperService.NetworkHostFound += NetworkMapperService_NetworkHostFound;
            networkMapperService.NetworkHostLost += NetworkMapperService_NetworkHostLost;
            mappingManager.MappingCreated += MappingManager_MappingCreated;
            mappingManager.MappingUpdated += MappingManager_MappingUpdated;
            mappingManager.MappingDeleted += MappingManager_MappingDeleted;
            configManager.ConfigEntryUpdated += ConfigManager_ConfigEntryUpdated;

            serviceCancellationToken = new CancellationTokenSource();
            watchdogThread = new Thread(async () => await WatchdogJob(serviceCancellationToken.Token));
            watchdogThread.IsBackground = true;
            watchdogThread.Start();

            serviceState = ServiceState.Running;
        }

        public async Task StopAsync()
        {
            serviceCancellationToken?.Cancel();
            serviceCancellationToken = null;
            watchdogThread = null;

            if (daemon != null)
            {
                MappingManager mappingManager = await daemon.GetServiceAsync<MappingManager>();
                ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
                NetworkMapperService networkMapperService = await daemon.GetServiceAsync<NetworkMapperService>();
                NatDiscovererService natDiscovererService = await daemon.GetServiceAsync<NatDiscovererService>();

                natDiscovererService.NatDeviceFound -= NatDiscovererService_NatDeviceFound;
                natDiscovererService.NatDeviceLost -= NatDiscovererService_NatDeviceLost;
                networkMapperService.NetworkHostFound -= NetworkMapperService_NetworkHostFound;
                networkMapperService.NetworkHostLost -= NetworkMapperService_NetworkHostLost;
                mappingManager.MappingCreated -= MappingManager_MappingCreated;
                mappingManager.MappingUpdated -= MappingManager_MappingUpdated;
                mappingManager.MappingDeleted -= MappingManager_MappingDeleted;
                configManager.ConfigEntryUpdated -= ConfigManager_ConfigEntryUpdated;
            }

            try
            {
                await managedMappingsLock.WaitAsync();
                managedMappings.Clear();
            }
            finally
            {
                managedMappingsLock.Release();
            }

            try
            {
                await aliveHostsLock.WaitAsync();
                aliveHosts.Clear();
            }
            finally
            {
                aliveHostsLock.Release();
            }

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
            if (!await configManager.ConfigEntryExistsAsync(CONFIG_KEY_UPDATE_FREQUENCY))
            {
                await configManager.SetConfigValueAsync(CONFIG_KEY_UPDATE_FREQUENCY, DEFAULT_UPDATE_FREQUENCY);
                updateFrequency = DEFAULT_UPDATE_FREQUENCY;
            }
            else
            {
                updateFrequency = await configManager.GetConfigValueUIntAsync(CONFIG_KEY_UPDATE_FREQUENCY);
            }
        }

        private void NatDiscovererService_NatDeviceFound(object? sender, EventArgs.NatDeviceFoundEventArgs e)
        {
            Task.Run(() =>
            {
                natDevice = e.NatDevice;
            }).ExecuteWithinLock(natDeviceLock).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void NatDiscovererService_NatDeviceLost(object? sender, EventArgs.NatDeviceLostEventArgs e)
        {
            Task.Run(() =>
            {
                natDevice = null;
            }).ExecuteWithinLock(natDeviceLock).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void NetworkMapperService_NetworkHostFound(object? sender, Networking.EventArgs.NetworkHostFoundEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    await aliveHostsLock.WaitAsync();
                    if (!aliveHosts.Any(h => PhysicalAddressHelper.AddressEqual(h.PhysicalAddress, e.NetworkHost.PhysicalAddress)))
                        aliveHosts.Add(e.NetworkHost);
                }
                finally
                {
                    aliveHostsLock.Release();
                }
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void NetworkMapperService_NetworkHostLost(object? sender, Networking.EventArgs.NetworkHostLostEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    await aliveHostsLock.WaitAsync();
                    aliveHosts.RemoveAll(h => PhysicalAddressHelper.AddressEqual(h.PhysicalAddress, e.NetworkHost.PhysicalAddress));
                }
                finally
                {
                    aliveHostsLock.Release();
                }
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void MappingManager_MappingCreated(object? sender, PortMapping.EventArgs.MappingCreatedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    await managedMappingsLock.WaitAsync();
                    if (!managedMappings.ContainsKey(e.Mapping.Id))
                        managedMappings.Add(e.Mapping.Id, e.Mapping);
                }
                finally
                {
                    managedMappingsLock.Release();
                }
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void MappingManager_MappingUpdated(object? sender, PortMapping.EventArgs.MappingUpdatedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    await managedMappingsLock.WaitAsync();
                    if (managedMappings.Remove(e.OldMapping.Id))
                        managedMappings.Add(e.UpdatedMapping.Id, e.UpdatedMapping);
                }
                finally
                {
                    managedMappingsLock.Release();
                }
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void MappingManager_MappingDeleted(object? sender, PortMapping.EventArgs.MappingDeletedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    await managedMappingsLock.WaitAsync();
                    managedMappings.Remove(e.Mapping.Id);
                }
                finally
                {
                    managedMappingsLock.Release();
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
                    await RemoveInvalidMappings();
                    await CreateEnabledMappings();
                }
                catch (Exception ex)
                {
                    if (daemon == null)
                        return;

                    daemon.GetErrorHandler().HandleException(ex);
                }

                await Task.Delay((int)updateFrequency);
            }
        }

        private async Task RemoveInvalidMappings()
        {
            IEnumerable<Mapping> unmanagedMappings;

            try
            {
                await natDeviceLock.WaitAsync();
                if (natDevice == null)
                    return;

                unmanagedMappings = await natDevice.GetMappingsAsync();
            }
            finally
            {
                natDeviceLock.Release();
            }

            foreach (Mapping unmanagedMapping in unmanagedMappings)
            {
                if (!Guid.TryParse(unmanagedMapping.Description, out Guid managedMappingId))
                {
                    // Unknown mapping Id, delete if Enforcing
                    if (daemon == null)
                        throw new Exception("Service was detached from the current daemon unexpectedly");

                    bool enforcing = daemon.BehaviourMode == BehaviourMode.Enforcing;
                    if (enforcing)
                    {
                        await daemon.GetLogger().InfoAsync("Removing unmanaged mapping");
                        await natDevice.DeletePortMappingAsync(unmanagedMapping).ExecuteWithinLock(natDeviceLock);
                    }

                    continue;
                }

                try
                {
                    await managedMappingsLock.WaitAsync();
                    if (!managedMappings.ContainsKey(managedMappingId))
                    {
                        // Orphaned managed mapping already deleted from the database
                        daemon?.GetLogger().InfoAsync("Removing orphaned mapping");
                        await natDevice.DeletePortMappingAsync(unmanagedMapping).ExecuteWithinLock(natDeviceLock);
                        continue;
                    }

                    ManagedMapping managedMapping = managedMappings[managedMappingId];
                    if (!managedMapping.Enabled)
                    {
                        // Managed mapping has been disabled
                        daemon?.GetLogger().InfoAsync("Removing expired mapping (mapping disabled)");
                        await natDevice.DeletePortMappingAsync(unmanagedMapping).ExecuteWithinLock(natDeviceLock);
                        continue;
                    }

                    if (!MappingsEqual(managedMapping, unmanagedMapping) || unmanagedMapping.IsExpired())
                    {
                        // Managed mapping has been updated/unmanaged mapping has expired
                        daemon?.GetLogger().InfoAsync("Removing expired mapping (mapping modified)");
                        await natDevice.DeletePortMappingAsync(unmanagedMapping).ExecuteWithinLock(natDeviceLock);
                        continue;
                    }

                    try
                    {
                        await aliveHostsLock.WaitAsync();
                        NetworkHost? managedMappingTargetHost = aliveHosts.FirstOrDefault(h => h.IPAddress.Equals(unmanagedMapping.PrivateIP));

                        if (managedMappingTargetHost == null)
                        {
                            // Target host is down
                            daemon?.GetLogger().InfoAsync("Removing expired mapping (target host down)");
                            await natDevice.DeletePortMappingAsync(unmanagedMapping).ExecuteWithinLock(natDeviceLock);
                        }
                        else if (!PhysicalAddressHelper.AddressEqual(managedMappingTargetHost.PhysicalAddress, managedMapping.PrivateMAC))
                        {
                            // Managed mapping is targeting the wrong host (ie. due to DHCP lending the host a different address)
                            daemon?.GetLogger().InfoAsync("Removing expired mapping (DHCP lease expired)");
                            await natDevice.DeletePortMappingAsync(unmanagedMapping).ExecuteWithinLock(natDeviceLock);
                        }
                    }
                    finally
                    {
                        aliveHostsLock.Release();
                    }
                }
                finally
                {
                    managedMappingsLock.Release();
                }
            }
        }

        private async Task CreateEnabledMappings()
        {
            IEnumerable<Mapping> unmanagedMappings;

            try
            {
                await natDeviceLock.WaitAsync();
                if (natDevice == null)
                    return;

                unmanagedMappings = await natDevice.GetMappingsAsync();
            }
            finally
            {
                natDeviceLock.Release();
            }

            try
            {
                await managedMappingsLock.WaitAsync();
                foreach (KeyValuePair<Guid, ManagedMapping> kvp in managedMappings.Where(m => m.Value.Enabled))
                {
                    ManagedMapping managedMapping = kvp.Value;
                    NetworkHost? targetHost;

                    try
                    {
                        await aliveHostsLock.WaitAsync();
                        targetHost = aliveHosts.FirstOrDefault(h => PhysicalAddressHelper.AddressEqual(h.PhysicalAddress.GetAddressBytes(), managedMapping.PrivateMAC.GetAddressBytes()));
                        // Target host is currently down, so we don't enable the mapping.

                        if (targetHost == null)
                        {
#if DEBUG
                            daemon?.GetLogger().DebugAsync($"Skipping managed mapping {managedMapping.Id}, target host is down.");
#endif
                            continue;
                        }
                    }
                    finally
                    {
                        aliveHostsLock.Release();
                    }

                    if (!unmanagedMappings.Any(m => MappingsEqual(kvp.Value, m)))
                    {
                        // This mapping doesn't exist but it should, add it.
                        daemon?.GetLogger().InfoAsync($"Adding managed mapping {managedMapping.Id}");
                        Mapping mapping = new Mapping((Open.Nat.Protocol)managedMapping.Protocol, targetHost.IPAddress, managedMapping.PrivatePort, managedMapping.PublicPort, 0, managedMapping.Id.ToString());
                        await natDevice.CreatePortMappingAsync(mapping).ExecuteWithinLock(natDeviceLock);
                    }
                }
            }
            finally
            {
                managedMappingsLock.Release();
            }
        }

        private bool MappingsEqual(ManagedMapping managedMapping, Mapping unmanagedMapping)
        {
            if (managedMapping == null)
                throw new ArgumentNullException(nameof(managedMapping));

            if (unmanagedMapping == null)
                throw new ArgumentNullException(nameof(unmanagedMapping));

            return (managedMapping.PublicPort == unmanagedMapping.PublicPort && managedMapping.PrivatePort == unmanagedMapping.PrivatePort
                && managedMapping.Protocol == (NatManager.Shared.PortMapping.Protocol)unmanagedMapping.Protocol);
        }
    }
}
