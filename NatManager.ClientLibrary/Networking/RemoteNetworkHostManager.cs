using NatManager.Shared.Configuration;
using NatManager.Shared.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.ClientLibrary.Networking
{
    public class RemoteNetworkHostManager : IRemoteNetworkHostManager, IServiceProxy
    {
        private IRemoteClient client;
        public IRemoteClient Client { get { return client; } }

        public RemoteNetworkHostManager(IRemoteClient client)
        {
            this.client = client;
        }

        public async Task<NetworkHost[]> GetAllHostsAsync()
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<NetworkHost[]>(NetworkHostManagerRpcMethods.GetAllHosts, client.RequestTimeout);
        }
    }
}
