using AustinHarris.JsonRpc;
using NatManager.Server.Configuration;
using NatManager.Shared.Configuration;
using NatManager.Shared.Exceptions;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.RPC.Services
{
    public class ConfigManagerRpcService : AuthenticatedJsonRpcService, IConfigManager
    {
        private readonly TcpRpcServer rpcServer;

        public ConfigManagerRpcService(TcpRpcServer rpcServer)
        {
            this.rpcServer = rpcServer;
        }

        private IDaemon GetDaemon()
        {
            if (rpcServer.Daemon == null)
                throw new InvalidOperationException("The underlying RPC server was not attached to a daemon");

            return rpcServer.Daemon;
        }

        private void CheckPermissions()
        {
            User identity = GetSessionIdentity();
            if (!identity.Permissions.HasFlag(UserPermissions.ManageSettings))
                throw new UnauthorizedException(identity.Id);
        }

        [JsonRpcMethod(ConfigManagerRpcMethods.ConfigEntryExists)]
        public async Task<bool> ConfigEntryExistsAsync(string key)
        {
            CheckPermissions();
            IDaemon daemon = GetDaemon();
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            return await configManager.ConfigEntryExistsAsync(key);
        }

        [JsonRpcMethod(ConfigManagerRpcMethods.GetAllConfigEntries)]
        public async Task<Dictionary<string, byte[]>> GetAllConfigEntriesAsync()
        {
            CheckPermissions();
            IDaemon daemon = GetDaemon();
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            return await configManager.GetAllConfigEntriesAsync();
        }

        [JsonRpcMethod(ConfigManagerRpcMethods.GetConfigValueBytes)]
        public async Task<byte[]> GetConfigValueBytesAsync(string key)
        {
            CheckPermissions();
            IDaemon daemon = GetDaemon();
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            return await configManager.GetConfigValueBytesAsync(key);
        }

        [JsonRpcMethod(ConfigManagerRpcMethods.GetConfigValueString)]
        public async Task<string> GetConfigValueStringAsync(string key)
        {
            CheckPermissions();
            IDaemon daemon = GetDaemon();
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            return await configManager.GetConfigValueStringAsync(key);
        }

        [JsonRpcMethod(ConfigManagerRpcMethods.GetConfigValueInt)]
        public async Task<int> GetConfigValueIntAsync(string key)
        {
            CheckPermissions();
            IDaemon daemon = GetDaemon();
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            return await configManager.GetConfigValueIntAsync(key);
        }

        [JsonRpcMethod(ConfigManagerRpcMethods.GetConfigValueUInt)]
        public async Task<uint> GetConfigValueUIntAsync(string key)
        {
            CheckPermissions();
            IDaemon daemon = GetDaemon();
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            return await configManager.GetConfigValueUIntAsync(key);
        }

        [JsonRpcMethod(ConfigManagerRpcMethods.SetConfigValueBytes)]
        public async Task SetConfigValueAsync(string key, byte[] value)
        {
            CheckPermissions();
            IDaemon daemon = GetDaemon();
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            await configManager.SetConfigValueAsync(key, value);
        }

        [JsonRpcMethod(ConfigManagerRpcMethods.SetConfigValueString)]
        public async Task SetConfigValueAsync(string key, string value)
        {
            CheckPermissions();
            IDaemon daemon = GetDaemon();
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            await configManager.SetConfigValueAsync(key, value);
        }

        [JsonRpcMethod(ConfigManagerRpcMethods.SetConfigValueInt)]
        public async Task SetConfigValueAsync(string key, int value)
        {
            CheckPermissions();
            IDaemon daemon = GetDaemon();
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            await configManager.SetConfigValueAsync(key, value);
        }

        [JsonRpcMethod(ConfigManagerRpcMethods.SetConfigValueUInt)]
        public async Task SetConfigValueAsync(string key, uint value)
        {
            CheckPermissions();
            IDaemon daemon = GetDaemon();
            ConfigManager configManager = await daemon.GetServiceAsync<ConfigManager>();
            await configManager.SetConfigValueAsync(key, value);
        }
    }
}
