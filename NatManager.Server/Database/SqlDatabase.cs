using NatManager.Server.Cryptography;
using NatManager.Shared.PortMapping;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data.Common;
using NatManager.Shared.Exceptions;
using NatManager.Shared.Networking;
using System.Threading;

namespace NatManager.Server.Database
{
    public class SqlDatabase : IDatabaseProvider
    {
        public DatabaseState State
        {
            get
            {
                if (sqlConnection == null)
                    return DatabaseState.Disconnected;

                try
                {
                    return sqlConnection.Ping() ? DatabaseState.Connected : DatabaseState.Disconnected;
                }
                catch
                { 
                    return DatabaseState.Disconnected;
                }
            }
        }

        private SemaphoreSlim databaseStateLock = new SemaphoreSlim(1, 1);
        private string connectionString;
        private MySqlConnection? sqlConnection;

        public SqlDatabase(string host, uint port, string username, string password, string database, bool disableSsl = false)
        {
            MySqlConnectionStringBuilder mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder();
            mySqlConnectionStringBuilder.Server = host;
            mySqlConnectionStringBuilder.Port = port;
            mySqlConnectionStringBuilder.UserID = username;
            mySqlConnectionStringBuilder.Password = password;
            mySqlConnectionStringBuilder.Database = database;
            mySqlConnectionStringBuilder.CharacterSet = "utf8mb4";
            if (disableSsl) mySqlConnectionStringBuilder.SslMode = MySqlSslMode.None;
            connectionString = mySqlConnectionStringBuilder.ToString();
        }

        public SqlDatabase(DatabaseCredentials databaseCredentials, bool disableSsl = false)
        {
            MySqlConnectionStringBuilder mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder();
            mySqlConnectionStringBuilder.Server = databaseCredentials.Host;
            mySqlConnectionStringBuilder.Port = databaseCredentials.Port;
            mySqlConnectionStringBuilder.UserID = databaseCredentials.Username;
            mySqlConnectionStringBuilder.Password = databaseCredentials.Password;
            mySqlConnectionStringBuilder.Database = databaseCredentials.Database;
            if (disableSsl) mySqlConnectionStringBuilder.SslMode = MySqlSslMode.None;
            mySqlConnectionStringBuilder.CharacterSet = "utf8mb4";
            connectionString = mySqlConnectionStringBuilder.ToString();
        }

        public async Task<bool> StartAsync()
        {
            sqlConnection = new MySqlConnection(connectionString);
            await sqlConnection.OpenAsync();
            return State == DatabaseState.Connected;
        }

        public async Task<bool> StopAsync()
        {
            if(sqlConnection != null)
            {
                await sqlConnection.CloseAsync();
                sqlConnection = null;
            }

            return State == DatabaseState.Disconnected;
        }

        public async Task EnsureAvailable()
        {
            try
            {
                await databaseStateLock.WaitAsync();
                if (State != DatabaseState.Connected)
                {
                    if (!await StartAsync())
                        throw new Exception("The system is experiencing database errors. Please try again later.");
                }
            }
            finally
            {
                databaseStateLock.Release();
            }
        }

        public async Task<User?> GetUserAsync(Guid userId)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("SELECT * FROM `users` WHERE `GUID` = ?guid", sqlConnection);
            command.Parameters.AddWithValue("guid", userId);
            using DbDataReader reader = await command.ExecuteReaderAsync();
            List<User> users = new List<User>();
            if (!await reader.ReadAsync())
                return null;

            Guid guid = Guid.Parse((string)reader["GUID"]);
            string username = (string)reader["Username"];
            UserPermissions permissions = (UserPermissions)reader["Permissions"];
            bool enabled = (bool)reader["Enabled"];
            DateTime createdDate = (DateTime)reader["CreationDate"];
            Guid createdBy = Guid.Parse((string)reader["CreatedBy"]);
            return new User(guid, username, permissions, enabled, createdDate, createdBy);
        }

        public async Task<User?> GetUserAsync(string usernameKey)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("SELECT * FROM `users` WHERE `Username` = ?username", sqlConnection);
            command.Parameters.AddWithValue("username", usernameKey);
            using DbDataReader reader = await command.ExecuteReaderAsync();
            List<User> users = new List<User>();
            if (!await reader.ReadAsync())
                return null;

