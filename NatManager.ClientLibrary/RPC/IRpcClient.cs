using NatManager.ClientLibrary.RPC.EventArgs;
using SimpleSocketsJsonRpc;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.ClientLibrary.RPC
{
    public interface IRpcClient
    {
        event EventHandler<RpcConnectedEventArgs>? Connected;
        event EventHandler? Disconnected;

        IJsonRpc? RpcConnection { get; }
        IJsonRpc? GetRpcConnection();
        bool Connect();
        void Disconnect();
    }
}
