using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace WebServer.Models.Device
{
    public class Device
    {

        public Device(string deviceName,ulong hostDeviceId, bool online)
        { 
            Id = GetGUID(deviceName);
            DeviceName = deviceName;
            HostDeviceId = hostDeviceId;
            Online = online;
            Activated = false;
            Error = "";
            //LastOnlineTime = lastOnlineTime;
            if (online)
            {
                LastOnlineTime = DateTime.Now;
                FirstOnlineTime = DateTime.Now;
            }
            Slots = 0;
            DevMainServer = "";
            DevResServer = "";
            //PowerBanks = powerBanks;
            LastUpdate = DateTime.Now;
            ActivateTime = DateTime.MinValue;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public ulong Id { get; set; }
        [NotMapped]
        public string Id_str 
        {
            get { return Id.ToString(); }
        }
        public string DeviceName { get; set; }
        public ulong HostDeviceId { get; set; }
        [NotMapped]
        public string HostDeviceId_str 
        {          
            get { return HostDeviceId.ToString(); } 
        }
        public bool Online { get; set; }
        public bool Activated { get; set; }
        public uint Slots {  get; set; }
        public string IP { get; set; } = "";
        public string DevMainServer { get; set; }
        public string DevResServer { get; set; }
        //public PowerBank[] PowerBanks { get; set; }
        public string Error { get; set; }
        public DateTime ActivateTime { get; set; }
        public DateTime LastOnlineTime { get; set; }
        public DateTime FirstOnlineTime { get; set; }
        public DateTime LastUpdate { get; private set; }
        [NotMapped]
        public bool Init { get; set; }

        [NotMapped]
        public bool Stored { get; set; }

        private ulong GetGUID(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            Byte[] bId = new Guid(hash).ToByteArray();
            ulong result = BitConverter.ToUInt64(bId, 0);
            return result;
        }


    }




}
