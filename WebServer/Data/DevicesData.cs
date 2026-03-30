using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebServer.Models.Device;

namespace WebServer.Data
{
    public class DevicesData : IDevicesData
    {
        public DevicesData()
        {
            Servers = new ThreadSafeList<Server>(s => s.Id);
            Devices = new ThreadSafeList<Device>(d => d.Id);
            PowerBanks = new ThreadSafeList<PowerBank>(p => p.Id);
        }

        public DevicesData(List<Server>? servers, List<Device>? devices, List<PowerBank>? powerBanks)
        {
            Servers = new ThreadSafeList<Server>(s => s.Id);
            Devices = new ThreadSafeList<Device>(d => d.Id);
            PowerBanks = new ThreadSafeList<PowerBank>(p => p.Id);

            if (servers != null)
                foreach (var s in servers) Servers.Add(s);
            if (devices != null)
                foreach (var d in devices) Devices.Add(d);
            if (powerBanks != null)
                foreach (var p in powerBanks) PowerBanks.Add(p);
        }

        public ThreadSafeList<Server> Servers { get; }
        public ThreadSafeList<Device> Devices { get; }
        public ThreadSafeList<PowerBank> PowerBanks { get; }

        public List<Server> GetServersSorted() => Servers.ToList().OrderBy(c => c.Host).ToList();
        public List<Device> GetDevicesSorted() => Devices.ToList().OrderBy(c => c.DeviceName).ToList();
        public List<PowerBank> GetPowerBanksSorted() => PowerBanks.ToList().OrderBy(c => c.HostDeviceName).ToList();
    }
}
