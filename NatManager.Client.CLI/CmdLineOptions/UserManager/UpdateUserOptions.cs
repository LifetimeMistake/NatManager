using CommandLine;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.UserManager
{
    [Verb("update", HelpText = "Modifies the target user")]
    public class UpdateUserOptions
    {
        [Option('i', "id", HelpText = "ID of the target user", Required = false)]
        public Guid? TargetUserId { get; set; }
        [Option('u', "username", HelpText = "Username of the target user", Required = false)]
        public string? TargetUsername { get; set; }
        [Option('p', "permissions", HelpText = "Permission flags to apply to the user", Required = false)]
        public UserPermissions? Permissions { get; set; }
        [Option('P', "password", HelpText = "Specifies whether to display the password change prompt.", Required = false)]
        public bool PasswordRequired { get; set; }
        [Option('e', "enabled", HelpText = "Enables or disables the account.", Required = false)]
        public bool? Enabled { get; set; }
    }
}
