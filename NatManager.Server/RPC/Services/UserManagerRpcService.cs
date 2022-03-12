using AustinHarris.JsonRpc;
using NatManager.Server.Users;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.RPC.Services
{
    public class UserManagerRpcService : AuthenticatedJsonRpcService, IRemoteUserManager
    {
        private readonly TcpRpcServer rpcServer;

        public UserManagerRpcService(TcpRpcServer rpcServer)
        {
            this.rpcServer = rpcServer;
        }

        private IDaemon GetDaemon()
        {
            if (rpcServer.Daemon == null)
                throw new InvalidOperationException("The underlying RPC server was not attached to a daemon");

            return rpcServer.Daemon;
        }

        [JsonRpcMethod(UserManagerRpcMethods.CreateUser)]
        public async Task<User> CreateUserAsync(string username, string password, bool enabled)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            return await userManager.CreateUserAsync(identity.Id, username, password, enabled);
        }

        [JsonRpcMethod(UserManagerRpcMethods.GetUserList)]
        public async Task<User[]> GetUserListAsync()
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            return (await userManager.GetUsersAsync(identity.Id)).ToArray();
        }

        [JsonRpcMethod(UserManagerRpcMethods.GetUserInfo)]
        public async Task<User> GetUserInfoAsync(Guid targetUserId)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            return await userManager.GetUserInfoAsync(identity.Id, targetUserId);
        }

        [JsonRpcMethod(UserManagerRpcMethods.DeleteUser)]
        public async Task DeleteUserAsync(Guid targetUserId)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            await userManager.DeleteUserAsync(identity.Id, targetUserId);
        }

        [JsonRpcMethod(UserManagerRpcMethods.SetUserPermissions)]
        public async Task SetUserPermissionsAsync(Guid targetUserId, UserPermissions userPermissions)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            await userManager.SetUserPermissionsAsync(identity.Id, targetUserId, userPermissions);
        }

        [JsonRpcMethod(UserManagerRpcMethods.ChangeUserCredentials)]
        public async Task ChangeUserCredentialsAsync(Guid targetUserId, string newPassword)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            await userManager.ChangeUserPasswordAsync(identity.Id, targetUserId, newPassword);
        }

        [JsonRpcMethod(UserManagerRpcMethods.SetEnabledState)]
        public async Task SetEnabledStateAsync(Guid targetUserId, bool enabled)
        {
            User identity = GetSessionIdentity();
            IDaemon daemon = GetDaemon();
            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            await userManager.SetEnabledStateAsync(identity.Id, targetUserId, enabled);
        }
    }
}
