using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.NAT.EventArgs
{
    public class NatDeviceLostEventArgs : System.EventArgs
    {
        public NatDeviceConnection NatDevice { get; }
        public NatDeviceLostEventArgs(NatDeviceConnection natDevice)
        {
            NatDevice = natDevice ?? throw new ArgumentNullException(nameof(natDevice));
        }
    }
}
