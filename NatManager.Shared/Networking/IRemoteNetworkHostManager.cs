using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Shared.Networking
{
    public interface IRemoteNetworkHostManager
    {
        Task<NetworkHost[]> GetAllHostsAsync();
    }
}
