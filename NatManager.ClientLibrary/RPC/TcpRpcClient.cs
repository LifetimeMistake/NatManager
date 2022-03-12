using NatManager.ClientLibrary.RPC.EventArgs;
using Nerdbank.Streams;
using SimpleSockets.Client;
using SimpleSocketsJsonRpc;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace NatManager.ClientLibrary.RPC
{
    public class TcpRpcClient : IRpcClient
    {
        private IPEndPoint? serverEndpoint;
        private SimpleSocketTcpClient? tcpClient;
        private SimpleSocketJsonRpc? jsonRpc;

        public event EventHandler<RpcConnectedEventArgs>? Connected;
        public event EventHandler? Disconnected;

        public IJsonRpc? RpcConnection { get { return jsonRpc; } }
        
        public TcpRpcClient(IPEndPoint serverEndpoint)
        {
            this.serverEndpoint = serverEndpoint;
        }

        public bool Connect()
        {
            if (serverEndpoint == null)
                throw new ArgumentNullException(nameof(serverEndpoint));

            Disconnect();

            try
            {
                tcpClient = new SimpleSocketTcpClient();
                tcpClient.AllowReceivingFiles = false;
                tcpClient.ConnectedToServer += TcpClient_ConnectedToServer;
                tcpClient.DisconnectedFromServer += TcpClient_DisconnectedFromServer;
                tcpClient.StartClient(serverEndpoint.Address.ToString(), serverEndpoint.Port);
            }
            catch(Exception)
            {
                Disconnect();
                return false;
            }

            return true;
        }

        public void Disconnect()
        {
            if (tcpClient != null && tcpClient.IsConnected())
            {
                tcpClient.Close();
                tcpClient.ConnectedToServer -= TcpClient_ConnectedToServer;
                tcpClient.DisconnectedFromServer -= TcpClient_DisconnectedFromServer;
            }

            tcpClient = null;
        }

        public IJsonRpc? GetRpcConnection()
        {
            return jsonRpc;
        }

        private void TcpClient_ConnectedToServer(SimpleSocketClient client)
        {
            jsonRpc = new SimpleSocketJsonRpc(client);
            Connected?.Invoke(this, new RpcConnectedEventArgs(jsonRpc));
        }

        private void TcpClient_DisconnectedFromServer(SimpleSocketClient client)
        {
            jsonRpc?.Dispose();
            jsonRpc = null;
            Disconnected?.Invoke(this, new System.EventArgs());
        }
    }
}
