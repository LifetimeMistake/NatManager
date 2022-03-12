using CommandLine;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.UserManager
{
    [Verb("list", HelpText = "Lists and filters users")]
    public class ListUsersOptions
    {
        [Option('f', "filter", HelpText = "Permissions flags to filter by", Required = false)]
        public UserPermissions? PermissionsFilter { get; set; }
        [Option('F', "full", HelpText = "Toggles full user description", Required = false)]
        public bool FullDescription { get; set; }
    }
}
