using NatManager.ClientLibrary.PortMapping;
using NatManager.ClientLibrary.RPC;
using NatManager.ClientLibrary.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NatManager.ClientLibrary
{
    public class NatManagerClient : RemoteClient
    {
        public NatManagerClient(IRpcClient rpcClient, int requestTimeoutMs) : base(rpcClient, requestTimeoutMs)
        {
            IEnumerable<Type> serviceTypes = Assembly.GetExecutingAssembly().GetTypes().
                Where(t => t.GetTypeInfo().IsAssignableTo(typeof(IServiceProxy)) && t != typeof(RpcSessionManager) && !t.IsAbstract);

            foreach (Type service in serviceTypes)
            {
                IServiceProxy? serviceInstance = Activator.CreateInstance(service, this) as IServiceProxy;
                if (serviceInstance == null)
                    throw new ArgumentException("Failed to construct an instance of " + service);
                serviceProxies.Add(serviceInstance);
            }
        }
    }
}
