using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Shared.Exceptions
{
    public class DatabaseException : Exception
    {
        public DatabaseException() : base("A database error has occurred.")
        {
        }

        public DatabaseException(string message) : base(message)
        {
        }
    }
}
