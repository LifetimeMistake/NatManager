using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace NatManager.Server.Cryptography
{
    public static class CryptographyProvider
    {
        public static HashValue Argon2(string input)
        {
            byte[] salt = CreateSalt();
            return Argon2(input, salt);
        }

        public static async Task<HashValue> Argon2Async(string input)
        {
            byte[] salt = CreateSalt();
            return await Argon2Async(input, salt);
        }

        public static HashValue Argon2(string input, byte[] salt)
        {
            Argon2id argon2 = new Argon2id(Encoding.UTF8.GetBytes(input));
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = Environment.ProcessorCount;
            argon2.Iterations = 4;
            argon2.MemorySize = 131072;

            byte[] buffer = argon2.GetBytes(16);
            return new HashValue(buffer, salt);
        }

        public static async Task<HashValue> Argon2Async(string input, byte[] salt)
        {
            Argon2id argon2 = new Argon2id(Encoding.UTF8.GetBytes(input));
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = Environment.ProcessorCount;
            argon2.Iterations = 4;
            argon2.MemorySize = 131072;

            byte[] buffer = await argon2.GetBytesAsync(16);
            return new HashValue(buffer, salt);
        }

        public static byte[] CreateSalt()
        {
            byte[] buffer = new byte[16];
            RandomNumberGenerator cryptoServiceProvider = RandomNumberGenerator.Create();
            cryptoServiceProvider.GetBytes(buffer);
            return buffer;
        }

        public static bool HashEqual(HashValue a, HashValue b)
        {
            return a.Hash.SequenceEqual(b.Hash);
        }

        public static bool HashEqual(string input, HashValue hash)
        {
            HashValue newHash = Argon2(input, hash.Salt);
            return HashEqual(newHash, hash);
        }

        public static async Task<bool> HashEqualAsync(string input, HashValue hash)
        {
            HashValue newHash = await Argon2Async(input, hash.Salt);
            return HashEqual(input, newHash);
        }
    }
}
