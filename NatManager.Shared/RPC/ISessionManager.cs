using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Shared.RPC
{
    public interface ISessionManager
    {
        Task<User> AuthenticateSessionAsync(string username, string password);
        Task DisconnectSessionAsync();
    }
}
