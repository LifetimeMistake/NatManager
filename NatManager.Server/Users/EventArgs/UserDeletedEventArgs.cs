using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.Users.EventArgs
{
    public class UserDeletedEventArgs : System.EventArgs
    {
        public Guid? CallerSubject { get; }
        public User DeletedUser { get; }

        public UserDeletedEventArgs(Guid? callerSubject, User deletedUser)
        {
            CallerSubject = callerSubject;
            DeletedUser = deletedUser ?? throw new ArgumentNullException(nameof(deletedUser));
        }
    }
}
