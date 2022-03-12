using Microsoft.VisualStudio.Threading;
using NatManager.Shared.RPC;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.ClientLibrary.RPC
{
    public class RpcSessionManager : ISessionManager, IServiceProxy
    {
        private IRemoteClient client;
        public IRemoteClient Client { get { return client; } }

        public RpcSessionManager(IRemoteClient client)
        {
            this.client = client;
        }

        public async Task<User> AuthenticateSessionAsync(string username, string password)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<User>(SessionManagerRpcMethods.AuthenticateSession, client.RequestTimeout, username, password);
        }

        public async Task DisconnectSessionAsync()
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(SessionManagerRpcMethods.DisconnectSession, client.RequestTimeout);
        }
    }
}
