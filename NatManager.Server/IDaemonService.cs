using NatManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server
{
    public interface IDaemonService
    {
        ServiceState State { get; }
        IDaemon? Daemon { get; set; }
        Task StartAsync();
        Task StopAsync();
        void ThrowIfNotReady(bool checkState = true);
    }
}
