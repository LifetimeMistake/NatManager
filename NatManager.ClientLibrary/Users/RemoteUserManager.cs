using NatManager.ClientLibrary.RPC;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.ClientLibrary.Users
{
    public class RemoteUserManager : IRemoteUserManager, IServiceProxy
    {
        private IRemoteClient client;
        public IRemoteClient Client { get { return client; } }

        public RemoteUserManager(IRemoteClient client)
        {
            this.client = client;
        }

        public async Task ChangeUserCredentialsAsync(Guid targetUserId, string newPassword)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(UserManagerRpcMethods.ChangeUserCredentials, client.RequestTimeout, targetUserId, newPassword);
        }

        public async Task<User> CreateUserAsync(string username, string password, bool enabled)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<User>(UserManagerRpcMethods.CreateUser, client.RequestTimeout, username, password, enabled);
        }

        public async Task DeleteUserAsync(Guid targetUserId)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(UserManagerRpcMethods.DeleteUser, client.RequestTimeout, targetUserId);
        }

        public async Task<User> GetUserInfoAsync(Guid targetUserId)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<User>(UserManagerRpcMethods.GetUserInfo, client.RequestTimeout, targetUserId);
        }

        public async Task<User[]> GetUserListAsync()
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            return await client.RpcClient.RpcConnection.InvokeAsync<User[]>(UserManagerRpcMethods.GetUserList, client.RequestTimeout);
        }

        public async Task SetEnabledStateAsync(Guid targetUserId, bool enabled)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(UserManagerRpcMethods.SetEnabledState, client.RequestTimeout, targetUserId, enabled);
        }

        public async Task SetUserPermissionsAsync(Guid targetUserId, UserPermissions userPermissions)
        {
            if (client.RpcClient.RpcConnection == null)
                throw new InvalidOperationException("Attempted to invoke an RPC method while the connection with the remote server was not estabilished");

            await client.RpcClient.RpcConnection.InvokeAsync(UserManagerRpcMethods.SetUserPermissions, client.RequestTimeout, targetUserId, userPermissions);
        }
    }
}