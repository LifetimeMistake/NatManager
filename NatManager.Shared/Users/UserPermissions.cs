using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Shared.Users
{
    public enum UserPermissions
    {
        Standard = 0,
        ManageUsers = 1,
        ManageMappings = 2,
        ManageSettings = 4,
        ManageDaemon = 8,
        ManageNetwork = 16,
        Administrator = 31
    }
}
