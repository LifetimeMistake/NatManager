using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Shared.Networking
{
    [Serializable]
    public class NetworkAddress
    {
        private byte[] addressBytes;
        private IPAddress ipAddress;
        public byte[] AddressBytes { get { return addressBytes; } }

        public NetworkAddress(byte[] addressBytes)
        {
            this.ipAddress = new IPAddress(addressBytes);
            this.addressBytes = addressBytes;
        }

        public byte[] GetAddressBytes()
        {
            return addressBytes;
        }

        public NetworkAddress Parse(string address)
        {
            return new NetworkAddress(IPAddress.Parse(address).GetAddressBytes());
        }

        public bool TryParse(string address, out NetworkAddress? networkAddress)
        {
            IPAddress? ipAddress;
            if (!IPAddress.TryParse(address, out ipAddress))
            {
                networkAddress = null;
                return false;
            }

            networkAddress = new NetworkAddress(ipAddress.GetAddressBytes());
            return true;
        }

        public override string ToString()
        {
            return ipAddress.ToString();
        }

        public override bool Equals(object? obj)
        {
            return ipAddress.Equals(obj);
        }

        public override int GetHashCode()
        {
            return ipAddress.GetHashCode();
        }

        public static implicit operator IPAddress(NetworkAddress address) => new IPAddress(address.GetAddressBytes());
        public static implicit operator NetworkAddress(IPAddress address) => new NetworkAddress(address.GetAddressBytes());
    }
}
