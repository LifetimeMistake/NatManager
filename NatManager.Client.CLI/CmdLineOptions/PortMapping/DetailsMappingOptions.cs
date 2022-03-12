using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.PortMapping
{
    [Verb("details", HelpText = "Prints a detailed description of a port mapping")]
    public class DetailsMappingOptions
    {
        [Value(0, HelpText = "The mapping to show by ID", Required = true)]
        public Guid Id { get; set; }
    }
}
