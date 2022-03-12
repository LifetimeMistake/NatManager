using NatManager.Shared.PortMapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.PortMapping.EventArgs
{
    public class MappingUpdatedEventArgs : System.EventArgs
    {
        public Guid? CallerSubject { get; }
        public ManagedMapping OldMapping { get; }
        public ManagedMapping UpdatedMapping { get; }

        public MappingUpdatedEventArgs(Guid? callerSubject, ManagedMapping oldMapping, ManagedMapping updatedMapping)
        {
            CallerSubject = callerSubject;
            OldMapping = oldMapping ?? throw new ArgumentNullException(nameof(oldMapping));
            UpdatedMapping = updatedMapping ?? throw new ArgumentNullException(nameof(updatedMapping));
        }
    }
}
