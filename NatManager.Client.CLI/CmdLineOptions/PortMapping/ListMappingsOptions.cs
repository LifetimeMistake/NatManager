using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.PortMapping
{
    [Verb("list")]
    public class ListMappingsOptions
    {
        [Option('a', "all", HelpText = "Shows all mappings, including the ones not belonging to the user (required ManageMappings permissions)", Required = false)]
        public bool ShowAll { get; set; }
        [Option('F', "full", HelpText = "Toggles full mapping description", Required = false)]
        public bool FullDescription { get; set; }
        [Option('i', "id", HelpText = "ID of the target user", Required = false)]
        public Guid? TargetUserId { get; set; }
        [Option('u', "username", HelpText = "Username of the target user", Required = false)]
        public string? TargetUsername { get; set; }
    }
}
