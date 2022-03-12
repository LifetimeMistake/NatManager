using CommandLine;
using NatManager.Client.CLI.CmdLineOptions.UserManager;
using NatManager.Client.CLI.Tables;
using NatManager.ClientLibrary;
using NatManager.ClientLibrary.Users;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.Processors
{
    public class UserCommandLineProcessor : ICommandLineProcessor
    {
        public string Verb { get => "users"; }
        public string Description { get => "User management"; }
        private NatManagerClient natManagerClient;
        public UserCommandLineProcessor(NatManagerClient natManagerClient)
        {
            this.natManagerClient = natManagerClient ?? throw new ArgumentNullException(nameof(natManagerClient));
        }

        public async Task<bool> ProcessAsync(IEnumerable<string> args)
        {
            var result = Parser.Default.ParseArguments<CreateUserOptions, DeleteUserOptions, ListUsersOptions, DetailsUserOptions, UpdateUserOptions>(args);
            await result.WithParsedAsync<CreateUserOptions>(async (options) => await CreateUserOptionsAsync(options));
            await result.WithParsedAsync<DeleteUserOptions>(async (options) => await DeleteUserOptionsAsync(options));
            await result.WithParsedAsync<ListUsersOptions>(async (options) => await ListUsersOptionsAsync(options));
            await result.WithParsedAsync<DetailsUserOptions>(async (options) => await DetailsUserOptionsAsync(options));
            await result.WithParsedAsync<UpdateUserOptions>(async (options) => await ModifyUserOptionsAsync(options));

            bool parseResult = true;
            result.WithNotParsed(errors => parseResult = false);
            return parseResult;
        }

        private async Task CreateUserOptionsAsync(CreateUserOptions options)
        {
            try
            {
                RemoteUserManager remoteUserManager = await natManagerClient.GetServiceProxyAsync<RemoteUserManager>();
                string password = ReadLine.ReadPassword("New password: ");
                string passwordConfirm = ReadLine.ReadPassword("Confirm password: ");
                if (password != passwordConfirm)
                    throw new ArgumentException("The passwords you have entered do not match");

                if(!options.Enabled.HasValue)
                    throw new ArgumentNullException("Missing required parameter: Enabled");

                User user = await remoteUserManager.CreateUserAsync(options.Username, password, options.Enabled.Value);
                Console.WriteLine($"User created: {user.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create user: {ex.Message}");
            }
        }

        private async Task DeleteUserOptionsAsync(DeleteUserOptions options)
        {
            try
            {
                RemoteUserManager remoteUserManager = await natManagerClient.GetServiceProxyAsync<RemoteUserManager>();
                Guid targetUserId;
                if (options.TargetUserId == null)
                {
                    if (options.TargetUsername == null)
                        throw new ArgumentNullException("Did not specify a target ID or a username");

                    User? currentIdentity = natManagerClient.GetCurrentIdentity();
                    if (currentIdentity != null && currentIdentity.Username == options.TargetUsername)
                    {
                        targetUserId = currentIdentity.Id;
                    }
                    else
                    {
                        User[] userList = await remoteUserManager.GetUserListAsync();
                        User? targetUser = userList.FirstOrDefault(user => user.Username == options.TargetUsername);
                        if (targetUser == null)
                            throw new ArgumentNullException("Could not find user by username");

                        targetUserId = targetUser.Id;
                    }
                }
                else
                {
                    targetUserId = options.TargetUserId.Value;
                }

                await remoteUserManager.DeleteUserAsync(targetUserId);
                Console.WriteLine($"User deleted: {targetUserId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete user: {ex.Message}");
            }
        }

        private async Task ListUsersOptionsAsync(ListUsersOptions options)
        {
            try
            {
                RemoteUserManager remoteUserManager = await natManagerClient.GetServiceProxyAsync<RemoteUserManager>();
                User[] userList = await remoteUserManager.GetUserListAsync();

                if (options.PermissionsFilter.HasValue)
                    userList = userList.Where(u => u.Permissions.HasFlag(options.PermissionsFilter.Value)).ToArray();

                TableBuilder tableBuilder = new TableBuilder();
                
                if(options.FullDescription)
                {
                    tableBuilder.AddHeader("Id", "Username", "Permissions", "Enabled", "Creation Date", "Created By");

                    foreach (User user in userList)
                    {
                        List<UserPermissions> userPermissions = new List<UserPermissions>();
                        foreach (UserPermissions permission in Enum.GetValues(typeof(UserPermissions)))
                        {
                            if (user.Permissions.HasFlag(permission))
                                userPermissions.Add(permission);
                        }

                        string permissions = string.Join(",", userPermissions);
                        tableBuilder.AddRow(user.Id, user.Username, permissions, user.Enabled, user.CreatedDate, user.CreatedBy);
                    }
                }
                else
                {
                    tableBuilder.AddHeader("Id", "Username", "Permissions", "Enabled", "Creation Date");

                    foreach (User user in userList)
                    {
                        tableBuilder.AddRow(user.Id, user.Username, user.Permissions, user.Enabled, user.CreatedDate);
                    }
                }

                Console.WriteLine();
                Console.WriteLine(tableBuilder.Output());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to list users: {ex.Message}");
            }
        }

        private async Task DetailsUserOptionsAsync(DetailsUserOptions options)
        {
            try
            {
                RemoteUserManager remoteUserManager = await natManagerClient.GetServiceProxyAsync<RemoteUserManager>();
                Guid targetUserId;
                if (options.TargetUserId == null)
                {
                    if (options.TargetUsername == null)
                        throw new ArgumentNullException("Did not specify a target ID or a username");

                    User? currentIdentity = natManagerClient.GetCurrentIdentity();
                    if (currentIdentity != null && currentIdentity.Username == options.TargetUsername)
                    {
                        targetUserId = currentIdentity.Id;
                    }
                    else
                    {
                        User[] userList = await remoteUserManager.GetUserListAsync();
                        User? targetUser = userList.FirstOrDefault(user => user.Username == options.TargetUsername);
                        if (targetUser == null)
                            throw new ArgumentNullException("Could not find user by username");

                        targetUserId = targetUser.Id;
                    }
                }
                else
                {
                    targetUserId = options.TargetUserId.Value;
                }

                User user = await remoteUserManager.GetUserInfoAsync(targetUserId);
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Id: {user.Id}");
                stringBuilder.AppendLine($"Username: {user.Username}");
                List<UserPermissions> userPermissions = new List<UserPermissions>();
                foreach (UserPermissions permission in Enum.GetValues(typeof(UserPermissions)))
                {
                    if (user.Permissions.HasFlag(permission))
                        userPermissions.Add(permission);
                }
                stringBuilder.AppendLine($"Permissions: {string.Join(", ", userPermissions)}");
                stringBuilder.AppendLine($"Enabled: {user.Enabled}");
                stringBuilder.AppendLine($"Creation date: {user.CreatedDate}");
                stringBuilder.AppendLine($"Created by: {user.CreatedBy}");
                Console.WriteLine(stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get user description: {ex.Message}");
            }
        }

        private async Task ModifyUserOptionsAsync(UpdateUserOptions options)
        {
            try
            {
                RemoteUserManager remoteUserManager = await natManagerClient.GetServiceProxyAsync<RemoteUserManager>();
                Guid targetUserId;
                if (options.TargetUserId == null)
                {
                    if (options.TargetUsername == null)
                        throw new ArgumentNullException("Did not specify a target ID or a username");

                    User? currentIdentity = natManagerClient.GetCurrentIdentity();
                    if (currentIdentity != null && currentIdentity.Username == options.TargetUsername)
                    {
                        targetUserId = currentIdentity.Id;
                    }
                    else
                    {
                        User[] userList = await remoteUserManager.GetUserListAsync();
                        User? targetUser = userList.FirstOrDefault(user => user.Username == options.TargetUsername);
                        if (targetUser == null)
                            throw new ArgumentNullException("Could not find user by username");

                        targetUserId = targetUser.Id;
                    }
                }
                else
                {
                    targetUserId = options.TargetUserId.Value;
                }

                if (options.PasswordRequired)
                {
                    string password = ReadLine.ReadPassword("New password: ");
                    string passwordConfirm = ReadLine.ReadPassword("Confirm password: ");
                    if (password != passwordConfirm)
                        throw new ArgumentException("The passwords you have entered do not match.");

                    try
                    {
                        await remoteUserManager.ChangeUserCredentialsAsync(targetUserId, password);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to change password: {ex.Message}");
                        return;
                    }
                }

                if (options.Permissions.HasValue)
                {
                    try
                    {
                        await remoteUserManager.SetUserPermissionsAsync(targetUserId, options.Permissions.Value);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Failed to set permissions: {ex.Message}");
                        return;
                    }
                }

                if (options.Enabled.HasValue)
                {
                    try
                    {
                        await remoteUserManager.SetEnabledStateAsync(targetUserId, options.Enabled.Value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to change enabled state: {ex.Message}");
                        return;
                    }
                }

                Console.WriteLine("Success");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to modify user: {ex.Message}");
            }
        }
    }
}
