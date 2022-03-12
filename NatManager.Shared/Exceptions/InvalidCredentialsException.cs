using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Shared.Exceptions
{
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException() : base("Invalid credentials.")
        { }

        public InvalidCredentialsException(string message) : base(message)
        { }
    }
}
