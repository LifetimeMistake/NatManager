using AustinHarris.JsonRpc;
using NatManager.Shared.Exceptions;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.RPC.Services
{
    public abstract class AuthenticatedJsonRpcService : JsonRpcService
    {
        public RpcSessionContext GetSessionContext()
        {
            RpcSessionContext? sessionContext = JsonRpcContext.Current().Value as RpcSessionContext;
            if (sessionContext == null)
                throw new InternalServerErrorException();

            return sessionContext;
        }

        public User GetSessionIdentity()
        {
            RpcSessionContext context = GetSessionContext();
            if (context.Identity == null)
                throw new UnauthenticatedException();

            return context.Identity;
        }
    }
}
