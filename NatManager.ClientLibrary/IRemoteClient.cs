using NatManager.ClientLibrary.RPC;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.ClientLibrary
{
    public interface IRemoteClient
    {
        IRpcClient RpcClient { get; }
        int RequestTimeout { get; }
        User? SessionIdentity { get; }

        Task<bool> StartAsync();
        Task StopAsync();
        Task RegisterServiceProxyAsync(IServiceProxy serviceProxy);
        Task UnregisterServiceProxyAsync<T>() where T : IServiceProxy;
        Task<T> GetServiceProxyAsync<T>() where T: IServiceProxy;
        User? GetCurrentIdentity();
        Task<User> AuthenticateClientAsync(string username, string password);
    }
}
