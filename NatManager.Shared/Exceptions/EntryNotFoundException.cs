using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Shared.Exceptions
{
    public class EntryNotFoundException : Exception
    {
        public Guid? Subject;

        public EntryNotFoundException(Guid? subject) : base("Object not found.")
        {
            Subject = subject;
        }

        public EntryNotFoundException(Guid? subject, string message) : base(message)
        {
            Subject = subject;
        }
    }
}
