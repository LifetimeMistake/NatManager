using NatManager.Server.Users;
using NatManager.Shared;
using NatManager.Shared.Exceptions;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server
{
    public class DaemonManager : IDaemonService
    {
        private IDaemon? daemon;
        private ServiceState serviceState;

        public IDaemon? Daemon { get { return daemon; } set { daemon = value; } }
        public ServiceState State { get { return serviceState; } }

        public DaemonManager(IDaemon daemon)
        {
            this.daemon = daemon;
        }

        public async Task StartAsync()
        {
            await StopAsync();
            serviceState = ServiceState.Running;
        }

        public Task StopAsync()
        {
            serviceState = ServiceState.Stopped;
            return Task.CompletedTask;
        }

        [MemberNotNull(nameof(daemon))]
        public void ThrowIfNotReady(bool checkState = true)
        {
            if (daemon == null)
                throw new InvalidOperationException("Requested service is not attached to a daemon instance");

            if (serviceState != ServiceState.Running && checkState)
                throw new InvalidOperationException("Requested service or it's dependency is not available");
        }

        public async Task<BehaviourMode> GetBehaviourModeAsync(Guid callerId)
        {
            ThrowIfNotReady();

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            User caller = await userManager.GetUserInfoAsync(callerId, callerId);

            if (!caller.Permissions.HasFlag(UserPermissions.ManageDaemon))
                throw new UnauthorizedException(callerId);

            return daemon.BehaviourMode;
        }

        public async Task SetBehaviourModeAsync(Guid callerId, BehaviourMode behaviourMode, bool permanent)
        {
            ThrowIfNotReady();

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            User caller = await userManager.GetUserInfoAsync(callerId, callerId);

            if (!caller.Permissions.HasFlag(UserPermissions.ManageDaemon))
                throw new UnauthorizedException(callerId);

            daemon.SetBehaviourMode(behaviourMode, permanent);
        }
    }
}
