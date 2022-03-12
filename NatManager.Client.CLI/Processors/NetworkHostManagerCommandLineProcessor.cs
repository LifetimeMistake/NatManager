using CommandLine;
using NatManager.Client.CLI.CmdLineOptions.NetworkHostManager;
using NatManager.Client.CLI.Tables;
using NatManager.ClientLibrary;
using NatManager.ClientLibrary.Networking;
using NatManager.Shared.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.Processors
{
    public class NetworkHostManagerCommandLineProcessor : ICommandLineProcessor
    {
        public string Verb => "network";

        public string Description => "Manage the physical network";
        private NatManagerClient natManagerClient;

        public NetworkHostManagerCommandLineProcessor(NatManagerClient natManagerClient)
        {
            this.natManagerClient = natManagerClient;
        }

        public async Task<bool> ProcessAsync(IEnumerable<string> args)
        {
            var result = Parser.Default.ParseArguments<ListNetworkHostsOptions>(args);
            await result.WithParsedAsync<ListNetworkHostsOptions>(async (options) => await ListNetworkHostsOptionsAsync(options));
            bool parseResult = true;
            result.WithNotParsed(errors => parseResult = false);
            return parseResult;
        }

        private async Task ListNetworkHostsOptionsAsync(ListNetworkHostsOptions options)
        {
            try
            {
                RemoteNetworkHostManager remoteNetworkHostManager = await natManagerClient.GetServiceProxyAsync<RemoteNetworkHostManager>();
                NetworkHost[] networkHosts = await remoteNetworkHostManager.GetAllHostsAsync();
                TableBuilder tableBuilder = new TableBuilder();
                tableBuilder.AddHeader("Hostname", "IP Address", "Physical Address");

                foreach (NetworkHost host in networkHosts)
                {
                    string hostname = host.Hostname != null && host.Hostname != host.IPAddress.ToString() ? host.Hostname : "<unknown>";
                    tableBuilder.AddRow(hostname, host.IPAddress, PhysicalAddressHelper.AddressToString(host.PhysicalAddress));
                }

                Console.WriteLine(tableBuilder.Output());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not get network host list: {ex.Message}");
            }
        }
    }
}
