using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NatManager.Shared.Networking
{
    public static class PhysicalAddressHelper
    {
        public static bool ValidateAddress(MACAddress physicalAddress)
        {
            if (physicalAddress == null)
                throw new ArgumentNullException(nameof(physicalAddress));

            return (physicalAddress.GetAddressBytes().Length == 6);
        }

        public static bool ValidateAddress(string macAddress)
        {
            if (macAddress == null)
                throw new ArgumentNullException(nameof(macAddress));

            return Regex.IsMatch(macAddress, "^(?:[0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}|(?:[0-9a-fA-F]{2}-){5}[0-9a-fA-F]{2}|(?:[0-9a-fA-F]{2}){5}[0-9a-fA-F]{2}$");
        }

        public static string AddressToString(MACAddress physicalAddress, string separator = "-")
        {
            if (physicalAddress == null)
                throw new ArgumentNullException(nameof(physicalAddress));

            return AddressToString(physicalAddress.GetAddressBytes(), separator);
        }

        public static string AddressToString(byte[] macArray, string separator = "-")
        {
            if (macArray == null)
                throw new ArgumentNullException(nameof(macArray));

            if (separator == null)
                throw new ArgumentNullException(nameof(separator));

            if (separator.Length != 1)
                throw new ArgumentException("MAC separator must be 1 character long.");

            if (macArray.Length != 6)
                throw new ArgumentException($"MAC address must be 6 octects long.");

            string[] macOctets = new string[6];
            for (int i = 0; i < macArray.Length; i++)
                macOctets[i] = macArray[i].ToString("X2");

            return string.Join(separator, macOctets);
        }

        public static byte[] GetAddressBytes(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
                throw new ArgumentNullException(nameof(macAddress));

            string macAddressCleaned = Regex.Replace(macAddress, "[::-]", "");
            if (macAddressCleaned.Length != 12)
                throw new ArgumentException("MAC address must be 6 octects long.");

            byte[] macArray = new byte[6];
            for (int i = 0; i < macArray.Length; i++)
                macArray[i] = Convert.ToByte($"{macAddressCleaned[i*2]}{macAddressCleaned[i*2 + 1]}", 16);

            return macArray;
        }

        public static MACAddress GetAddressObject(string macAddress)
        {
            return new MACAddress(GetAddressBytes(macAddress));
        }

        public static bool AddressEqual(byte[] addressA, byte[] addressB)
        {
            if (addressA == null)
                throw new ArgumentNullException(nameof(addressA));

            if (addressB == null)
                throw new ArgumentNullException(nameof(addressB));

            for (int i = 0; i < addressA.Length; i++)
                if (addressA[i] != addressB[i])
                    return false;

            return true;
        }

        public static bool AddressEqual(MACAddress addressA, MACAddress addressB)
        {
            return AddressEqual(addressA.GetAddressBytes(), addressB.GetAddressBytes());
        }

        public static bool AddressEqual(PhysicalAddress addressA, PhysicalAddress addressB)
        {
            return AddressEqual(addressA.GetAddressBytes(), addressB.GetAddressBytes());
        }

        public static bool AddressEqual(PhysicalAddress addressA, MACAddress addressB)
        {
            return AddressEqual(addressA.GetAddressBytes(), addressB.GetAddressBytes());
        }

        public static bool AddressEqual(MACAddress addressA, PhysicalAddress addressB)
        {
            return AddressEqual(addressA.GetAddressBytes(), addressB.GetAddressBytes());
        }
    }
}
