using NatManager.ClientLibrary.RPC;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.ClientLibrary
{
    public interface IServiceProxy
    {
        IRemoteClient Client { get; }
    }
}
