using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Shared.Networking
{
    [Serializable]
    public class MACAddress
    {
        private byte[] addressBytes;
        public byte[] AddressBytes { get { return addressBytes; } }
        
        public MACAddress(byte[] addressBytes)
        {
            this.addressBytes = addressBytes;
        }

        public byte[] GetAddressBytes()
        {
            return addressBytes;
        }

        public static MACAddress Parse(string address)
        {
            return new MACAddress(PhysicalAddress.Parse(address).GetAddressBytes());
        }

        public static bool TryParse(string address, [NotNullWhen(true)] out MACAddress? macAddress)
        {
            PhysicalAddress? physicalAddress;
            if (!PhysicalAddress.TryParse(address, out physicalAddress))
            {
                macAddress = null;
                return false;
            }

            macAddress = new MACAddress(physicalAddress.GetAddressBytes());
            return true;
        }

        public override string ToString()
        {
            return BitConverter.ToString(addressBytes).Replace("-", "");
        }

        public static implicit operator MACAddress(PhysicalAddress address) => new MACAddress(address.GetAddressBytes());
        public static implicit operator PhysicalAddress(MACAddress address) => new PhysicalAddress(address.GetAddressBytes());
    }
}
