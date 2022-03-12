using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Shared.Exceptions
{
    public class AccountDisabledException : Exception
    {
        public Guid? SubjectId;

        public AccountDisabledException(Guid? subjectId) : base("User's account is disabled.")
        {
            SubjectId = subjectId;
        }

        public AccountDisabledException(Guid? subjectId, string message) : base(message)
        {
            SubjectId = subjectId;
        }
    }
}
