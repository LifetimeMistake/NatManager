using CommandLine;
using NatManager.Client.CLI.CmdLineOptions.PortMapping;
using NatManager.Client.CLI.Tables;
using NatManager.ClientLibrary;
using NatManager.ClientLibrary.PortMapping;
using NatManager.ClientLibrary.Users;
using NatManager.Shared.Networking;
using NatManager.Shared.PortMapping;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.Processors
{
    public class MappingCommandLineProcessor : ICommandLineProcessor
    {
        public string Verb { get => "mappings"; }
        public string Description { get => "Port mapping management"; }
        private NatManagerClient natManagerClient;
        public MappingCommandLineProcessor(NatManagerClient natManagerClient)
        {
            this.natManagerClient = natManagerClient ?? throw new ArgumentNullException(nameof(natManagerClient));
        }

        public async Task<bool> ProcessAsync(IEnumerable<string> args)
        {
            var result = Parser.Default.ParseArguments<CreateMappingOptions, DeleteMappingOptions, ListMappingsOptions, DetailsMappingOptions, UpdateMappingOptions, SetMappingOwnerOptions>(args);
            await result.WithParsedAsync<CreateMappingOptions>(async (options) => await CreateMappingOptionsAsync(options));
            await result.WithParsedAsync<DeleteMappingOptions>(async (options) => await DeleteMappingOptionsAsync(options));
            await result.WithParsedAsync<ListMappingsOptions>(async (options) => await ListMappingsOptionsAsync(options));
            await result.WithParsedAsync<DetailsMappingOptions>(async (options) => await DetailsMappingOptionsAsync(options));
            await result.WithParsedAsync<UpdateMappingOptions>(async (options) => await UpdateMappingOptionsAsync(options));
            await result.WithParsedAsync<SetMappingOwnerOptions>(async (options) => await SetMappingOwnerOptionsAsync(options));

            bool parseResult = true;
            result.WithNotParsed(errors => parseResult = false);
            return parseResult;
        }

        public async Task CreateMappingOptionsAsync(CreateMappingOptions options)
        {
            try
            {
                RemoteMappingManager remoteMappingManager = await natManagerClient.GetServiceProxyAsync<RemoteMappingManager>();
                MACAddress physicalAddress = PhysicalAddressHelper.GetAddressObject(options.PrivateMAC);

                User? user = natManagerClient.GetCurrentIdentity();
                if (user == null)
                    throw new ArgumentException("Local user is not logged in.");

                if (!options.Enabled.HasValue)
                    throw new ArgumentNullException(nameof(options.Enabled));

                ManagedMapping mapping = await remoteMappingManager.CreateMappingAsync(user.Id, options.Protocol, options.PrivatePort, options.PublicPort, physicalAddress, options.Description, options.Enabled.Value);
                Console.WriteLine($"Mapping created: {mapping.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create mapping: {ex.Message}");
            }
        }

        public async Task DeleteMappingOptionsAsync(DeleteMappingOptions options)
        {
            try
            {
                RemoteMappingManager remoteMappingManager = await natManagerClient.GetServiceProxyAsync<RemoteMappingManager>();
                await remoteMappingManager.DeleteMappingAsync(options.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete mapping: {ex.Message}");
            }
        }

        public async Task ListMappingsOptionsAsync(ListMappingsOptions options)
        {
            try
            {
                RemoteMappingManager remoteMappingManager = await natManagerClient.GetServiceProxyAsync<RemoteMappingManager>();
                ManagedMapping[] mappings;

                if (options.ShowAll)
                    mappings = await remoteMappingManager.GetAllMappingsAsync();
                else if (options.TargetUserId != null || options.TargetUsername != null)
                {
                    Guid targetUserId;
                    if (options.TargetUserId == null)
                    {
                        RemoteUserManager remoteUserManager = await natManagerClient.GetServiceProxyAsync<RemoteUserManager>();
                        User[] userList = await remoteUserManager.GetUserListAsync();
                        User? targetUser = userList.FirstOrDefault(user => user.Username == options.TargetUsername);
                        if (targetUser == null)
                            throw new ArgumentNullException("Could not find user by username");

                        targetUserId = targetUser.Id;
                    }
                    else
                    {
                        targetUserId = options.TargetUserId.Value;
                    }

                    mappings = await remoteMappingManager.GetUsersMappingsAsync(targetUserId);
                }
                else
                {
                    User? user = natManagerClient.GetCurrentIdentity();
                    if (user == null)
                        throw new ArgumentNullException("Could not obtain local identity, specify a target user");

                    mappings = await remoteMappingManager.GetUsersMappingsAsync(user.Id);
                }

                TableBuilder tableBuilder = new TableBuilder();
                

                if (options.FullDescription)
                {
                    tableBuilder.AddHeader("Id", "Owner Id", "Protocol", "PrivatePort", "Public Port", "Private MAC", "Enabled", "Description", "Created By", "Creation Date");
                    foreach (ManagedMapping mapping in mappings)
                    {
                        tableBuilder.AddRow(mapping.Id, mapping.OwnerId, mapping.Protocol, mapping.PrivatePort, mapping.PublicPort, 
                            PhysicalAddressHelper.AddressToString(mapping.PrivateMAC), mapping.Enabled, mapping.Description, mapping.CreatedBy, mapping.CreatedDate);
                    }
                }
                else
                {
                    tableBuilder.AddHeader("Id", "Owner Id", "Protocol", "PrivatePort", "Public Port", "Enabled", "Description");
                    foreach (ManagedMapping mapping in mappings)
                    {
                        tableBuilder.AddRow(mapping.Id, mapping.OwnerId, mapping.Protocol, mapping.PrivatePort, mapping.PublicPort, mapping.Enabled, mapping.Description);
                    }
                }

                Console.WriteLine();
                Console.WriteLine(tableBuilder.Output());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to list mappings: {ex.Message}");
            }
        }

        public async Task DetailsMappingOptionsAsync(DetailsMappingOptions options)
        {
            try
            {
                RemoteMappingManager remoteMappingManager = await natManagerClient.GetServiceProxyAsync<RemoteMappingManager>();
                ManagedMapping mapping = await remoteMappingManager.GetMappingInfoAsync(options.Id);

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Id: {mapping.Id}");
                stringBuilder.AppendLine($"Owner Id: {mapping.OwnerId}");
                stringBuilder.AppendLine($"Protocol: {mapping.Protocol}");
                stringBuilder.AppendLine($"Private Port: {mapping.PrivatePort}");
                stringBuilder.AppendLine($"Public Port: {mapping.PublicPort}");
                stringBuilder.AppendLine($"Private MAC: {PhysicalAddressHelper.AddressToString(mapping.PrivateMAC)}");
                stringBuilder.AppendLine($"Enabled: {mapping.Enabled}");
                stringBuilder.AppendLine($"Description: {mapping.Description}");
                stringBuilder.AppendLine($"Created By: {mapping.CreatedBy}");
                stringBuilder.AppendLine($"Created Date: {mapping.CreatedDate}");
                Console.WriteLine(stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get mapping description: {ex.Message}");
            }
        }

        public async Task UpdateMappingOptionsAsync(UpdateMappingOptions options)
        {
            try
            {
                RemoteMappingManager remoteMappingManager = await natManagerClient.GetServiceProxyAsync<RemoteMappingManager>();
                ManagedMapping mapping = await remoteMappingManager.GetMappingInfoAsync(options.Id);
                
                if(options.Protocol.HasValue)
                    mapping.Protocol = options.Protocol.Value;
                if(options.PrivatePort.HasValue)
                    mapping.PrivatePort = options.PrivatePort.Value;
                if(options.PublicPort.HasValue)
                    mapping.PublicPort = options.PublicPort.Value;
                if(options.PrivateMAC != null)
                {
                    MACAddress physicalAddress = PhysicalAddressHelper.GetAddressObject(options.PrivateMAC);
                    mapping.PrivateMAC = physicalAddress;
                }
                if(options.Description != null)
                    mapping.Description = options.Description;
                if(options.Enabled.HasValue)
                    mapping.Enabled = options.Enabled.Value;

                await remoteMappingManager.UpdateMappingConfigAsync(options.Id, mapping.Protocol, mapping.PrivatePort, mapping.PublicPort, mapping.PrivateMAC, mapping.Description, mapping.Enabled);
                Console.WriteLine("Success");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to modify mapping: {ex.Message}");
            }
        }

        public async Task SetMappingOwnerOptionsAsync(SetMappingOwnerOptions options)
        {
            try
            {
                RemoteMappingManager remoteMappingManager = await natManagerClient.GetServiceProxyAsync<RemoteMappingManager>();
                RemoteUserManager remoteUserManager = await natManagerClient.GetServiceProxyAsync<RemoteUserManager>();
                Guid targetUserId;
                if (options.TargetUserId == null)
                {
                    if (options.TargetUsername == null)
                        throw new ArgumentNullException("Did not specify a target ID or a username");

                    User? currentIdentity = natManagerClient.GetCurrentIdentity();
                    if (currentIdentity != null && currentIdentity.Username == options.TargetUsername)
                    {
                        targetUserId = currentIdentity.Id;
                    }
                    else
                    {
                        User[] userList = await remoteUserManager.GetUserListAsync();
                        User? targetUser = userList.FirstOrDefault(user => user.Username == options.TargetUsername);
                        if (targetUser == null)
                            throw new ArgumentNullException("Could not find user by username");

                        targetUserId = targetUser.Id;
                    }
                }
                else
                {
                    targetUserId = options.TargetUserId.Value;
                }

                await remoteMappingManager.SetMappingOwnerAsync(options.Id, targetUserId);
                Console.WriteLine($"Transferred mapping ownership to {targetUserId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to transfer ownership: {ex.Message}");
            }
        }
    }
}
