using NatManager.Shared.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.Users.EventArgs
{
    public class UserCredentialsUpdatedEventArgs : System.EventArgs
    {
        public Guid? CallerSubject { get; }
        public User UpdatedUser { get; }

        public UserCredentialsUpdatedEventArgs(Guid? callerSubject, User updatedUser)
        {
            CallerSubject = callerSubject;
            UpdatedUser = updatedUser ?? throw new ArgumentNullException(nameof(updatedUser));
        }
    }
}
