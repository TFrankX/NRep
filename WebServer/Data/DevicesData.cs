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
            Servers = new List<Server>();
            Devices = new List<Device>();
            PowerBanks = new List<PowerBank>();
        }

        public DevicesData(List<Server> servers, List<Device> devices, List<PowerBank> powerBanks)
        {
            Servers = servers;
            Devices = devices;
            PowerBanks = powerBanks;
        }

        public List<Server> Servers { get; set; }
        public List<Device> Devices { get; set; }
        public List<PowerBank> PowerBanks { get; set; }

        public void Sort()
        {
            Servers = Servers.OrderBy(c => c.Host).ToList();
            Devices = Devices.OrderBy(c => c.DeviceName).ToList();
            PowerBanks = PowerBanks.OrderBy(c => c.HostDeviceId).ToList();
        }


    }
}
