using System;
using System.Collections.Generic;
using System.Text;

namespace NatManager.Server.Cryptography
{
    public class HashValue
    {
        public byte[] Hash;
        public byte[] Salt;

        public HashValue(byte[] hash, byte[] salt)
        {
            Hash = hash ?? throw new ArgumentNullException(nameof(hash));
            Salt = salt ?? throw new ArgumentNullException(nameof(salt));
        }
    }
}
