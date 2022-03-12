using AustinHarris.JsonRpc;
using NatManager.Shared;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.RPC.Services
{
    public class DaemonManagerRpcService : AuthenticatedJsonRpcService, IRemoteDaemonManager
    {
        private readonly TcpRpcServer rpcServer;

        public DaemonManagerRpcService(TcpRpcServer rpcServer)
        {
            this.rpcServer = rpcServer;
        }

        private IDaemon GetDaemon()
        {
            if (rpcServer.Daemon == null)
                throw new InvalidOperationException("The underlying RPC server was not attached to a daemon");

            return rpcServer.Daemon;
        }

        [JsonRpcMethod(DaemonManagerRpcMethods.GetBehaviourMode)]
        public async Task<BehaviourMode> GetBehaviourModeAsync()
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            DaemonManager daemonManager = await daemon.GetServiceAsync<DaemonManager>();
            return await daemonManager.GetBehaviourModeAsync(identity.Id);
        }

        [JsonRpcMethod(DaemonManagerRpcMethods.SetBehaviourMode)]
        public async Task SetBehaviourModeAsync(BehaviourMode behaviourMode, bool permanent)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            DaemonManager daemonManager = await daemon.GetServiceAsync<DaemonManager>();
            await daemonManager.SetBehaviourModeAsync(identity.Id, behaviourMode, permanent);
        }
    }
}
