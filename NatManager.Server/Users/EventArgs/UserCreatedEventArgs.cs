using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.Users.EventArgs
{
    public class UserCreatedEventArgs : System.EventArgs
    {
        public Guid? CallerSubject { get; }
        public User CreatedUser { get; }

        public UserCreatedEventArgs(Guid? callerSubject, User createdUser)
        {
            CallerSubject = callerSubject;
            CreatedUser = createdUser ?? throw new ArgumentNullException(nameof(createdUser));
        }
    }
}
