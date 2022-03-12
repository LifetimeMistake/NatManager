using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.UserManager
{
    [Verb("details", HelpText = "Prints a detailed description of a user")]
    public class DetailsUserOptions
    {
        [Option('i', "id", HelpText = "ID of the target user", Required = false)]
        public Guid? TargetUserId { get; set; }
        [Option('u', "username", HelpText = "Username of the target user", Required = false)]
        public string? TargetUsername { get; set; }
    }
}
