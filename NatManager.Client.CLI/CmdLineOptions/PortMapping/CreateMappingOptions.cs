using CommandLine;
using NatManager.Shared.PortMapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.PortMapping
{
    [Verb("create", HelpText = "Creates a new port mapping")]
    public class CreateMappingOptions
    {
        [Option('p', "protocol", HelpText = "Protocol to use with the mapping (TCP/UDP)", Required = true)]
        public Protocol Protocol { get; set; }
        [Option('i', "privatePort", HelpText = "The private port to bind", Required = true)]
        public ushort PrivatePort { get; set; }
        [Option('e', "publicPort", HelpText = "The public port to bind", Required = true)]
        public ushort PublicPort { get; set; }
        [Option('a', "address", HelpText = "The MAC address to bind", Required = true)]
        public string PrivateMAC { get; set; }
        [Option('d', "description", Default = "", HelpText = "The mapping's description", Required = false)]
        public string Description { get; set; }
        [Option('E', "enable", Default = true, HelpText = "Sets the mapping's initial state (active true/false)", Required = false)]
        public bool? Enabled { get; set; }
    }
}
