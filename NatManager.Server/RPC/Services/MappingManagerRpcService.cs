using AustinHarris.JsonRpc;
using NatManager.Server.PortMapping;
using NatManager.Shared.Networking;
using NatManager.Shared.PortMapping;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.RPC.Services
{
    public class MappingManagerRpcService : AuthenticatedJsonRpcService, IRemoteMappingManager
    {
        private readonly TcpRpcServer rpcServer;

        public MappingManagerRpcService(TcpRpcServer rpcServer)
        {
            this.rpcServer = rpcServer;
        }

        private IDaemon GetDaemon()
        {
            if (rpcServer.Daemon == null)
                throw new InvalidOperationException("The underlying RPC server was not attached to a daemon");

            return rpcServer.Daemon;
        }

        [JsonRpcMethod(MappingManagerRpcMethods.CreateMapping)]
        public async Task<ManagedMapping> CreateMappingAsync(Guid ownerId, Protocol proto, ushort privatePort, ushort publicPort, MACAddress privateMAC, string description, bool enabled)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            MappingManager mappingManager = await daemon.GetServiceAsync<MappingManager>();
            return await mappingManager.CreateMappingAsync(identity.Id, ownerId, proto, privatePort, publicPort, privateMAC, description, enabled);
        }

        [JsonRpcMethod(MappingManagerRpcMethods.DeleteMapping)]
        public async Task DeleteMappingAsync(Guid targetMappingId)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            MappingManager mappingManager = await daemon.GetServiceAsync<MappingManager>();
            await mappingManager.DeleteMappingAsync(identity.Id, targetMappingId);
        }

        [JsonRpcMethod(MappingManagerRpcMethods.GetAllMappings)]
        public async Task<ManagedMapping[]> GetAllMappingsAsync()
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            MappingManager mappingManager = await daemon.GetServiceAsync<MappingManager>();
            return (await mappingManager.GetAllMappingsAsync(identity.Id)).ToArray();
        }

        [JsonRpcMethod(MappingManagerRpcMethods.GetMappingInfo)]
        public async Task<ManagedMapping> GetMappingInfoAsync(Guid targetMappingId)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            MappingManager mappingManager = await daemon.GetServiceAsync<MappingManager>();
            return await mappingManager.GetMappingInfoAsync(identity.Id, targetMappingId);
        }

        [JsonRpcMethod(MappingManagerRpcMethods.GetUsersMappings)]
        public async Task<ManagedMapping[]> GetUsersMappingsAsync(Guid targetUserId)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            MappingManager mappingManager = await daemon.GetServiceAsync<MappingManager>();
            return (await mappingManager.GetUsersMappingsAsync(identity.Id, targetUserId)).ToArray();
        }

        [JsonRpcMethod(MappingManagerRpcMethods.SetMappingOwner)]
        public async Task SetMappingOwnerAsync(Guid targetMappingId, Guid newOwner)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            MappingManager mappingManager = await daemon.GetServiceAsync<MappingManager>();
            await mappingManager.SetMappingOwnerAsync(identity.Id, targetMappingId, newOwner);
        }

        [JsonRpcMethod(MappingManagerRpcMethods.UpdateMapping)]
        public async Task UpdateMappingConfigAsync(Guid targetMappingId, Protocol proto, ushort privatePort, ushort publicPort, MACAddress privateMAC, string description, bool enabled)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            MappingManager mappingManager = await daemon.GetServiceAsync<MappingManager>();
            await mappingManager.UpdateMappingConfigAsync(identity.Id, targetMappingId, proto, privatePort, publicPort, privateMAC, description, enabled);
        }
    }
}
