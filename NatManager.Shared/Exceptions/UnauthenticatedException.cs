using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Shared.Exceptions
{
    public class UnauthenticatedException : Exception
    {
        public UnauthenticatedException() : base("Authentication is required to access this object.")
        { }

        public UnauthenticatedException(string message) : base(message)
        { }
    }
}
