using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NatManager.Shared.Users
{
    public class User
    {
        public Guid Id;
        public string Username;
        public UserPermissions Permissions;
        public bool Enabled;
        public DateTime CreatedDate;
        public Guid CreatedBy;

        public User(Guid id, string username, UserPermissions permissions, bool enabled, DateTime creationDate, Guid createdBy)
        {
            Id = id;
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Permissions = permissions;
            Enabled = enabled;
            CreatedDate = creationDate;
            CreatedBy = createdBy;
        }

        public User(User other)
        {
            Id = other.Id;
            Username = other.Username;
            Permissions = other.Permissions;
            Enabled = other.Enabled;
            CreatedDate = other.CreatedDate;
            CreatedBy = other.CreatedBy;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public User()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        { }
    }
}
