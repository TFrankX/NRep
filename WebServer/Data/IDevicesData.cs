using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebServer.Models.Device;


namespace WebServer.Data
{
    public interface IDevicesData
    {
        ThreadSafeList<Server> Servers { get; }
        ThreadSafeList<Device> Devices { get; }
        ThreadSafeList<PowerBank> PowerBanks { get; }
    }
}