            Guid guid = Guid.Parse((string)reader["GUID"]);
            string username = (string)reader["Username"];
            UserPermissions permissions = (UserPermissions)reader["Permissions"];
            bool enabled = (bool)reader["Enabled"];
            DateTime createdDate = (DateTime)reader["CreationDate"];
            Guid createdBy = Guid.Parse((string)reader["CreatedBy"]);
            return new User(guid, username, permissions, enabled, createdDate, createdBy);
        }

        public async Task<List<User>> GetUsersAsync()
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("SELECT * FROM `users`", sqlConnection);
            using DbDataReader reader = await command.ExecuteReaderAsync();
            List<User> users = new List<User>();
            while(await reader.ReadAsync())
            {
                Guid guid = Guid.Parse((string)reader["GUID"]);
                string username = (string)reader["Username"];
                UserPermissions permissions = (UserPermissions)reader["Permissions"];
                bool enabled = (bool)reader["Enabled"];
                DateTime createdDate = (DateTime)reader["CreationDate"];
                Guid createdBy = Guid.Parse((string)reader["CreatedBy"]);
                users.Add(new User(guid, username, permissions, enabled, createdDate, createdBy));
            }

            return users;
        }

        public async Task InsertUserAsync(User user, HashValue credentials)
        {
            await EnsureAvailable();
            using MySqlCommand insertUserCommand = new MySqlCommand("INSERT INTO `users` (`GUID`, `Username`, `Permissions`, `Enabled`, `CreationDate`, `CreatedBy`, `Hash`, `Salt`) " +
                "VALUES (?guid, ?username, ?permissions, ?enabled, ?creationDate, ?createdBy, ?hash, ?salt)", sqlConnection);

            insertUserCommand.Parameters.AddWithValue("guid", user.Id);
            insertUserCommand.Parameters.AddWithValue("username", user.Username);
            insertUserCommand.Parameters.AddWithValue("permissions", user.Permissions);
            insertUserCommand.Parameters.AddWithValue("enabled", user.Enabled);
            insertUserCommand.Parameters.AddWithValue("creationDate", user.CreatedDate);
            insertUserCommand.Parameters.AddWithValue("createdBy", user.CreatedBy);
            insertUserCommand.Parameters.AddWithValue("hash", credentials.Hash);
            insertUserCommand.Parameters.AddWithValue("salt", credentials.Salt);

            await insertUserCommand.ExecuteNonQueryAsync();
        }

        public async Task<HashValue?> GetUserCredentialsAsync(Guid userId)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("SELECT `Hash`,`Salt` FROM `users` WHERE `GUID` = ?guid", sqlConnection);
            command.Parameters.AddWithValue("guid", userId);
            using DbDataReader reader = await command.ExecuteReaderAsync();
            List<User> users = new List<User>();
            if (!await reader.ReadAsync())
                return null;

            byte[] salt = (byte[])reader["Salt"];
            byte[] hash = (byte[])reader["Hash"];
            return new HashValue(hash, salt);
        }

        public async Task UpdateUserAsync(User user)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("UPDATE `users` SET `Username` = ?username, `Permissions` = ?permissions, " +
                "`Enabled` = ?enabled, `CreationDate` = ?creationDate, `CreatedBy` = ?createdBy WHERE `GUID` = ?guid", sqlConnection);

            command.Parameters.AddWithValue("guid", user.Id);
            command.Parameters.AddWithValue("username", user.Username);
            command.Parameters.AddWithValue("permissions", user.Permissions);
            command.Parameters.AddWithValue("enabled", user.Enabled);
            command.Parameters.AddWithValue("creationDate", user.CreatedDate);
            command.Parameters.AddWithValue("createdBy", user.CreatedBy);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateUserPasswordAsync(Guid userId, HashValue passwordHash)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("UPDATE `users` SET `Salt` = ?salt, `Hash` = ?hash WHERE `GUID` = ?guid", sqlConnection);

            command.Parameters.AddWithValue("guid", userId);
            command.Parameters.AddWithValue("hash", passwordHash.Hash);
            command.Parameters.AddWithValue("salt", passwordHash.Salt);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteUserAsync(Guid userId)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("DELETE FROM `users` WHERE `GUID` = ?guid");
            command.Parameters.AddWithValue("guid", userId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<ManagedMapping?> GetPortMappingAsync(Guid portMappingId)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("SELECT * FROM `mappings` WHERE `GUID` = ?guid", sqlConnection);
            command.Parameters.AddWithValue("guid", portMappingId);

            using DbDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            Guid guid = Guid.Parse((string)reader["GUID"]);
            Guid ownerGuid = Guid.Parse((string)reader["OwnerGUID"]);
            Protocol protocol = (Protocol)reader["Protocol"];
            ushort privatePort = (ushort)reader["PrivatePort"];
            ushort publicPort = (ushort)reader["PublicPort"];
            MACAddress macAddress = new MACAddress((byte[])reader["PrivateMAC"]);
            string description = (string)reader["Description"];
            bool enabled = (bool)reader["Enabled"];
            DateTime dateTime = (DateTime)reader["CreatedDate"];
            Guid createdBy = Guid.Parse((string)reader["CreatedBy"]);

            return new ManagedMapping(guid, ownerGuid, protocol, privatePort, publicPort, macAddress, description, enabled, dateTime, createdBy);
        }

        public async Task<List<ManagedMapping>> GetPortMappingsAsync()
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("SELECT * FROM `mappings`", sqlConnection);
            using DbDataReader reader = await command.ExecuteReaderAsync();
            List<ManagedMapping> mappings = new List<ManagedMapping>();

            while (await reader.ReadAsync())
            {
                Guid guid = Guid.Parse((string)reader["GUID"]);
                Guid ownerGuid = Guid.Parse((string)reader["OwnerGUID"]);
                Protocol protocol = (Protocol)reader["Protocol"];
                ushort privatePort = (ushort)reader["PrivatePort"];
                ushort publicPort = (ushort)reader["PublicPort"];
                MACAddress macAddress = new MACAddress((byte[])reader["PrivateMAC"]);
                string description = (string)reader["Description"];
                bool enabled = (bool)reader["Enabled"];
                DateTime dateTime = (DateTime)reader["CreatedDate"];
                Guid createdBy = Guid.Parse((string)reader["CreatedBy"]);
                mappings.Add(new ManagedMapping(guid, ownerGuid, protocol, privatePort, publicPort, macAddress, description, enabled, dateTime, createdBy));
            }

            return mappings;
        }

        public async Task<List<ManagedMapping>> GetPortMappingsAsync(Guid userId)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("SELECT * FROM `mappings` WHERE `OwnerGUID` = ?guid", sqlConnection);
            command.Parameters.AddWithValue("guid", userId);

            using DbDataReader reader = await command.ExecuteReaderAsync();
            List<ManagedMapping> mappings = new List<ManagedMapping>();

            while (await reader.ReadAsync())
            {
                Guid guid = Guid.Parse((string)reader["GUID"]);
                Guid ownerGuid = Guid.Parse((string)reader["OwnerGUID"]);
                Protocol protocol = (Protocol)reader["Protocol"];
                ushort privatePort = (ushort)reader["PrivatePort"];
                ushort publicPort = (ushort)reader["PublicPort"];
                MACAddress macAddress = new MACAddress((byte[])reader["PrivateMAC"]);
                string description = (string)reader["Description"];
                bool enabled = (bool)reader["Enabled"];
                DateTime dateTime = (DateTime)reader["CreatedDate"];
                Guid createdBy = Guid.Parse((string)reader["CreatedBy"]);
                mappings.Add(new ManagedMapping(guid, ownerGuid, protocol, privatePort, publicPort, macAddress, description, enabled, dateTime, createdBy));
            }

            return mappings;
        }

        public async Task InsertPortMappingAsync(ManagedMapping portMapping)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("INSERT INTO `mappings` (`GUID`, `OwnerGUID`, `Protocol`, `PrivatePort`, `PublicPort`, `PrivateMAC`, " +
                "`Description`, `Enabled`, `CreatedDate`, `CreatedBy`) VALUES (?guid, ?ownerGuid, ?protocol, ?privatePort, ?publicPort, ?privateMAC, ?description, ?enabled, ?createdDate, ?createdBy)", sqlConnection);

            command.Parameters.AddWithValue("guid", portMapping.Id);
            command.Parameters.AddWithValue("ownerGuid", portMapping.OwnerId);
            command.Parameters.AddWithValue("protocol", portMapping.Protocol);
            command.Parameters.AddWithValue("privatePort", portMapping.PrivatePort);
            command.Parameters.AddWithValue("publicPort", portMapping.PublicPort);
            command.Parameters.AddWithValue("privateMAC", portMapping.PrivateMAC.GetAddressBytes());
            command.Parameters.AddWithValue("description", portMapping.Description);
            command.Parameters.AddWithValue("enabled", portMapping.Enabled);
            command.Parameters.AddWithValue("createdDate", portMapping.CreatedDate);
            command.Parameters.AddWithValue("createdBy", portMapping.CreatedBy);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdatePortMappingAsync(ManagedMapping portMapping)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("UPDATE `mappings` SET `OwnerGUID` = ?ownerGuid, `Protocol` = ?protocol, `PrivatePort` = ?privatePort, `PublicPort` = ?publicPort, " +
                "`PrivateMAC` = ?privateMAC, `Description` = ?description, `Enabled` = ?enabled, `CreatedDate` = ?createdDate, `CreatedBy` = ?createdBy WHERE `GUID` = ?guid", sqlConnection);

            command.Parameters.AddWithValue("guid", portMapping.Id);
            command.Parameters.AddWithValue("ownerGuid", portMapping.OwnerId);
            command.Parameters.AddWithValue("protocol", portMapping.Protocol);
            command.Parameters.AddWithValue("privatePort", portMapping.PrivatePort);
            command.Parameters.AddWithValue("publicPort", portMapping.PublicPort);
            command.Parameters.AddWithValue("privateMAC", portMapping.PrivateMAC.GetAddressBytes());
            command.Parameters.AddWithValue("description", portMapping.Description);
            command.Parameters.AddWithValue("enabled", portMapping.Enabled);
            command.Parameters.AddWithValue("createdDate", portMapping.CreatedDate);
            command.Parameters.AddWithValue("createdBy", portMapping.CreatedBy);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeletePortMappingAsync(Guid portMappingId)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("DELETE FROM `mappings` WHERE `GUID` = ?guid", sqlConnection);
            command.Parameters.AddWithValue("guid", portMappingId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<byte[]?> GetConfigValueAsync(string key)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("SELECT `ConfigValue` FROM `configuration` WHERE `ConfigKey` = ?key", sqlConnection);
            command.Parameters.AddWithValue("key", key);
            object? result = await command.ExecuteScalarAsync();
            if (result == null)
                return null;

            return (byte[])result;
        }

        public async Task SetConfigValueAsync(string key, byte[]? value)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("INSERT INTO `configuration` (`ConfigKey`, `ConfigValue`) VALUES(?key, ?value) ON DUPLICATE KEY UPDATE `ConfigKey`=?key, ConfigValue=?value", sqlConnection);
            command.Parameters.AddWithValue("key", key);
            command.Parameters.AddWithValue("value", value);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<Dictionary<string, byte[]>> GetConfigEntriesAsync()
        {
            await EnsureAvailable();
            Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>();
            using MySqlCommand command = new MySqlCommand("SELECT * FROM `configuration`", sqlConnection);
            using DbDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string key = (string)reader["ConfigKey"];
                var value = reader["ConfigValue"];
                entries.Add(key, (byte[])value);
            }

            return entries;
        }

        public async Task<bool> ConfigEntryExistsAsync(string key)
        {
            await EnsureAvailable();
            using MySqlCommand command = new MySqlCommand("SELECT COUNT(*) FROM `configuration` WHERE `ConfigKey` = ?key", sqlConnection);
            command.Parameters.AddWithValue("key", key);
            long? result = await command.ExecuteScalarAsync() as long?;
            if (!result.HasValue)
                return false;

            return result.Value > 0;
        }
    }
}
