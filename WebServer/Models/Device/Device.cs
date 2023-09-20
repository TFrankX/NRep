using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.Device
{
    public class Device
    {

        public Device() { }

        public Device(ulong id,ulong hostDeviceId, bool online, uint error, DateTime lastOnlineTime, DateTime firstOnlineTime, string ip, string devMainServer, string devResServer)
        {
            Id = id;
            HostDeviceId = hostDeviceId;
            Online = online;
            Error = error;
            LastOnlineTime = lastOnlineTime;
            FirstOnlineTime = firstOnlineTime;
            IP = ip;
            DevMainServer = devMainServer;
            DevResServer = devResServer;
            //PowerBanks = powerBanks;
            LastUpdate = DateTime.Now;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public ulong Id { get; set; }
        public ulong HostDeviceId { get; set; }
        public bool Online { get; set; }
        public uint Error { get; set; }
        public DateTime LastOnlineTime { get; set; }
        public DateTime FirstOnlineTime { get; set; }
        public string IP { get; set; }
        public string DevMainServer { get; set; }
        public string DevResServer { get; set; }
        //public PowerBank[] PowerBanks { get; set; }
        public DateTime LastUpdate { get; private set; }

    }



    
}
