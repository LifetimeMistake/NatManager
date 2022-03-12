using NatManager.Shared;
using NatManager.Shared.Configuration;
using NatManager.Shared.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.Server.Configuration
{
    public class ConfigManager : IDaemonService, IConfigManager
    {
        private IDaemon? daemon;
        private ServiceState serviceState;

        private Dictionary<string, byte[]> configCache = new Dictionary<string, byte[]>();
        private SemaphoreSlim configCacheLock = new SemaphoreSlim(1, 1);

        public ServiceState State { get { return serviceState; } }
        public IDaemon? Daemon { get { return daemon; } set { daemon = value; } }

        public event EventHandler? ConfigEntryUpdated;

        public ConfigManager()
        {
            this.serviceState = ServiceState.Stopped;
        }

        public ConfigManager(IDaemon daemon) : this()
        {
            this.daemon = daemon;
        }

        public async Task StartAsync()
        {
            await StopAsync();
            ThrowIfNotReady(false);
            serviceState = ServiceState.Running;
        }

        public async Task StopAsync()
        {
            try
            {
                await configCacheLock.WaitAsync();
                configCache.Clear();
            }
            finally
            {
                configCacheLock.Release();
            }

            serviceState = ServiceState.Stopped;
        }

        [MemberNotNull(nameof(daemon))]
        public void ThrowIfNotReady(bool checkState = true)
        {
            if (daemon == null)
                throw new InvalidOperationException("Requested service is not attached to a daemon instance");

            if (serviceState != ServiceState.Running && checkState)
                throw new InvalidOperationException("Requested service or it's dependency is not available");
        }

        public async Task<bool> ConfigEntryExistsAsync(string key)
        {
            ThrowIfNotReady();
            return await daemon.GetDatabaseProvider().ConfigEntryExistsAsync(key);
        }

        public async Task<Dictionary<string, byte[]>> GetAllConfigEntriesAsync()
        {
            ThrowIfNotReady();
            return await daemon.GetDatabaseProvider().GetConfigEntriesAsync();
        }

        public async Task<byte[]> GetConfigValueBytesAsync(string key)
        {
            ThrowIfNotReady();

            try
            {
                await configCacheLock.WaitAsync();
                if (configCache.ContainsKey(key))
                    return configCache[key];
            }
            finally
            {
                configCacheLock.Release();
            }

            byte[]? bytes = await daemon.GetDatabaseProvider().GetConfigValueAsync(key);
            if (bytes == null)
                throw new EntryNotFoundException(null);

            try
            {
                await configCacheLock.WaitAsync();
                configCache.Add(key, bytes);
            }
            finally
            {
                configCacheLock.Release();
            }

            return bytes;
        }

        public async Task<string> GetConfigValueStringAsync(string key)
        {
            byte[] bytes = await GetConfigValueBytesAsync(key);
            return Encoding.UTF8.GetString(bytes);
        }

        public async Task<int> GetConfigValueIntAsync(string key)
        {
            byte[] bytes = await GetConfigValueBytesAsync(key);
            return BitConverter.ToInt32(bytes, 0);
        }

        public async Task<uint> GetConfigValueUIntAsync(string key)
        {
            byte[] bytes = await GetConfigValueBytesAsync(key);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public async Task SetConfigValueAsync(string key, byte[] value)
        {
            ThrowIfNotReady();

            await daemon.GetDatabaseProvider().SetConfigValueAsync(key, value);
            try
            {
                await configCacheLock.WaitAsync();
                if (configCache.ContainsKey(key))
                    configCache[key] = value;
                else
                    configCache.Add(key, value);
            }
            finally
            {
                configCacheLock.Release();
            }

            ConfigEntryUpdated?.Invoke(this, EventArgs.Empty);
        }

        public async Task SetConfigValueAsync(string key, string value)
        {
            ThrowIfNotReady();

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            await SetConfigValueAsync(key, bytes);
        }

        public async Task SetConfigValueAsync(string key, int value)
        {
            ThrowIfNotReady();

            byte[] bytes = BitConverter.GetBytes(value);
            await SetConfigValueAsync(key, bytes);
        }

        public async Task SetConfigValueAsync(string key, uint value)
        {
            ThrowIfNotReady();

            byte[] bytes = BitConverter.GetBytes(value);
            await SetConfigValueAsync(key, bytes);
        }
    }
}
