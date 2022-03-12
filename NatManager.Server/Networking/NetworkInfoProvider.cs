using ArpLookup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.Networking
{
    public static class NetworkInfoProvider
    {
        public static bool HostUp(IPAddress address)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(address);
                if (reply.Status != IPStatus.Success)
                    return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static async Task<bool> HostUpAsync(IPAddress address)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = await ping.SendPingAsync(address);
                if (reply.Status != IPStatus.Success)
                    return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static async Task<string?> GetDeviceHostnameAsync(IPAddress address)
        {
            try
            {
                IPHostEntry entry = await Dns.GetHostEntryAsync(address);
                if (entry.HostName == address.ToString())
                    return null;

                return entry.HostName;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static PhysicalAddress? ResolveMACAddressFromIP(IPAddress address, int timeoutMs = 250)
        {
            try
            {
                if (address == null)
                    return null;

                if (!Arp.IsSupported)
                    return null;

                Arp.LinuxPingTimeout = TimeSpan.FromMilliseconds(timeoutMs);
                return Arp.Lookup(address);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<PhysicalAddress?> ResolveMACAddressFromIPAsync(IPAddress address, int timeoutMs = 250)
        {
            try
            {
                if (address == null)
                    return null;

                if (!Arp.IsSupported)
                    return null;

                Arp.LinuxPingTimeout = TimeSpan.FromMilliseconds(timeoutMs);
                return await Arp.LookupAsync(address);
            }
            catch
            {
                return null;
            }
        }

        public static IPAddress? GetSubnetMaskFromInterfaceAddress(IPAddress address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            UnicastIPAddressInformation? unicastIPAddressInformation = NetworkInterface.GetAllNetworkInterfaces().Select(i => i.GetIPProperties().UnicastAddresses)
                .SelectMany(a => a).Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault(a => a.Address.Equals(address));

            return (unicastIPAddressInformation != null) ? unicastIPAddressInformation.IPv4Mask : null;
        }

        public static IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Addresses must be of equal length.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }

        public static IPAddress GetNetworkAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Addresses must be of equal length.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
            }
            return new IPAddress(broadcastAddress);
        }

        public static bool IsInSameSubnet(IPAddress addressA, IPAddress addressB, IPAddress subnetMask)
        {
            return GetNetworkAddress(addressA, subnetMask) == GetNetworkAddress(addressB, subnetMask);
        }

        public static IEnumerable<IPAddress> EnumerateIPRange(IPAddress startIP, IPAddress endIP)
        {
            if (startIP == null)
                throw new ArgumentNullException(nameof(startIP));

            if (endIP == null)
                throw new ArgumentNullException(nameof(endIP));

            byte[] startIPBytes = startIP.GetAddressBytes();
            byte[] endIPBytes = endIP.GetAddressBytes();

            if (startIPBytes.Length != endIPBytes.Length)
                throw new ArgumentException("Addresses must be of equal length.");

            uint startIPNumber = BitConverter.ToUInt32(startIPBytes.Reverse().ToArray(), 0);
            uint endIPNumber = BitConverter.ToUInt32(endIPBytes.Reverse().ToArray(), 0);

            for (uint i = startIPNumber; i < endIPNumber + 1; i++)
            {
                yield return new IPAddress(BitConverter.GetBytes(i).Reverse().ToArray());
            }
        }
    }
}
