using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.NAT.EventArgs
{
    public class NatDeviceFoundEventArgs : System.EventArgs
    {
        public NatDeviceConnection NatDevice { get; }

        public NatDeviceFoundEventArgs(NatDeviceConnection natDevice)
        {
            NatDevice = natDevice ?? throw new ArgumentNullException(nameof(natDevice));
        }
    }
}
