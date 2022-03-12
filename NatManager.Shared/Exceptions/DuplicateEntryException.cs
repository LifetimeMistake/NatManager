using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Shared.Exceptions
{
    public class DuplicateEntryException : Exception
    {
        public Guid? SubjectId;

        public DuplicateEntryException(Guid? subjectId) : base("Duplicate object already exists.")
        {
            SubjectId = subjectId;
        }

        public DuplicateEntryException(Guid? subjectId, string message) : base(message)
        {
            SubjectId = subjectId;
        }
    }
}
