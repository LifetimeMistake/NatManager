using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Shared.Networking
{
    [Serializable]
    public class NetworkHost
    {
        public NetworkAddress IPAddress;
        public MACAddress PhysicalAddress;
        public string? Hostname;

        public NetworkHost(NetworkAddress ipAddress, MACAddress physicalAddress, string? hostname)
        {
            IPAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            PhysicalAddress = physicalAddress ?? throw new ArgumentNullException(nameof(physicalAddress));
            Hostname = hostname;
        }
    }
}
