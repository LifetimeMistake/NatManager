using CommandLine;
using CommandLine.Text;
using Microsoft.VisualStudio.Threading;
using NatManager.Client.CLI.CmdLineOptions;
using NatManager.Client.CLI.CmdLineOptions.UserManager;
using NatManager.Client.CLI.Processors;
using NatManager.Client.CLI.Tables;
using NatManager.ClientLibrary;
using NatManager.ClientLibrary.RPC;
using NatManager.ClientLibrary.Users;
using NatManager.Shared.Users;
using Nerdbank.Streams;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.Client.CLI
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            ConnectionOptions? connectionOptions = null;
            Parser.Default.ParseArguments<ConnectionOptions>(args)
                .WithParsed(options => connectionOptions = options);

            if (connectionOptions == null)
            {
                return;
            }

            TcpRpcClient tcpRpcClient = new TcpRpcClient(connectionOptions.EndPoint);
            NatManagerClient natManagerClient = new NatManagerClient(tcpRpcClient, (int)connectionOptions.Timeout);

            TaskCompletionSource connectedTaskSource = new TaskCompletionSource();
            TaskCompletionSource disconnectedTaskSource = new TaskCompletionSource();

            var connectedHandler = new EventHandler<ClientLibrary.RPC.EventArgs.RpcConnectedEventArgs>((s, e) =>
            {
                connectedTaskSource.SetResult();
            });

            var disconnectedHandler = new EventHandler((s, e) =>
            {
                Console.WriteLine("Lost connection to the NatManager server.");
                disconnectedTaskSource.SetResult();
            });

            natManagerClient.RpcClient.Connected += connectedHandler;
            natManagerClient.RpcClient.Disconnected += disconnectedHandler;

            if (!await natManagerClient.StartAsync())
            {
                Console.WriteLine("Failed to connect to the NatManager server.");
                return;
            }

            await Task.WhenAny(connectedTaskSource.Task, Task.Delay((int)connectionOptions.Timeout));
            if (!connectedTaskSource.Task.IsCompleted)
            {
                Console.WriteLine("Connection timed out.");
                return;
            }

            if(!await AuthenticateUserAsync(natManagerClient, connectionOptions))
            {
                Console.WriteLine("Login failed.");
                return;
            }

            await Task.WhenAny(RunCommandLineAsync(natManagerClient), disconnectedTaskSource.Task);

            natManagerClient.RpcClient.Connected -= connectedHandler;
            natManagerClient.RpcClient.Disconnected -= disconnectedHandler;

            await natManagerClient.StopAsync();
        }

        static async Task<bool> AuthenticateUserAsync(NatManagerClient natManagerClient, ConnectionOptions connectionOptions)
        {
            string? username;
            string? password;

            Console.Write("Username: ");
            username = Console.ReadLine();
            if (string.IsNullOrEmpty(username))
            {
                Console.WriteLine("Username cannot be empty.");
                return false;
            }

            for(int i = 0; i < 3; i++)
            {
                Console.Write("Password: ");
                password = ReadLine.ReadPassword();
                try
                {
                    await natManagerClient.AuthenticateClientAsync(username, password);
                    username = "";
                    password = "";
                    return true;
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error, please try again: {ex.Message}");
                }
            }

            Console.WriteLine("Too many failed login attempts.");
            return false;
        }

        static async Task RunCommandLineAsync(NatManagerClient natManagerClient)
        {
            User? identity = natManagerClient.GetCurrentIdentity();
            string identityUsername = identity != null ? identity.Username : "ANONYMOUS";

            Dictionary<string, ICommandLineProcessor> processors = new Dictionary<string, ICommandLineProcessor>();
            UserCommandLineProcessor userCommandLineProcessor = new UserCommandLineProcessor(natManagerClient);
            DaemonCommandLineProcessor daemonCommandLineProcessor = new DaemonCommandLineProcessor(natManagerClient);
            MappingCommandLineProcessor mappingCommandLineProcessor = new MappingCommandLineProcessor(natManagerClient);
            NetworkHostManagerCommandLineProcessor networkHostManagerCommandLineProcessor = new NetworkHostManagerCommandLineProcessor(natManagerClient);
            processors.Add(userCommandLineProcessor.Verb, userCommandLineProcessor);
            processors.Add(daemonCommandLineProcessor.Verb, daemonCommandLineProcessor);
            processors.Add(mappingCommandLineProcessor.Verb, mappingCommandLineProcessor);
            processors.Add(networkHostManagerCommandLineProcessor.Verb, networkHostManagerCommandLineProcessor);

            ReadLine.HistoryEnabled = true;

            while (true)
            {
                string? command = ReadLine.Read($"{identityUsername}@NatManager> ");
                ReadLine.AddHistory(command);

                if (string.IsNullOrWhiteSpace(command))
                    continue;

                IEnumerable<string> args = SplitCommandLineArguments.SplitArgs(command);

                string verb = args.First().ToLower();

                if (verb == "exit" || verb == "logout")
                {
                    Console.WriteLine("Exiting interactive session");
                    return;
                }

                if (verb == "clear")
                {
                    Console.Clear();
                    continue;
                }

                if (verb == "help" || !processors.ContainsKey(verb))
                {
                    TableBuilder tableBuilder = new TableBuilder();
                    foreach(KeyValuePair<string, ICommandLineProcessor> p in processors)
                        tableBuilder.AddRow("  " + p.Value.Verb, "\t" + p.Value.Description);

                    tableBuilder.AddRow("exit, logout", "Exits the interactive session");
                    tableBuilder.AddRow("clear", "Cleans the terminal window");
                    tableBuilder.AddRow("help", "Displays this help prompt");

                    Console.WriteLine();
                    Console.WriteLine(tableBuilder.Output());
                    continue;
                }

                ICommandLineProcessor processor = processors[verb];
                if (!await processor.ProcessAsync(args.Skip(1)))
                    Console.WriteLine("Syntax error");
            }
        }
    }
}
