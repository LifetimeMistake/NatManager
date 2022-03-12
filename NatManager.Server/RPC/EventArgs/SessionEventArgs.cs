using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.RPC.EventArgs
{
    public class SessionEventArgs : System.EventArgs
    {
        public RpcSessionContext SessionContext;

        public SessionEventArgs(RpcSessionContext sessionContext)
        {
            SessionContext = sessionContext ?? throw new ArgumentNullException(nameof(sessionContext));
        }
    }
}
