using NatManager.Shared.PortMapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.PortMapping.EventArgs
{
    public class MappingCreatedEventArgs : System.EventArgs
    {
        public Guid? CallerSubject { get; }
        public ManagedMapping Mapping { get; }

        public MappingCreatedEventArgs(Guid? callerSubject, ManagedMapping mapping)
        {
            CallerSubject = callerSubject;
            Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
        }
    }
}
