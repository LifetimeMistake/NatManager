using CommandLine;
using NatManager.Client.CLI.CmdLineOptions.DaemonManager;
using NatManager.ClientLibrary;
using NatManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.Processors
{
    public class DaemonCommandLineProcessor : ICommandLineProcessor
    {
        public string Verb => "daemon";

        public string Description => "Manage the server daemon";
        private NatManagerClient natManagerClient;

        public DaemonCommandLineProcessor(NatManagerClient natManagerClient)
        {
            this.natManagerClient = natManagerClient;
        }

        public async Task<bool> ProcessAsync(IEnumerable<string> args)
        {
            var result = Parser.Default.ParseArguments<DaemonStatusOptions, UpdateDaemonOptions>(args);
            await result.WithParsedAsync<DaemonStatusOptions>(async (options) => await DaemonStatusOptionsAsync(options));
            await result.WithParsedAsync<UpdateDaemonOptions>(async (options) => await UpdateDaemonOptionsAsync(options));

            bool parseResult = true;
            result.WithNotParsed(errors => parseResult = false);
            return parseResult;
        }

        private async Task DaemonStatusOptionsAsync(DaemonStatusOptions options)
        {
            try
            {
                RemoteDaemonManager remoteDaemonManager = await natManagerClient.GetServiceProxyAsync<RemoteDaemonManager>();
                BehaviourMode behaviourMode = await remoteDaemonManager.GetBehaviourModeAsync();
                Console.WriteLine($"Behaviour mode: {behaviourMode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not get daemon status: {ex.Message}");
            }
        }

        private async Task UpdateDaemonOptionsAsync(UpdateDaemonOptions options)
        {
            try
            {
                RemoteDaemonManager remoteDaemonManager = await natManagerClient.GetServiceProxyAsync<RemoteDaemonManager>();
                if (!options.BehaviourMode.HasValue)
                    throw new ArgumentNullException("Missing required parameter: BehaviourMode");

                if (!options.Permanent.HasValue)
                    throw new ArgumentNullException("Missing required parameter: Permanent");

                await remoteDaemonManager.SetBehaviourModeAsync(options.BehaviourMode.Value, options.Permanent.Value);
                Console.WriteLine("Success");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not update daemon policy: {ex.Message}");
            }
        }
    }
}
