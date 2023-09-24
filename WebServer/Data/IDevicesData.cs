using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebServer.Models.Device;


namespace WebServer.Data
{
    public interface IDevicesData
    {
        List<Server> Servers { get; set; }
        List<Device> Devices { get; set; }
        List<PowerBank> PowerBanks { get; set; }
    }
}
