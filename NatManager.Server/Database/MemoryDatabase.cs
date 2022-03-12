using NatManager.Server.Cryptography;
using NatManager.Shared.Exceptions;
using NatManager.Shared.PortMapping;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.Database
{
    public class MemoryDatabase : IDatabaseProvider
    {
        public DatabaseState State { get; private set; }
        private Dictionary<string, byte[]> configStore;
        private Dictionary<Guid, ManagedMapping> mappingStore;
        private Dictionary<Guid, User> userStore;
        private Dictionary<Guid, HashValue> credentialStore;

        public MemoryDatabase()
        {
            configStore = new Dictionary<string, byte[]>();
            mappingStore = new Dictionary<Guid, ManagedMapping>();
            userStore = new Dictionary<Guid, User>();
            credentialStore = new Dictionary<Guid, HashValue>();
        }

        public Task<bool> StartAsync()
        {
            State = DatabaseState.Connected;
            return Task.FromResult(true);
        }

        public Task<bool> StopAsync()
        {
            State = DatabaseState.Disconnected;
            return Task.FromResult(true);
        }
        public Task<bool> ConfigEntryExistsAsync(string key)
        {
            lock (configStore)
            {
                return Task.FromResult(configStore.ContainsKey(key));
            }
        }

        public Task DeletePortMappingAsync(Guid portMappingId)
        {
            lock (mappingStore)
            {
                if (!mappingStore.Remove(portMappingId))
                    throw new DatabaseException();
            }

            return Task.CompletedTask;
        }

        public Task DeleteUserAsync(Guid userId)
        {
            lock (userStore)
            {
                if (!userStore.Remove(userId) || !credentialStore.Remove(userId))
                    throw new DatabaseException();
            }

            return Task.CompletedTask;
        }

        public Task<Dictionary<string, byte[]>> GetConfigEntriesAsync()
        {
            lock (configStore)
            {
                Dictionary<string, byte[]> config = new Dictionary<string, byte[]>();
                foreach (KeyValuePair<string, byte[]> kvp in configStore)
                {
                    config.Add(kvp.Key, kvp.Value);
                }

                return Task.FromResult(config);
            }
        }

        public Task<byte[]?> GetConfigValueAsync(string key)
        {
            lock (configStore)
            {
                if (!configStore.ContainsKey(key))
                {
                    return Task.FromResult<byte[]?>(null);
                }

                return Task.FromResult<byte[]?>(configStore[key]);
            }
        }

        public Task<ManagedMapping?> GetPortMappingAsync(Guid portMappingId)
        {
            lock (mappingStore)
            {
                if (!mappingStore.ContainsKey(portMappingId))
                {
                    return Task.FromResult<ManagedMapping?>(null);
                }

                return Task.FromResult<ManagedMapping?>(mappingStore[portMappingId]);
            }
        }

        public Task<List<ManagedMapping>> GetPortMappingsAsync()
        {
            lock (mappingStore)
            {
                return Task.FromResult(mappingStore.Values.ToList());
            }
        }

        public Task<List<ManagedMapping>> GetPortMappingsAsync(Guid userId)
        {
            lock (mappingStore)
            {
                List<ManagedMapping> mappings = mappingStore.Values.Where(mapping => mapping.OwnerId == userId).ToList();
                return Task.FromResult(mappings);
            }
        }

        public Task<User?> GetUserAsync(Guid userId)
        {
            lock (userStore)
            {
                if (!userStore.ContainsKey(userId))
                {
                    return Task.FromResult<User?>(null);
                }

                return Task.FromResult<User?>(userStore[userId]);
            }
        }

        public Task<User?> GetUserAsync(string username)
        {
            lock (userStore)
            {
                User? user = userStore.Values.FirstOrDefault(u => u.Username == username);
                return Task.FromResult<User?>(user);
            }
        }

        public Task<HashValue?> GetUserCredentialsAsync(Guid userId)
        {
            lock (credentialStore)
            {
                if (!credentialStore.ContainsKey(userId))
                {
                    return Task.FromResult<HashValue?>(null);
                }

                return Task.FromResult<HashValue?>(credentialStore[userId]);
            }
        }

        public Task<List<User>> GetUsersAsync()
        {
            lock (userStore)
            {
                return Task.FromResult(userStore.Values.ToList());
            }
        }

        public Task InsertPortMappingAsync(ManagedMapping portMapping)
        {
            lock (mappingStore)
            {
                if (mappingStore.ContainsKey(portMapping.Id))
                    throw new DatabaseException();

                mappingStore.Add(portMapping.Id, portMapping);
                return Task.CompletedTask;
            }
        }

        public Task InsertUserAsync(User user, HashValue credentials)
        {
            lock (userStore)
            {
                if (userStore.ContainsKey(user.Id))
                    throw new DatabaseException();

                if (credentialStore.ContainsKey(user.Id))
                    throw new DatabaseException();

                userStore.Add(user.Id, user);
                credentialStore.Add(user.Id, credentials);
                return Task.CompletedTask;
            }
        }

        public Task SetConfigValueAsync(string key, byte[] value)
        {
            lock (configStore)
            {
                if (configStore.ContainsKey(key))
                {
                    configStore[key] = value;
                }
                else
                {
                    configStore.Add(key, value);
                }

                return Task.CompletedTask;
            }
        }

        public Task UpdatePortMappingAsync(ManagedMapping portMapping)
        {
            lock (mappingStore)
            {
                if (!mappingStore.ContainsKey(portMapping.Id))
                    throw new DatabaseException();

                mappingStore[portMapping.Id] = portMapping;
                return Task.CompletedTask;
            }
        }

        public Task UpdateUserAsync(User user)
        {
            lock (userStore)
            {
                if (!userStore.ContainsKey(user.Id))
                    throw new DatabaseException();

                userStore[user.Id] = user;
                return Task.CompletedTask;
            }
        }

        public Task UpdateUserPasswordAsync(Guid userId, HashValue passwordHash)
        {
            lock (credentialStore)
            {
                if (!credentialStore.ContainsKey(userId))
                    throw new DatabaseException();

                credentialStore[userId] = passwordHash;
                return Task.CompletedTask;
            }
        }
    }
}
