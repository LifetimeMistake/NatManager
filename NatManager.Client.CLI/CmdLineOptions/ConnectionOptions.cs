using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions
{
    public class ConnectionOptions
    {
        [Value(0, MetaName = "address", HelpText = "The NatManager server address", Required = true)]
        public string AddressText { get; set; }

        [Option('t', "timeout", Default = (uint)3000, HelpText = "The default RPC call timeout in milliseconds", Required = false)]
        public uint Timeout { get; set; }

        // This needs refactoring. The default port should be stored somewhere as a compile-time static value.
        [Option('p', "port", HelpText = "The NatManager server port", Required = false, Default = (ushort)30000)]
        public ushort Port { get; set; }

        public IPAddress Address { get { return IPAddress.Parse(AddressText); } }
        public IPEndPoint EndPoint { get { return new IPEndPoint(Address, Port); } }
    }
}
