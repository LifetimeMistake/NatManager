using NatManager.Server.Database;
using NatManager.Server.Extensions;
using NatManager.Server.PortMapping.EventArgs;
using NatManager.Server.Users;
using NatManager.Server.Users.EventArgs;
using NatManager.Shared;
using NatManager.Shared.Exceptions;
using NatManager.Shared.Networking;
using NatManager.Shared.PortMapping;
using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.PortMapping
{
    public class MappingManager : IDaemonService
    {
        private IDaemon? daemon;
        private ServiceState serviceState;

         public IDaemon? Daemon { get { return daemon; } set { daemon = value; } }
        public ServiceState State { get { return serviceState; } }

        public event EventHandler<MappingCreatedEventArgs>? MappingCreated;
        public event EventHandler<MappingUpdatedEventArgs>? MappingUpdated;
        public event EventHandler<MappingDeletedEventArgs>? MappingDeleted;

        public MappingManager(IDaemon daemon)
        {
            this.daemon = daemon;
            serviceState = ServiceState.Stopped;
        }

        public async Task StartAsync()
        {
            await StopAsync();
            ThrowIfNotReady(false);
            await CleanOrphanedMappings(false);
            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            userManager.UserDeleted += UserManager_UserDeleted;
            serviceState = ServiceState.Running;
        }

        public async Task StopAsync()
        {
            if (daemon != null)
            {
                UserManager userManager = await daemon.GetServiceAsync<UserManager>();
                userManager.UserDeleted -= UserManager_UserDeleted;
            }
            
            serviceState = ServiceState.Stopped;
        }

        [MemberNotNull(nameof(daemon))]
        public void ThrowIfNotReady(bool checkState = true)
        {
            if (daemon == null)
                throw new InvalidOperationException("Requested service is not attached to a daemon instance");

            if (serviceState != ServiceState.Running && checkState)
                throw new InvalidOperationException("Requested service or it's dependency is not available");
        }

        private void UserManager_UserDeleted(object? sender, UserDeletedEventArgs eventArgs)
        {
            CleanOrphanedMappings().FireAndForgetSafeAsync(daemon?.GetErrorHandler());
        }

        private async Task CleanOrphanedMappings(bool checkState = true)
        {
            ThrowIfNotReady(checkState);
            IDatabaseProvider databaseProvider = daemon.GetDatabaseProvider();
            List<User> users = await databaseProvider.GetUsersAsync();
            List<ManagedMapping> mappings = await databaseProvider.GetPortMappingsAsync();
            foreach (ManagedMapping mapping in mappings.Where(mapping => !users.Any(user => user.Id == mapping.OwnerId)))
            {
                await databaseProvider.DeletePortMappingAsync(mapping.Id);
                MappingDeleted?.Invoke(this, new MappingDeletedEventArgs(null, mapping));
            }
        }

        /// <summary>
        /// Creates a managed port mapping
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="ownerId"></param>
        /// <param name="proto"></param>
        /// <param name="privatePort"></param>
        /// <param name="publicPort"></param>
        /// <param name="privateMAC"></param>
        /// <param name="description"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="PortUnavailableException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task<ManagedMapping> CreateMappingAsync(Guid callerId, Guid ownerId, Protocol proto, ushort privatePort, ushort publicPort, MACAddress privateMAC, string description, bool enabled)
        {
            ThrowIfNotReady();

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            User caller = await userManager.GetUserInfoAsync(callerId, callerId);

            if (callerId != ownerId && !caller.Permissions.HasFlag(UserPermissions.ManageMappings))
                throw new UnauthorizedException(callerId);

            if (!PhysicalAddressHelper.ValidateAddress(privateMAC))
                throw new ArgumentException("Invalid MAC address");

            if (description == null)
                description = "";

            if (await IsPublicPortUsedAsync(publicPort, proto))
                throw new PortUnavailableException(publicPort, "Port is currently in use.");

            ManagedMapping portMapping = new ManagedMapping(Guid.NewGuid(), ownerId, proto, privatePort, publicPort, privateMAC, description, enabled, DateTime.UtcNow, callerId);
            await daemon.GetDatabaseProvider().InsertPortMappingAsync(portMapping);

            MappingCreated?.Invoke(this, new MappingCreatedEventArgs(callerId, portMapping));
            return portMapping;
        }

        /// <summary>
        /// Deletes a managed mapping
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetMappingId"></param>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task DeleteMappingAsync(Guid callerId, Guid targetMappingId)
        {
            ThrowIfNotReady();

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            User caller = await userManager.GetUserInfoAsync(callerId, callerId);

            ManagedMapping targetMapping = await GetMappingInfoAsync(callerId, targetMappingId);

            if (callerId != targetMapping.OwnerId && !caller.Permissions.HasFlag(UserPermissions.ManageMappings))
                throw new UnauthorizedException(callerId);

            await daemon.GetDatabaseProvider().DeletePortMappingAsync(targetMappingId);
                MappingDeleted?.Invoke(this, new MappingDeletedEventArgs(callerId, targetMapping));
        }

        /// <summary>
        /// Sets a new owner ID for the specified managed mapping
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetMappingId"></param>
        /// <param name="newOwner"></param>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task SetMappingOwnerAsync(Guid callerId, Guid targetMappingId, Guid newOwner)
        {
            ThrowIfNotReady();

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            User caller = await userManager.GetUserInfoAsync(callerId, callerId);

            if (callerId != newOwner && !caller.Permissions.HasFlag(UserPermissions.ManageMappings))
                throw new UnauthorizedException(callerId);

            ManagedMapping targetMapping = await GetMappingInfoAsync(callerId, targetMappingId);
            ManagedMapping newMapping = new ManagedMapping(targetMapping);
            newMapping.OwnerId = newOwner;

            await daemon.GetDatabaseProvider().UpdatePortMappingAsync(newMapping);
                MappingUpdated?.Invoke(this, new MappingUpdatedEventArgs(callerId, targetMapping, newMapping));
        }

        /// <summary>
        /// Updates most parameters of a managed mapping
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetMappingId"></param>
        /// <param name="proto"></param>
        /// <param name="privatePort"></param>
        /// <param name="publicPort"></param>
        /// <param name="privateMAC"></param>
        /// <param name="description"></param>
        /// <param name="enabled"></param>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="PortUnavailableException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task UpdateMappingConfigAsync(Guid callerId, Guid targetMappingId, Protocol proto, ushort privatePort, ushort publicPort, MACAddress privateMAC, string description, bool enabled)
        {
            ThrowIfNotReady();

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            User caller = await userManager.GetUserInfoAsync(callerId, callerId);

            ManagedMapping targetMapping = await GetMappingInfoAsync(callerId, targetMappingId);

            if (callerId != targetMapping.OwnerId && !caller.Permissions.HasFlag(UserPermissions.ManageMappings))
                throw new UnauthorizedException(callerId);

            if (!PhysicalAddressHelper.ValidateAddress(privateMAC))
                throw new ArgumentException("Invalid MAC address");

            if (targetMapping.PublicPort != publicPort && await IsPublicPortUsedAsync(publicPort, proto))
                throw new PortUnavailableException(publicPort, "Port is currently in use.");

            if (description == null)
                description = "";

            ManagedMapping newMapping = new ManagedMapping(targetMapping);
            newMapping.PrivatePort = privatePort;
            newMapping.PublicPort = publicPort;
            newMapping.PrivateMAC = privateMAC;
            newMapping.Protocol = proto;
            newMapping.Description = description;
            newMapping.Enabled = enabled;

            await daemon.GetDatabaseProvider().UpdatePortMappingAsync(newMapping);
            MappingUpdated?.Invoke(this, new MappingUpdatedEventArgs(callerId, targetMapping, newMapping));
        }

        /// <summary>
        /// Updates a managed mapping's description
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetMappingId"></param>
        /// <param name="description"></param>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="PortUnavailableException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task UpdateMappingDescriptionAsync(Guid callerId, Guid targetMappingId, string description)
        {
            ManagedMapping mapping = await GetMappingInfoAsync(callerId, targetMappingId);
            await UpdateMappingConfigAsync(callerId, targetMappingId, mapping.Protocol, mapping.PrivatePort, mapping.PublicPort, mapping.PrivateMAC, description, mapping.Enabled);
        }

        /// <summary>
        /// Updates a managed mapping's public port
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetMappingId"></param>
        /// <param name="proto"></param>
        /// <param name="publicPort"></param>
        public async Task UpdateMappingPublicPortAsync(Guid callerId, Guid targetMappingId, Protocol proto, ushort publicPort)
        {
            ManagedMapping mapping = await GetMappingInfoAsync(callerId, targetMappingId);
            await UpdateMappingConfigAsync(callerId, targetMappingId, proto, mapping.PrivatePort, publicPort, mapping.PrivateMAC, mapping.Description, mapping.Enabled);
        }

        /// <summary>
        /// Updates a managed mapping's private port
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetMappingId"></param>
        /// <param name="privatePort"></param>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="PortUnavailableException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task UpdateMappingPrivatePortAsync(Guid callerId, Guid targetMappingId, ushort privatePort)
        {
            ManagedMapping mapping = await GetMappingInfoAsync(callerId, targetMappingId);
            await UpdateMappingConfigAsync(callerId, targetMappingId, mapping.Protocol, privatePort, mapping.PublicPort, mapping.PrivateMAC, mapping.Description, mapping.Enabled);
        }

        /// <summary>
        /// Updates a managed mapping's target MAC address
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetMappingId"></param>
        /// <param name="privateMAC"></param>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="PortUnavailableException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task UpdateMappingMACAddressAsync(Guid callerId, Guid targetMappingId, MACAddress privateMAC)
        {
            ManagedMapping mapping = await GetMappingInfoAsync(callerId, targetMappingId);
            await UpdateMappingConfigAsync(callerId, targetMappingId, mapping.Protocol, mapping.PrivatePort, mapping.PublicPort, privateMAC, mapping.Description, mapping.Enabled);
        }

        /// <summary>
        /// Enables or disables the target managed mapping
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetMappingId"></param>
        /// <param name="enabled"></param>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="PortUnavailableException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task UpdateMappingEnabledAsync(Guid callerId, Guid targetMappingId, bool enabled)
        {
            ManagedMapping mapping = await GetMappingInfoAsync(callerId, targetMappingId);
            await UpdateMappingConfigAsync(callerId, targetMappingId, mapping.Protocol, mapping.PrivatePort, mapping.PublicPort, mapping.PrivateMAC, mapping.Description, enabled);
        }

        /// <summary>
        /// Returns information about a managed mapping object ID
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetMappingId"></param>
        /// <returns></returns>
        /// <exception cref="DatabaseException"></exception>
        /// <exception cref="EntryNotFoundException"></exception>
        /// <exception cref="UnauthorizedException"></exception>
        public async Task<ManagedMapping> GetMappingInfoAsync(Guid callerId, Guid targetMappingId)
        {
            ThrowIfNotReady();

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            User caller = await userManager.GetUserInfoAsync(callerId, callerId);

            ManagedMapping? mapping = await daemon.GetDatabaseProvider().GetPortMappingAsync(targetMappingId);

            if (mapping == null)
                throw new EntryNotFoundException(targetMappingId);

            if (mapping.OwnerId != callerId && !caller.Permissions.HasFlag(UserPermissions.ManageMappings))
                throw new UnauthorizedException(caller.Id);

            return mapping;
        }

        /// <summary>
        /// Returns all managed mapping objects
        /// </summary>
        /// <param name="callerId"></param>
        /// <returns></returns>
        /// <exception cref="EntryNotFoundException"></exception>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task<List<ManagedMapping>> GetAllMappingsAsync(Guid callerId)
        {
            ThrowIfNotReady();

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            User caller = await userManager.GetUserInfoAsync(callerId, callerId);

            if (!caller.Permissions.HasFlag(UserPermissions.ManageMappings))
                throw new UnauthorizedException(caller.Id);

            List<ManagedMapping> mappings = await daemon.GetDatabaseProvider().GetPortMappingsAsync();
            return mappings;
        }

        /// <summary>
        /// Returns a given user's managed mappings
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="targetUserId"></param>
        /// <returns></returns>
        /// <exception cref="EntryNotFoundException"></exception>
        /// <exception cref="UnauthorizedException"></exception>
        /// <exception cref="DatabaseException"></exception>
        public async Task<List<ManagedMapping>> GetUsersMappingsAsync(Guid callerId, Guid targetUserId)
        {
            ThrowIfNotReady();

            UserManager userManager = await daemon.GetServiceAsync<UserManager>();
            User caller = await userManager.GetUserInfoAsync(callerId, callerId);

            if (callerId != targetUserId && !caller.Permissions.HasFlag(UserPermissions.ManageMappings))
                throw new UnauthorizedException(callerId);

            List<ManagedMapping> mappings = await daemon.GetDatabaseProvider().GetPortMappingsAsync(targetUserId);
            return mappings;
        }

        /// <summary>
        /// Checks whether another mapping already exists for the given protocol and port
        /// </summary>
        /// <param name="publicPort"></param>
        /// <param name="proto"></param>
        /// <returns></returns>
        /// <exception cref="DatabaseException"></exception>
        public async Task<bool> IsPublicPortUsedAsync(ushort publicPort, Protocol proto)
        {
            ThrowIfNotReady();

            List<ManagedMapping> mappings = await daemon.GetDatabaseProvider().GetPortMappingsAsync();
            return mappings.Any(mapping => mapping.PublicPort == publicPort && mapping.Protocol == proto);
        }
    }
}
