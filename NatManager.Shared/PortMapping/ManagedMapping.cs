using NatManager.Shared.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NatManager.Shared.PortMapping
{
    public class ManagedMapping
    {
        public Guid Id;
        public Guid OwnerId;
        public Protocol Protocol;
        public ushort PrivatePort;
        public ushort PublicPort;
        public MACAddress PrivateMAC;
        public string Description;
        public bool Enabled;
        public DateTime CreatedDate;
        public Guid CreatedBy;

        public ManagedMapping(Guid id, Guid ownerId, Protocol protocol, ushort privatePort, ushort publicPort, MACAddress privateMAC, string description, bool enabled, DateTime creationDate, Guid createdBy)
        {
            Id = id;
            OwnerId = ownerId;
            Protocol = protocol;
            PrivatePort = privatePort;
            PublicPort = publicPort;
            PrivateMAC = privateMAC ?? throw new ArgumentNullException(nameof(privateMAC));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Enabled = enabled;
            CreatedDate = creationDate;
            CreatedBy = createdBy;
        }

        public ManagedMapping(ManagedMapping other)
        {
            Id = other.Id;
            OwnerId = other.OwnerId;
            Protocol = other.Protocol;
            PrivatePort = other.PrivatePort;
            PublicPort = other.PublicPort;
            PrivateMAC = other.PrivateMAC;
            Description = other.Description;
            Enabled = other.Enabled;
            CreatedDate = other.CreatedDate;
            CreatedBy = other.CreatedBy;
        }

        [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ManagedMapping()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        { }
    }
}
