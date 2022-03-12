using AustinHarris.JsonRpc;
using NatManager.Shared.Exceptions;
using NatManager.Shared.RPC;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.RPC.Services
{
    public class SessionManagementRpcService : JsonRpcService, ISessionManager
    {
        private readonly TcpRpcServer rpcServer;

        public SessionManagementRpcService(TcpRpcServer rpcServer)
        {
            this.rpcServer = rpcServer ?? throw new ArgumentNullException(nameof(rpcServer));
        }

        [JsonRpcMethod(SessionManagerRpcMethods.AuthenticateSession)]
        public async Task<User> AuthenticateSessionAsync(string username, string password)
        {
            RpcSessionContext? sessionContext = JsonRpcContext.Current().Value as RpcSessionContext;
            if (sessionContext == null)
                throw new InternalServerErrorException();

            User? user = await rpcServer.AuthenticateSessionContextAsync(sessionContext.Id, username, password);
            if (user == null)
                throw new InternalServerErrorException();

            return user;
        }

        [JsonRpcMethod(SessionManagerRpcMethods.DisconnectSession)]
        public async Task DisconnectSessionAsync()
        {
            RpcSessionContext? sessionContext = JsonRpcContext.Current().Value as RpcSessionContext;
            if (sessionContext == null)
                throw new InternalServerErrorException();

            if (!await rpcServer.DisconnectSessionAsync(sessionContext.Id))
                throw new InternalServerErrorException();

            await Task.CompletedTask;
        }
    }
}
