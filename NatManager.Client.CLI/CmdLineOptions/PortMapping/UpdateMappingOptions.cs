using CommandLine;
using NatManager.Shared.PortMapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.PortMapping
{
    [Verb("update", HelpText = "Modifies a port mapping")]
    public class UpdateMappingOptions
    {
        [Value(0, HelpText = "The mapping to modify by ID", Required = true)]
        public Guid Id { get; set; }
        [Option('p', "protocol", HelpText = "Protocol to use with the mapping (Tcp/Udp)", Required = false)]
        public Protocol? Protocol { get; set; }
        [Option('i', "privatePort", HelpText = "The private port to bind", Required = false)]
        public ushort? PrivatePort { get; set; }
        [Option('e', "publicPort", HelpText = "The public port to bind", Required = false)]
        public ushort? PublicPort { get; set; }
        [Option('a', "address", HelpText = "The MAC address to bind", Required = false)]
        public string? PrivateMAC { get; set; }
        [Option('d', "description", HelpText = "The mapping's description", Required = false)]
        public string? Description { get; set; }
        [Option('E', "enable", HelpText = "Sets the mapping's initial state (active true/false)", Required = false)]
        public bool? Enabled { get; set; }
    }
}
