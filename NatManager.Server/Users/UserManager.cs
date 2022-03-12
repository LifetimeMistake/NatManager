using NatManager.Server.Cryptography;
using NatManager.Server.Database;
using NatManager.Server.Users.EventArgs;
using NatManager.Shared;
using NatManager.Shared.Exceptions;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.Users
{
    public class UserManager : IDaemonService
    {
        public const string ALLOWED_USERNAME_CHARS = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
        private IDaemon? daemon;
        private ServiceState serviceState;

         public IDaemon? Daemon { get { return daemon; } set { daemon = value; } }
        public ServiceState State { get { return serviceState; } }
        public Guid RootUserId { get; private set; }

        public event EventHandler<UserCreatedEventArgs>? UserCreated;
        public event EventHandler<UserUpdatedEventArgs>? UserUpdated;
        public event EventHandler<UserCredentialsUpdatedEventArgs>? UserCredentialsUpdated;
        public event EventHandler<UserDeletedEventArgs>? UserDeleted;

        public UserManager(IDaemon daemon)
        {
            this.daemon = daemon;
            serviceState = ServiceState.Stopped;
        }

        public async Task StartAsync()
        {
            await StopAsync();
            ThrowIfNotReady(false);
            await VerifyRootUserAsync();

            serviceState = ServiceState.Running;
        }

        public Task StopAsync()
        {
            RootUserId = Guid.Empty;
            serviceState = ServiceState.Stopped;
            return Task.CompletedTask;
        }

        [MemberNotNull(nameof(daemon))]
        public void ThrowIfNotReady(bool checkState = true)
        {
            if (daemon == null)
                throw new InvalidOperationException("Requested service is not attached to a daemon instance");

            if (serviceState != ServiceState.Running && checkState)
                throw new InvalidOperationException("Requested service or it's dependency is not available");
        }

        private async Task VerifyRootUserAsync()
        {
            ThrowIfNotReady(false);
            IDatabaseProvider databaseProvider = daemon.GetDatabaseProvider();
            User? rootUser = await databaseProvider.GetUserAsync(Guid.Empty);

            if (rootUser == null)
            {
                // Create the root user
                await daemon.GetLogger().InfoAsync("Generating root account!");
                rootUser = new User(Guid.Empty, "root", UserPermissions.Administrator, true, DateTime.UtcNow, Guid.Empty);
                HashValue credentials = await CryptographyProvider.Argon2Async("root");
                await databaseProvider.InsertUserAsync(rootUser, credentials);
                await daemon.GetLogger().InfoAsync("Done");
                UserCreated?.Invoke(this, new UserCreatedEventArgs(null, rootUser));
            }

            RootUserId = rootUser.Id;
        }

        /// <summary>
        /// Attempts to match provided crededentials to a target user, returns the user's information on success
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="DatabaseException"></exception>
        /// <exception cref="EntryNotFoundException"></exception>
        /// <exception cref="InvalidCredentialsException"></exception>
        public async Task<User> AuthenticateUserAsync(string username, string password)
        {
            ThrowIfNotReady();

            User? user = await daemon.GetDatabaseProvider().GetUserAsync(username);

            if (user == null)
                throw new EntryNotFoundException(Guid.Empty);

            if (!user.Enabled)
                throw new AccountDisabledException(null, "User's account is locked.");

            HashValue? storedCredentials = await daemon.GetDatabaseProvider().GetUserCredentialsAsync(user.Id);
            if (storedCredentials == null)
                throw new EntryNotFoundException(Guid.Empty);

            if (!CryptographyProvider.HashEqual(password, storedCredentials))
                throw new InvalidCredentialsException("Invalid credentials.");

            await daemon.GetLogger().InfoAsync($"User {username} logged in.");
            return user;
        }

        /// <summary>
        /// Creates a user
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="DuplicateEntryException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task<User> CreateUserAsync(Guid callerId, string username, string password, bool enabled)
        {
            ThrowIfNotReady();

            User caller = await GetUserInfoAsync(callerId, callerId);
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty.");
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.");

            foreach (char c in username)
                if (!ALLOWED_USERNAME_CHARS.Contains(c))
                    throw new ArgumentException($"Username contains an illegal character: \"{c}\"");

            if (!caller.Permissions.HasFlag(UserPermissions.ManageUsers))
                throw new UnauthorizedException(callerId, "Missing permissions: ManageUsers");

            Guid guid = Guid.NewGuid();
            List<User> users = await GetUsersAsync(callerId);

            if (users.Any(user => user.Username == username))
                throw new DuplicateEntryException(callerId, "Username already taken.");

            User newUser = new User(guid, username, UserPermissions.Standard, enabled, DateTime.UtcNow, callerId);
            HashValue passwordHash = await CryptographyProvider.Argon2Async(password);

            await daemon.GetDatabaseProvider().InsertUserAsync(newUser, passwordHash);

            UserCreated?.Invoke(this, new UserCreatedEventArgs(callerId, newUser));
            return newUser;
        }

        /// <summary>
        /// Returns the target user object
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetUserId"></param>
        /// <returns></returns>
        /// <exception cref="DatabaseException"></exception>
        /// <exception cref="EntryNotFoundException"></exception>
        /// <exception cref="AccountDisabledException"></exception>
        public async Task<User> GetUserInfoAsync(Guid callerId, Guid targetUserId)
        {
            ThrowIfNotReady();

            User? caller = await daemon.GetDatabaseProvider().GetUserAsync(callerId);

            if (caller == null)
                throw new EntryNotFoundException(callerId);

            if (!caller.Enabled)
                throw new AccountDisabledException(callerId);

            // Allow everyone to read user info, but not retrieve the full user list
            //if (callerId != targetUserId && !caller.Permissions.HasFlag(UserPermissions.ManageUsers))
            //    throw new InsufficientPermissionsException(callerId, "Missing permissions: ManageUsers");

            User? targetUser = await daemon.GetDatabaseProvider().GetUserAsync(targetUserId);

            if (targetUser == null)
                throw new EntryNotFoundException(targetUserId);

            return targetUser;
        }

        /// <summary>
        /// Returns all users
        /// </summary>
        /// <param name="callerId"></param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task<List<User>> GetUsersAsync(Guid callerId)
        {
            ThrowIfNotReady();

            User caller = await GetUserInfoAsync(callerId, callerId);

            if (!caller.Permissions.HasFlag(UserPermissions.ManageUsers))
                throw new UnauthorizedException(callerId, "Missing permissions: ManageUsers");

            List<User> users = await daemon.GetDatabaseProvider().GetUsersAsync();
            return users;
        }

        /// <summary>
        /// Deletes the target user
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetUserId"></param>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task DeleteUserAsync(Guid callerId, Guid targetUserId)
        {
            ThrowIfNotReady();

            User caller = await GetUserInfoAsync(callerId, callerId);

            if (IsRootUid(targetUserId))
                throw new UnauthorizedException(callerId, "Cannot remove superuser account.");

            if (!caller.Permissions.HasFlag(UserPermissions.ManageUsers))
                throw new UnauthorizedException(callerId, "Missing permissions: ManageUsers");

            User targetUser = await GetUserInfoAsync(callerId, targetUserId);
            if (targetUser.Permissions > caller.Permissions)
                throw new UnauthorizedException(callerId, "Cannot remove users with higher privilege levels.");

            await daemon.GetDatabaseProvider().DeleteUserAsync(targetUserId);

            UserDeleted?.Invoke(this, new UserDeletedEventArgs(callerId, targetUser));
        }

        /// <summary>
        /// Modifies the target user's permissions.
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetUserId"></param>
        /// <param name="permissions"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task SetUserPermissionsAsync(Guid callerId, Guid targetUserId, UserPermissions permissions)
        {
            ThrowIfNotReady();

            User caller = await GetUserInfoAsync(callerId, callerId);

            if (IsRootUid(targetUserId))
                throw new Exception("Cannot change superuser's permissions");

            if (!caller.Permissions.HasFlag(UserPermissions.Administrator))
                throw new UnauthorizedException(callerId, "Missing permissions: Administrator");

            User targetUser = await GetUserInfoAsync(callerId, targetUserId);
            User modifiedTargetUser = new User(targetUser);
            modifiedTargetUser.Permissions = permissions;
            await daemon.GetDatabaseProvider().UpdateUserAsync(modifiedTargetUser);

            UserUpdated?.Invoke(this, new UserUpdatedEventArgs(callerId, targetUser, modifiedTargetUser));
        }

        /// <summary>
        /// Changes the target user's credentials.
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetUserId"></param>
        /// <param name="newPassword"></param>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task ChangeUserPasswordAsync(Guid callerId, Guid targetUserId, string newPassword)
        {
            ThrowIfNotReady();

            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("Password cannot be empty.");

            User caller = await GetUserInfoAsync(callerId, callerId);

            if (callerId != targetUserId && !caller.Permissions.HasFlag(UserPermissions.ManageUsers))
                throw new UnauthorizedException(callerId, "Missing permissions: ManageUsers");

            User targetUser = await GetUserInfoAsync(callerId, targetUserId);

            HashValue newPasswordHash = await CryptographyProvider.Argon2Async(newPassword);
            await daemon.GetDatabaseProvider().UpdateUserPasswordAsync(targetUser.Id, newPasswordHash);

            UserCredentialsUpdated?.Invoke(this, new UserCredentialsUpdatedEventArgs(callerId, targetUser));
        }

        /// <summary>
        /// Enables or disables the target user's account
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetUserId"></param>
        /// <param name="enabled"></param>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="Exception"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task SetEnabledStateAsync(Guid callerId, Guid targetUserId, bool enabled)
        {
            ThrowIfNotReady();

            User caller = await GetUserInfoAsync(callerId, callerId);
            if (callerId != targetUserId && !caller.Permissions.HasFlag(UserPermissions.ManageUsers))
                throw new UnauthorizedException(callerId, "Missing permissions: ManageUsers");

            if (IsRootUid(targetUserId))
                throw new Exception("Cannot enable/disable the superuser account.");

            User targetUser = await GetUserInfoAsync(callerId, targetUserId);

            if (targetUser.Permissions > caller.Permissions)
                throw new UnauthorizedException(callerId, "Cannot modify users with a higher privilege level.");

            User modifiedUser = new User(targetUser);
            modifiedUser.Enabled = enabled;

            await daemon.GetDatabaseProvider().UpdateUserAsync(modifiedUser);

            UserUpdated?.Invoke(this, new UserUpdatedEventArgs(callerId, targetUser, modifiedUser));
        }

        /// <summary>
        /// Checks whether a given user ID is the superuser ID
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public bool IsRootUid(Guid guid)
        {
            return RootUserId == guid;
        }

        /// <summary>
        /// Checks whether a given user object is the superuser
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool IsRoot(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return IsRootUid(user.Id);
        }
    }
}
