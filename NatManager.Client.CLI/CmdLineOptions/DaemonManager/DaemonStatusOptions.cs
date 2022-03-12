using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.DaemonManager
{
    [Verb("status", HelpText = "Prints the server's current port mapping enforcement policy")]
    public class DaemonStatusOptions
    {
    }
}
