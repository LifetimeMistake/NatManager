using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.RPC
{
    public class RpcSessionContext
    {
        public Guid Id;
        public int TransportConnectionId;
        public User? Identity;

        public RpcSessionContext(Guid sessionId, int transportConnectionId, User? identity = null)
        {
            Id = sessionId;
            TransportConnectionId = transportConnectionId;
            Identity = identity;
        }

        public RpcSessionContext(int transportConnectionId, User? identity = null)
        {
            Id = Guid.NewGuid();
            TransportConnectionId = transportConnectionId;
            Identity = identity;
        }
    }
}
