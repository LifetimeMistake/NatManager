using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Shared.Users
{
    public static class UserManagerRpcMethods
    {
        public const string CreateUser = "CreateUser";
        public const string GetUserList = "GetUserList";
        public const string GetUserInfo = "GetUserInfo";
        public const string DeleteUser = "DeleteUser";
        public const string SetUserPermissions = "SetUserPermissions";
        public const string ChangeUserCredentials = "ChangeUserCredentials";
        public const string SetEnabledState = "SetEnabledState";
    }
}
