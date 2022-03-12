using Open.Nat;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.NAT.EventArgs
{
    public class UnknownMappingFoundEventArgs : System.EventArgs
    {
        public Mapping Mapping { get; }
        public bool Deleted { get; }

        public UnknownMappingFoundEventArgs(Mapping mapping, bool deleted)
        {
            Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            Deleted = deleted;
        }
    }
}
