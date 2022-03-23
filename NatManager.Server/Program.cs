using CommandLine;
using NatManager.Server.Configuration;
using NatManager.Server.Database;
using NatManager.Server.Logging;
using NatManager.Server.NAT;
using NatManager.Server.Networking;
using NatManager.Server.PortMapping;
using NatManager.Server.RPC;
using NatManager.Server.Users;
using NatManager.Shared.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            ServerLaunchOptions? serverLaunchOptions = null;
            Parser.Default.ParseArguments<ServerLaunchOptions>(args)
                .WithParsed(options => serverLaunchOptions = options);

            if (serverLaunchOptions == null)
                return;

            if (!File.Exists(serverLaunchOptions.ConfigPath))
            {
                Console.WriteLine($"Configuration file not found at \"{Path.GetFullPath(serverLaunchOptions.ConfigPath)}\".");
                return;
            }

            DatabaseCredentials databaseCredentials;
            uint databaseRetryInterval;
            string logsDirectory;

            try
            {
                ConfigurationFile configFile = new ConfigurationFile(serverLaunchOptions.ConfigPath);
                string hostname = GetConfigValueAssert(configFile, "dbHostname");
                uint port;
                if (!uint.TryParse(GetConfigValueAssert(configFile, "dbPort"), out port))
                    throw new ArgumentException("Invalid port type");

                string username = GetConfigValueAssert(configFile, "dbUser");
                string password = GetConfigValueAssert(configFile, "dbPassword");
                string databaseName = GetConfigValueAssert(configFile, "dbName");

                if (!uint.TryParse(GetConfigValueAssert(configFile, "dbConnectionRetryInterval"), out databaseRetryInterval))
                    throw new ArgumentException("Invalid retry interval");

                logsDirectory = GetConfigValueAssert(configFile, "logsOutputDirectory");
                databaseCredentials = new DatabaseCredentials(hostname, port, username, password, databaseName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            SqlDatabase sqlDatabase = new SqlDatabase(databaseCredentials);
            while(sqlDatabase.State != DatabaseState.Connected)
            {
                try
                {
                    if (!await sqlDatabase.StartAsync())
                        throw new Exception();
                }
                catch
                {
                    Console.WriteLine("Failed to connect to the database... Retrying...");
                    await Task.Delay((int)databaseRetryInterval);
                }
            }

            Console.WriteLine("Database connection opened");
            
            // SimpleLogger simpleLogger = new SimpleLogger(logsDirectory);
            ConsoleLogger consoleLogger = new ConsoleLogger();
            Daemon natManagerDaemon = new Daemon(sqlDatabase, consoleLogger, consoleLogger);
            await natManagerDaemon.AddServiceAsync(new ActionLoggingService(natManagerDaemon));
            await natManagerDaemon.AddServiceAsync(new DaemonManager(natManagerDaemon));
            await natManagerDaemon.AddServiceAsync(new UserManager(natManagerDaemon));
            await natManagerDaemon.AddServiceAsync(new MappingManager(natManagerDaemon));
            await natManagerDaemon.AddServiceAsync(new NatDiscovererService(natManagerDaemon));
            await natManagerDaemon.AddServiceAsync(new NetworkMapperService(natManagerDaemon));
            await natManagerDaemon.AddServiceAsync(new PortMappingWatchdog(natManagerDaemon));
            await natManagerDaemon.AddServiceAsync(new AutoTcpRpcServer(natManagerDaemon));
            await natManagerDaemon.StartAsync();

            // Block until the NatManager server exits
            while (natManagerDaemon.State == Shared.ServiceState.Running)
                await Task.Delay(250);

            await sqlDatabase.StopAsync();
            Console.WriteLine("Database connection closed");
        }
        
        private static string GetConfigValueAssert(ConfigurationFile configFile, string key)
        {
            if (!configFile.HasConfigKey(key))
                throw new ArgumentNullException($"Required config key missing: {key}");

            return configFile.GetConfigValue(key);
        }
    }
}
