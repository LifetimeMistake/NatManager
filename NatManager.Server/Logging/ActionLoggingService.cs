using NatManager.Server.Extensions;
using NatManager.Server.NAT;
using NatManager.Server.Networking;
using NatManager.Server.PortMapping;
using NatManager.Server.RPC;
using NatManager.Server.Users;
using NatManager.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.Logging
{
    public class ActionLoggingService : IDaemonService
    {
        private IDaemon? daemon;
        private ServiceState serviceState;

        public IDaemon? Daemon { get { return daemon; } set { daemon = value; } }
        public ServiceState State { get { return serviceState; } }

        public ActionLoggingService(IDaemon daemon)
        {
            this.Daemon = daemon;
            this.serviceState = ServiceState.Stopped;
        }

        public async Task StartAsync()
        {
            await StopAsync();
            await RegisterUserManager();
            await RegisterMappingManager();
            await RegisterNatDiscovery();
            await RegisterNetworkMapper();
            await RegisterRpcServer();
            serviceState = ServiceState.Running;
        }

        public async Task StopAsync()
        {
            await UnregisterUserManager();
            await UnregisterMappingManager();
            await UnregisterNatDiscovery();
            await UnregisterNetworkMapper();
            await UnregisterRpcServer();
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

        private async Task RegisterUserManager()
        {
            if (daemon == null)
                return;

            if (!await daemon.HasServiceAsync<UserManager>())
                return;

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            userManager.UserCreated += UserManager_UserCreated;
            userManager.UserUpdated += UserManager_UserUpdated;
            userManager.UserCredentialsUpdated += UserManager_UserCredentialsUpdated;
            userManager.UserDeleted += UserManager_UserDeleted;
        }

        private async Task UnregisterUserManager()
        {
            if (daemon == null)
                return;

            if (!await daemon.HasServiceAsync<UserManager>())
                return;

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            userManager.UserCreated -= UserManager_UserCreated;
            userManager.UserUpdated -= UserManager_UserUpdated;
            userManager.UserCredentialsUpdated -= UserManager_UserCredentialsUpdated;
            userManager.UserDeleted -= UserManager_UserDeleted;
        }

        private void UserManager_UserCreated(object? sender, Users.EventArgs.UserCreatedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                string caller = (e.CallerSubject.HasValue) ? "by " + e.CallerSubject.Value.ToString() : "";
                await daemon.GetLogger().InfoAsync($"User {e.CreatedUser.Id} has been created {caller}");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void UserManager_UserUpdated(object? sender, Users.EventArgs.UserUpdatedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                string caller = (e.CallerSubject.HasValue) ? "by " + e.CallerSubject.Value.ToString() : "";
                await daemon.GetLogger().InfoAsync($"User {e.OldUser.Id} has been updated {caller}");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void UserManager_UserCredentialsUpdated(object? sender, Users.EventArgs.UserCredentialsUpdatedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                string caller = (e.CallerSubject.HasValue) ? "by " + e.CallerSubject.Value.ToString() : "";
                await daemon.GetLogger().InfoAsync($"User's {e.UpdatedUser.Id} credentials have been updated {caller}");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void UserManager_UserDeleted(object? sender, Users.EventArgs.UserDeletedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                string caller = (e.CallerSubject.HasValue) ? "by " + e.CallerSubject.Value.ToString() : "";
                await daemon.GetLogger().InfoAsync($"User {e.DeletedUser.Id} has been deleted {caller}");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private async Task RegisterMappingManager()
        {
            if (daemon == null)
                return;

            if (!await daemon.HasServiceAsync<MappingManager>())
                return;

            MappingManager mappingManager = await daemon.GetServiceAsync<MappingManager>();
            mappingManager.MappingCreated += MappingManager_MappingCreated;
            mappingManager.MappingUpdated += MappingManager_MappingUpdated;
            mappingManager.MappingDeleted += MappingManager_MappingDeleted;
        }

        private async Task UnregisterMappingManager()
        {
            if (daemon == null)
                return;

            if (!await daemon.HasServiceAsync<MappingManager>())
                return;

            MappingManager mappingManager = await daemon.GetServiceAsync<MappingManager>();
            mappingManager.MappingCreated -= MappingManager_MappingCreated;
            mappingManager.MappingUpdated -= MappingManager_MappingUpdated;
            mappingManager.MappingDeleted -= MappingManager_MappingDeleted;
        }

        private void MappingManager_MappingCreated(object? sender, PortMapping.EventArgs.MappingCreatedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                string caller = (e.CallerSubject.HasValue) ? "by " + e.CallerSubject.Value.ToString() : "";
                await daemon.GetLogger().InfoAsync($"Mapping {e.Mapping.Id} (port {e.Mapping.PublicPort}) has been created {caller}");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void MappingManager_MappingUpdated(object? sender, PortMapping.EventArgs.MappingUpdatedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                string caller = (e.CallerSubject.HasValue) ? "by " + e.CallerSubject.Value.ToString() : "";
                await daemon.GetLogger().InfoAsync($"Mapping {e.OldMapping.Id} (port {e.OldMapping.PublicPort} -> {e.UpdatedMapping.PublicPort}) has been updated {caller}");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void MappingManager_MappingDeleted(object? sender, PortMapping.EventArgs.MappingDeletedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                string caller = (e.CallerSubject.HasValue) ? "by " + e.CallerSubject.Value.ToString() : "";
                await daemon.GetLogger().InfoAsync($"Mapping {e.Mapping.Id} (port {e.Mapping.PublicPort}) has been deleted {caller}");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private async Task RegisterNatDiscovery()
        {
            if (daemon == null)
                return;

            if (!await daemon.HasServiceAsync<NatDiscovererService>())
                return;

            NatDiscovererService natDiscovererService = await daemon.GetServiceAsync<NatDiscovererService>();
            natDiscovererService.NatDeviceFound += NatDiscovererService_NatDeviceFound;
            natDiscovererService.NatDeviceLost += NatDiscovererService_NatDeviceLost;
        }

        private async Task UnregisterNatDiscovery()
        {
            if (daemon == null)
                return;

            if (!await daemon.HasServiceAsync<NatDiscovererService>())
                return;

            NatDiscovererService natDiscovererService = await daemon.GetServiceAsync<NatDiscovererService>();
            natDiscovererService.NatDeviceFound -= NatDiscovererService_NatDeviceFound;
            natDiscovererService.NatDeviceLost -= NatDiscovererService_NatDeviceLost;
        }

        private void NatDiscovererService_NatDeviceFound(object? sender, NAT.EventArgs.NatDeviceFoundEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                await daemon.GetLogger().InfoAsync($"NAT device discovered: {e.NatDevice.GetInternalIP()}");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void NatDiscovererService_NatDeviceLost(object? sender, NAT.EventArgs.NatDeviceLostEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                await daemon.GetLogger().InfoAsync($"NAT device lost: {e.NatDevice.GetInternalIP()}");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private async Task RegisterNetworkMapper()
        {
            if (daemon == null)
                return;

            if (!await daemon.HasServiceAsync<NetworkMapperService>())
                return;

            NetworkMapperService networkMapperService = await daemon.GetServiceAsync<NetworkMapperService>();
            networkMapperService.NetworkHostFound += NetworkMapperService_NetworkHostFound;
            networkMapperService.NetworkHostLost += NetworkMapperService_NetworkHostLost;
        }

        private async Task UnregisterNetworkMapper()
        {
            if (daemon == null)
                return;

            if (!await daemon.HasServiceAsync<NetworkMapperService>())
                return;

            NetworkMapperService networkMapperService = await daemon.GetServiceAsync<NetworkMapperService>();
            networkMapperService.NetworkHostFound -= NetworkMapperService_NetworkHostFound;
            networkMapperService.NetworkHostLost -= NetworkMapperService_NetworkHostLost;
        }

        private void NetworkMapperService_NetworkHostFound(object? sender, Networking.EventArgs.NetworkHostFoundEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                await daemon.GetLogger().InfoAsync($"Network host found: {e.NetworkHost.PhysicalAddress} ({e.NetworkHost.IPAddress})");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void NetworkMapperService_NetworkHostLost(object? sender, Networking.EventArgs.NetworkHostLostEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                await daemon.GetLogger().InfoAsync($"Network host lost: {e.NetworkHost.PhysicalAddress} ({e.NetworkHost.IPAddress})");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private async Task RegisterRpcServer()
        {
            if (daemon == null)
                return;

            if (!await daemon.HasServiceAsync<AutoTcpRpcServer>())
                return;

            AutoTcpRpcServer tcpRpcServer = await daemon.GetServiceAsync<AutoTcpRpcServer>();
            tcpRpcServer.SessionCreated += TcpRpcServer_SessionCreated;
            tcpRpcServer.SessionIdentityChanged += TcpRpcServer_SessionIdentityChanged;
            tcpRpcServer.SessionDestroyed += TcpRpcServer_SessionDestroyed;
        }

        private async Task UnregisterRpcServer()
        {
            if (daemon == null)
                return;

            if (!await daemon.HasServiceAsync<AutoTcpRpcServer>())
                return;

            AutoTcpRpcServer tcpRpcServer = await daemon.GetServiceAsync<AutoTcpRpcServer>();
            tcpRpcServer.SessionCreated -= TcpRpcServer_SessionCreated;
            tcpRpcServer.SessionIdentityChanged -= TcpRpcServer_SessionIdentityChanged;
            tcpRpcServer.SessionDestroyed -= TcpRpcServer_SessionDestroyed;
        }

        private void TcpRpcServer_SessionCreated(object? sender, RPC.EventArgs.SessionEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                await daemon.GetLogger().InfoAsync($"User RPC session created: {e.SessionContext.Id}");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void TcpRpcServer_SessionIdentityChanged(object? sender, RPC.EventArgs.SessionEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                string identityId = (e.SessionContext.Identity != null) ? e.SessionContext.Identity.Id.ToString() : "Null identity";
                await daemon.GetLogger().InfoAsync($"User RPC session {e.SessionContext.Id} identity changed: {identityId}");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private void TcpRpcServer_SessionDestroyed(object? sender, RPC.EventArgs.SessionEventArgs e)
        {
            Task.Run(async () =>
            {
                if (daemon == null)
                    return;

                await daemon.GetLogger().InfoAsync($"User RPC session closed: {e.SessionContext.Id}");
            }).FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }
    }
}
