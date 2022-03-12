using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Shared.Exceptions
{
    public class PortUnavailableException : Exception
    {
        public ushort Port;

        public PortUnavailableException(ushort port) : base("The specified port is already taken.")
        {
            Port = port;
        }

        public PortUnavailableException(ushort port, string message) : base(message)
        {
            Port = port;
        }
    }
}
