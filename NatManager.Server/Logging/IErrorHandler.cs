using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.Logging
{
    public interface IErrorHandler
    {
        void HandleException(Exception ex);
    }
}
