using NatManager.Server.Cryptography;
using NatManager.Shared.PortMapping;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.Database
{
    public interface IDatabaseProvider
    {
        DatabaseState State { get; }
        Task<bool> StartAsync();
        Task<bool> StopAsync();
        Task<User?> GetUserAsync(Guid userId);
        Task<User?> GetUserAsync(string username);
        Task<List<User>> GetUsersAsync();
        Task InsertUserAsync(User user, HashValue credentials);
        Task<HashValue?> GetUserCredentialsAsync(Guid userId);
        Task UpdateUserAsync(User user);
        Task UpdateUserPasswordAsync(Guid userId, HashValue passwordHash);
        Task DeleteUserAsync(Guid userId);

        Task<ManagedMapping?> GetPortMappingAsync(Guid portMappingId);
        Task<List<ManagedMapping>> GetPortMappingsAsync();
        Task<List<ManagedMapping>> GetPortMappingsAsync(Guid userId);
        Task InsertPortMappingAsync(ManagedMapping portMapping);
        Task UpdatePortMappingAsync(ManagedMapping portMapping);
        Task DeletePortMappingAsync(Guid portMappingId);

        Task<byte[]?> GetConfigValueAsync(string key);
        Task SetConfigValueAsync(string key, byte[] value);
        Task<Dictionary<string, byte[]>> GetConfigEntriesAsync();
        Task<bool> ConfigEntryExistsAsync(string key);
    }
}
