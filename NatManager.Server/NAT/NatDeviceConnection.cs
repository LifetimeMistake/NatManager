using Open.Nat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatManager.Server.NAT
{
    public class NatDeviceConnection
    {
        private readonly NatDevice Device;

        public NatDeviceConnection(NatDevice device)
        {
            Device = device;
        }

        public static async Task<NatDeviceConnection?> DiscoverDeviceAsync(PortMapper protocol = PortMapper.Pmp | PortMapper.Upnp, int timeout = 3000)
        {
            try
            {
                NatDiscoverer discoverer = new NatDiscoverer();
                NatDevice device = await discoverer.DiscoverDeviceAsync(protocol, new CancellationTokenSource(timeout));
                NatDeviceConnection natManager = new NatDeviceConnection(device);
                return natManager;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IPAddress GetClientInternalIP()
        {
            return Device.LocalAddress;
        }

        public IPAddress GetInternalIP()
        {
            return Device.HostEndPoint.Address;
        }

        public async Task<IPAddress> GetExternalIPAsync()
        {
            return await Device.GetExternalIPAsync();
        }
        public async Task CreatePortMappingAsync(Mapping mapping)
        {
            await Device.CreatePortMapAsync(mapping);
        }
        public async Task<IEnumerable<Mapping>> GetMappingsAsync()
        {
            IEnumerable<Mapping> mappings = await Device.GetAllMappingsAsync();
            return mappings;
        }
        public async Task<IEnumerable<Mapping>> GetMappingsAsync(Func<Mapping, bool> predicate)
        {
            return (await GetMappingsAsync()).Where(predicate);
        }

        public async Task DeletePortMappingAsync(Mapping mapping)
        {
            await Device.DeletePortMapAsync(mapping);
        }
    }
}
