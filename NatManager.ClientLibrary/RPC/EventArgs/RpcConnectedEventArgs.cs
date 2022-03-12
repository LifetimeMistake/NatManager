using SimpleSocketsJsonRpc;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.ClientLibrary.RPC.EventArgs
{
    public class RpcConnectedEventArgs : System.EventArgs
    {
        public IJsonRpc RpcClient;

        public RpcConnectedEventArgs(IJsonRpc rpcClient)
        {
            RpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
        }
    }
}
