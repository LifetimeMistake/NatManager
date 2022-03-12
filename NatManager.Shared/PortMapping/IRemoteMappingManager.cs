using NatManager.Shared.Networking;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Shared.PortMapping
{
    public interface IRemoteMappingManager
    {
        Task<ManagedMapping> CreateMappingAsync(Guid ownerId, Protocol proto, ushort privatePort, ushort publicPort, MACAddress privateMAC, string description, bool enabled);
        Task DeleteMappingAsync(Guid targetMappingId);
        Task SetMappingOwnerAsync(Guid targetMappingId, Guid newOwner);
        Task UpdateMappingConfigAsync(Guid targetMappingId, Protocol proto, ushort privatePort, ushort publicPort, MACAddress privateMAC, string description, bool enabled);
        Task<ManagedMapping> GetMappingInfoAsync(Guid targetMappingId);
        Task<ManagedMapping[]> GetAllMappingsAsync();
        Task<ManagedMapping[]> GetUsersMappingsAsync(Guid targetUserId);
    }
}
