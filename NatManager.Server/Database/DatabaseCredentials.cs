using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.Database
{
    public class DatabaseCredentials
    {
        public string Host;
        public uint Port;
        public string Username;
        public string Password;
        public string Database;

        public DatabaseCredentials(string host, uint port, string username, string password, string database)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Password = password ?? throw new ArgumentNullException(nameof(password));
            Database = database ?? throw new ArgumentNullException(nameof(database));
        }
    }
}
