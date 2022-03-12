using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.PortMapping
{
    [Verb("delete", HelpText = "Deletes a port mapping")]
    public class DeleteMappingOptions
    {
        [Value(0, HelpText = "The mapping to delete by ID", Required = true)]
        public Guid Id { get; set; }
    }
}
