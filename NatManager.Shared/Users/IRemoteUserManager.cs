using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Shared.Users
{
    public interface IRemoteUserManager
    {
        Task<User> CreateUserAsync(string username, string password, bool enabled);
        Task<User[]> GetUserListAsync();
        Task<User> GetUserInfoAsync(Guid targetUserId);
        Task DeleteUserAsync(Guid targetUserId);
        Task SetUserPermissionsAsync(Guid targetUserId, UserPermissions userPermissions);
        Task ChangeUserCredentialsAsync(Guid targetUserId, string newPassword);
        Task SetEnabledStateAsync(Guid targetUserId, bool enabled);
    }
}
