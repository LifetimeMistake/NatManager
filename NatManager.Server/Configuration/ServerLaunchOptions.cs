using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.Configuration
{
    public class ServerLaunchOptions
    {
        [Option('f', "config", HelpText = "Path to the server launch config.", Required = false, Default = "server.ini")]
        public string ConfigPath { get; set; }
    }
}
