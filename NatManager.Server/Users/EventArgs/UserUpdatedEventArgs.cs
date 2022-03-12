using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.Users.EventArgs
{
    public class UserUpdatedEventArgs : System.EventArgs
    {
        public Guid? CallerSubject { get; }
        public User OldUser { get; }
        public User UpdatedUser { get; }

        public UserUpdatedEventArgs(Guid? callerSubject, User oldUser, User updatedUser)
        {
            CallerSubject = callerSubject;
            OldUser = oldUser ?? throw new ArgumentNullException(nameof(oldUser));
            UpdatedUser = updatedUser ?? throw new ArgumentNullException(nameof(updatedUser));
        }
    }
}
