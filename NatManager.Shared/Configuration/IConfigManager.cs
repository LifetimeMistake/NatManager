using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Shared.Configuration
{
    public interface IConfigManager
    {
        Task<bool> ConfigEntryExistsAsync(string key);
        Task<Dictionary<string, byte[]>> GetAllConfigEntriesAsync();
        Task<byte[]> GetConfigValueBytesAsync(string key);
        Task<string> GetConfigValueStringAsync(string key);
        Task<int> GetConfigValueIntAsync(string key);
        Task<uint> GetConfigValueUIntAsync(string key);
        Task SetConfigValueAsync(string key, byte[] value);
        Task SetConfigValueAsync(string key, string value);
        Task SetConfigValueAsync(string key, int value);
        Task SetConfigValueAsync(string key, uint value);
    }
}
