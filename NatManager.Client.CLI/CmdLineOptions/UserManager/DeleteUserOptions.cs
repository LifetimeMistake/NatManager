using CommandLine;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.UserManager
{
    [Verb("delete", HelpText = "Deletes a user")]
    public class DeleteUserOptions
    {
        [Option('i', "id", HelpText = "ID of the target user", Required = false)]
        public Guid? TargetUserId { get; set; }
        [Option('u', "username", HelpText = "Username of the target user", Required = false)]
        public string? TargetUsername { get; set; }
    }
}
