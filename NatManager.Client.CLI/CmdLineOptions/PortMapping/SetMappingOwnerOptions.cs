using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.PortMapping
{
    [Verb("chown", HelpText = "Changes the mapping's owner")]
    public class SetMappingOwnerOptions
    {
        [Value(0, HelpText = "The mapping to modify by ID", Required = true)]
        public Guid Id { get; set; }
        [Option('i', "id", HelpText = "ID of the target user", Required = false)]
        public Guid? TargetUserId { get; set; }
        [Option('u', "username", HelpText = "Username of the target user", Required = false)]
        public string? TargetUsername { get; set; }
    }
}
