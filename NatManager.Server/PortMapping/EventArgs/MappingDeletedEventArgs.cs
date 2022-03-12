using NatManager.Shared.PortMapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.PortMapping.EventArgs
{
    public class MappingDeletedEventArgs : System.EventArgs
    {
        public Guid? CallerSubject { get; }
        public ManagedMapping Mapping { get; }

        public MappingDeletedEventArgs(Guid? callerSubject, ManagedMapping mapping)
        {
            CallerSubject = callerSubject;
            Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
        }
    }
}
