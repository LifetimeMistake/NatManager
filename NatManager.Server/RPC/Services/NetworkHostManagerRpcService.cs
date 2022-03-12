using AustinHarris.JsonRpc;
using NatManager.Server.Networking;
using NatManager.Shared.Networking;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.RPC.Services
{
    public class NetworkHostManagerRpcService : AuthenticatedJsonRpcService, IRemoteNetworkHostManager
    {
        private readonly TcpRpcServer rpcServer;

        public NetworkHostManagerRpcService(TcpRpcServer rpcServer)
        {
            this.rpcServer = rpcServer;
        }

        private IDaemon GetDaemon()
        {
            if (rpcServer.Daemon == null)
                throw new InvalidOperationException("The underlying RPC server was not attached to a daemon");

            return rpcServer.Daemon;
        }

        [JsonRpcMethod(NetworkHostManagerRpcMethods.GetAllHosts)]
        public async Task<NetworkHost[]> GetAllHostsAsync()
        {
            User identity = GetSessionIdentity();

            if (!identity.Permissions.HasFlag(UserPermissions.ManageNetwork))
                throw new UnauthorizedAccessException("Missing permissions: ManageNetwork");

            IDaemon daemon = GetDaemon();
            NetworkMapperService networkMapperService = await daemon.GetServiceAsync<NetworkMapperService>();
            return await networkMapperService.GetAliveHostsAsync();
        }
    }
}
