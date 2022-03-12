using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Shared.Exceptions
{
    public class InternalServerErrorException : Exception
    {
        public InternalServerErrorException() : base("Internal server error")
        { }

        public InternalServerErrorException(string message) : base(message)
        { }
    }
}
