using NatManager.ClientLibrary.RPC;
using NatManager.Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.ClientLibrary.Configuration
{
    public class RemoteConfigManager : IConfigManager, IServiceProxy
    {
        private IRemoteClient client;
        public IRemoteClient Client { get { return client; } }

        public RemoteConfigManager(IRemoteClient client)
        {
            this.client = client;
        }

        public async Task<bool> ConfigEntryExistsAsync(string key)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<bool>(ConfigManagerRpcMethods.ConfigEntryExists, client.RequestTimeout, key);
        }

        public async Task<Dictionary<string, byte[]>> GetAllConfigEntriesAsync()
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<Dictionary<string, byte[]>>(ConfigManagerRpcMethods.GetAllConfigEntries, client.RequestTimeout);
        }

        public async Task<byte[]> GetConfigValueBytesAsync(string key)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<byte[]>(ConfigManagerRpcMethods.GetConfigValueBytes, client.RequestTimeout, key);
        }

        public async Task<string> GetConfigValueStringAsync(string key)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<string>(ConfigManagerRpcMethods.GetConfigValueString, client.RequestTimeout, key);
        }

        public async Task<int> GetConfigValueIntAsync(string key)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<int>(ConfigManagerRpcMethods.GetConfigValueInt, client.RequestTimeout, key);
        }

        public async Task<uint> GetConfigValueUIntAsync(string key)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<uint>(ConfigManagerRpcMethods.GetConfigValueUInt, client.RequestTimeout, key);
        }

        public async Task SetConfigValueAsync(string key, byte[] value)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(ConfigManagerRpcMethods.SetConfigValueBytes, client.RequestTimeout, key, value);
        }

        public async Task SetConfigValueAsync(string key, string value)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(ConfigManagerRpcMethods.SetConfigValueString, client.RequestTimeout, key, value);
        }

        public async Task SetConfigValueAsync(string key, int value)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(ConfigManagerRpcMethods.SetConfigValueInt, client.RequestTimeout, key, value);
        }

        public async Task SetConfigValueAsync(string key, uint value)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(ConfigManagerRpcMethods.SetConfigValueUInt, client.RequestTimeout, key, value);
        }
    }
}
