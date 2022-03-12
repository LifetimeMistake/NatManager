using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Shared
{
    public interface IRemoteDaemonManager
    {
        Task<BehaviourMode> GetBehaviourModeAsync();
        Task SetBehaviourModeAsync(BehaviourMode behaviourMode, bool permanent);
    }
}
