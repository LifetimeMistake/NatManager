using Microsoft.VisualStudio.Threading;
using NatManager.ClientLibrary.PortMapping;
using NatManager.ClientLibrary.RPC;
using NatManager.ClientLibrary.Users;
using NatManager.Shared.Exceptions;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.ClientLibrary
{
    public class RemoteClient : IRemoteClient
    {
        protected readonly List<IServiceProxy> serviceProxies = new List<IServiceProxy>();
        private readonly SemaphoreSlim serviceProxiesLock = new SemaphoreSlim(1, 1);

        private IRpcClient rpcClient;
        private int requestTimeout;
        private User? sessionIdentity;
        
        public IRpcClient RpcClient { get { return rpcClient; } }
        public int RequestTimeout { get { return requestTimeout; } }
        public User? SessionIdentity { get { return sessionIdentity; } }

        public RemoteClient(IRpcClient rpcClient, int requestTimeout)
        {
            if (requestTimeout <= 0)
                throw new ArgumentOutOfRangeException(nameof(requestTimeout));

            this.rpcClient = rpcClient ?? throw new ArgumentException(nameof(rpcClient));
            this.requestTimeout = requestTimeout;
            RpcSessionManager rpcSessionManager = new RpcSessionManager(this);
            serviceProxies.Add(rpcSessionManager);
        }

        public async Task<bool> StartAsync()
        {
            await StopAsync();
            return rpcClient.Connect();
        }

        public async Task StopAsync()
        {
            if (rpcClient.RpcConnection != null)
            {
                try
                {
                    RpcSessionManager rpcSessionManager = await GetServiceProxyAsync<RpcSessionManager>();
                    await rpcSessionManager.DisconnectSessionAsync();
                }
                catch { }
            }
                

            rpcClient.Disconnect();
        }

        public async Task RegisterServiceProxyAsync(IServiceProxy serviceProxy)
        {
            if (serviceProxy.GetType() == typeof(RpcSessionManager))
                throw new InvalidOperationException("Cannot add a service proxy of type " + typeof(RpcSessionManager).Name);

            try
            {
                await serviceProxiesLock.WaitAsync();

                if (serviceProxies.Any(proxy => proxy.GetType() == serviceProxy.GetType()))
                    throw new InvalidOperationException("Service proxy of this type has already been registered");

                serviceProxies.Add(serviceProxy);
            }
            finally
            {
                serviceProxiesLock.Release();
            }
        }

        public async Task UnregisterServiceProxyAsync<T>() where T : IServiceProxy
        {
            if (typeof(T) == typeof(RpcSessionManager))
                throw new InvalidOperationException("Cannot remove a service proxy of type " + typeof(RpcSessionManager).Name);

            try
            {
                await serviceProxiesLock.WaitAsync();
                IServiceProxy? serviceProxy = serviceProxies.FirstOrDefault(proxy => proxy.GetType() == typeof(T));
                if (serviceProxy == null)
                    throw new InvalidOperationException("Service proxy of this type has not been registered");

                serviceProxies.Remove(serviceProxy);
            }
            finally
            {
                serviceProxiesLock.Release();
            }
        }

        public async Task<T> GetServiceProxyAsync<T>() where T : IServiceProxy
        {
            try
            {
                await serviceProxiesLock.WaitAsync();
                IServiceProxy? serviceProxy = serviceProxies.FirstOrDefault(proxy => proxy.GetType() == typeof(T));
                if (serviceProxy == null)
                    throw new InvalidOperationException("Requested service or it's dependency is not available");

                return (T)serviceProxy;
            }
            finally
            {
                serviceProxiesLock.Release();
            }
        }

        public async Task<User> AuthenticateClientAsync(string username, string password)
        {
            RpcSessionManager rpcSessionManager = await GetServiceProxyAsync<RpcSessionManager>();
            User identity = await rpcSessionManager.AuthenticateSessionAsync(username, password);
            if (identity == null) // this should not happen
                throw new ArgumentNullException(nameof(identity));

            sessionIdentity = identity;
            return identity;
        }

        public User? GetCurrentIdentity()
        {
            return sessionIdentity;
        }
    }
}
