using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Shared.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public Guid? SubjectId;

        public UnauthorizedException(Guid? subjectId) : base("Insufficient permissions.")
        {
            SubjectId = subjectId;
        }

        public UnauthorizedException(Guid? subjectId, string message) : base(message)
        {
            SubjectId = subjectId;
        }
    }
}
