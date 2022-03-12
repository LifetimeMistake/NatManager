using NatManager.Shared.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.Networking.EventArgs
{
    public class NetworkHostLostEventArgs : System.EventArgs
    {
        public NetworkHost NetworkHost { get; }

        public NetworkHostLostEventArgs(NetworkHost networkHost)
        {
            NetworkHost = networkHost ?? throw new ArgumentNullException(nameof(networkHost));
        }
    }
}
