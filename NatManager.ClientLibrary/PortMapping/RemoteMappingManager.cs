using NatManager.ClientLibrary.RPC;
using NatManager.Shared.Networking;
using NatManager.Shared.PortMapping;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.ClientLibrary.PortMapping
{
    public class RemoteMappingManager : IRemoteMappingManager, IServiceProxy
    {
        private IRemoteClient client;
        public IRemoteClient Client { get { return client; } }

        public RemoteMappingManager(IRemoteClient client)
        {
            this.client = client;
        }

        public async Task<ManagedMapping> CreateMappingAsync(Guid ownerId, Protocol proto, ushort privatePort, ushort publicPort, MACAddress privateMAC, string description, bool enabled)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<ManagedMapping>(MappingManagerRpcMethods.CreateMapping, client.RequestTimeout, ownerId, proto, privatePort, publicPort, privateMAC, description, enabled);
        }

        public async Task DeleteMappingAsync(Guid targetMappingId)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(MappingManagerRpcMethods.DeleteMapping, client.RequestTimeout, targetMappingId);
        }

        public async Task<ManagedMapping[]> GetAllMappingsAsync()
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<ManagedMapping[]>(MappingManagerRpcMethods.GetAllMappings, client.RequestTimeout);
        }

        public async Task<ManagedMapping> GetMappingInfoAsync(Guid targetMappingId)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<ManagedMapping>(MappingManagerRpcMethods.GetMappingInfo, client.RequestTimeout, targetMappingId);
        }

        public async Task<ManagedMapping[]> GetUsersMappingsAsync(Guid targetUserId)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<ManagedMapping[]>(MappingManagerRpcMethods.GetUsersMappings, client.RequestTimeout, targetUserId);
        }

        public async Task SetMappingOwnerAsync(Guid targetMappingId, Guid newOwner)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(MappingManagerRpcMethods.SetMappingOwner, client.RequestTimeout, targetMappingId, newOwner);
        }

        public async Task UpdateMappingConfigAsync(Guid targetMappingId, Protocol proto, ushort privatePort, ushort publicPort, MACAddress privateMAC, string description, bool enabled)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(MappingManagerRpcMethods.UpdateMapping, client.RequestTimeout, targetMappingId, proto, privatePort, publicPort, privateMAC, description, enabled);
        }
    }
}
