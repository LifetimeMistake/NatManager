using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.UserManager
{
    [Verb("create", HelpText = "Creates a user")]
    public class CreateUserOptions
    {
        [Option('u', "username", Required = true)]
        public string Username { get; set; }
        [Option('e', "enabled", Default = true, Required = false)]
        public bool? Enabled { get; set; }
    }
}
