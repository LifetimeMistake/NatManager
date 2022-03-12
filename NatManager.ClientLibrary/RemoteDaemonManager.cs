using NatManager.ClientLibrary.RPC;
using NatManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.ClientLibrary
{
    public class RemoteDaemonManager : IServiceProxy, IRemoteDaemonManager
    {
        private IRemoteClient client;
        public IRemoteClient Client { get { return client; } }

        public RemoteDaemonManager(IRemoteClient client)
        {
            this.client = client;
        }

        public async Task<BehaviourMode> GetBehaviourModeAsync()
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<BehaviourMode>(DaemonManagerRpcMethods.GetBehaviourMode, client.RequestTimeout);
        }

        public async Task SetBehaviourModeAsync(BehaviourMode behaviourMode, bool permanent)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(DaemonManagerRpcMethods.SetBehaviourMode, client.RequestTimeout, behaviourMode, permanent);
        }
    }
}
