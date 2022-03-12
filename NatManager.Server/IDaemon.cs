using NatManager.Server.Database;
using NatManager.Server.Logging;
using NatManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server
{
    public interface IDaemon
    {
        ServiceState State { get; }
        BehaviourMode BehaviourMode { get; }
        Task StartAsync();
        Task StopAsync();
        IDatabaseProvider GetDatabaseProvider();
        ILogger GetLogger();
        IErrorHandler GetErrorHandler();
        void SetErrorHandler(IErrorHandler exceptionhandler);
        Task AddServiceAsync(IDaemonService service, bool startService = false);
        Task RemoveServiceAsync<T>() where T : IDaemonService;
        Task<T> GetServiceAsync<T>() where T : IDaemonService;
        Task<bool> HasServiceAsync<T>() where T : IDaemonService;
        Task StartServiceAsync<T>(bool force = false) where T : IDaemonService;
        Task StopServiceAsync<T>(bool force = false) where T : IDaemonService;
        void SetBehaviourMode(BehaviourMode newBehaviourMode, bool permanent);
    }
}
