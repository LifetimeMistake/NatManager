using AustinHarris.JsonRpc;
using NatManager.Server.RPC.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NatManager.Server.RPC
{
    public class AutoTcpRpcServer : TcpRpcServer
    {
        public AutoTcpRpcServer(IDaemon daemon) : base(daemon)
        {
            IEnumerable<Type> serviceTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetTypeInfo().IsAssignableTo(typeof(JsonRpcService)) && t != typeof(SessionManagementRpcService) && !t.IsAbstract);

            foreach (Type service in serviceTypes)
            {
                try
                {
                    JsonRpcService? serviceInstance = Activator.CreateInstance(service, this) as JsonRpcService;
                    if (serviceInstance == null)
                        throw new ArgumentException("Failed to construct an instance of " + service);
                    registeredServices.Add(serviceInstance);
                    daemon.GetLogger().InfoAsync($"Registered an RPC service of type {serviceInstance}");
                }
                catch (Exception ex)
                {
                    daemon.GetErrorHandler().HandleException(ex);
                }
            }
        }
    }
}
