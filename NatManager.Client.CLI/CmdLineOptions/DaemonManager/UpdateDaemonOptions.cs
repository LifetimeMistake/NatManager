using CommandLine;
using NatManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.CmdLineOptions.DaemonManager
{
    [Verb("update")]
    public class UpdateDaemonOptions
    {
        [Value(0, MetaName = "mode", HelpText = "Sets the port mapping manager enforcement policy", Required = true)]
        public BehaviourMode? BehaviourMode { get; set; } 
        [Value(1, MetaName = "permanent", HelpText = "Specifies whether the change should survive a server restart", Required = true)]
        public bool? Permanent { get; set; }
    }
}
