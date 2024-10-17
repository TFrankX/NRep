using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace WebServer.Models.Device
{
    public class PowerBank
    {
        public PowerBank(ulong id, string hostDeviceName, uint hostSlot, bool locked, bool plugged, bool charging, PowerBankChargeLevel chargeLevel)
        {
            Id = id;
            HostDeviceId = GetGUID(hostDeviceName);
            HostDeviceName = hostDeviceName;
            HostSlot = hostSlot;
            Locked = locked;
            Plugged = plugged;
            Restricted = false;
            Charging = charging;
            ChargeLevel = chargeLevel;
            LastGetTime = DateTime.MinValue;
            LastPutTime = DateTime.Now;
            ClientTime = DateTimeOffset.MinValue;
            Price = 0;
            LastUpdate = DateTime.Now;
            UserId = "";
        }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public ulong Id { get; set; }
        [NotMapped]
        public string Id_str 
        {
            get { return Id.ToString(); }
        }

        //public ulong HostDeviceId { get; set; }
        public string HostDeviceName { get; set; }
        [NotMapped]
        public ulong HostDeviceId { get { return GetGUID(HostDeviceName); } set { } }
        [NotMapped]
        public string HostDeviceId_str 
        {
            get { return GetGUID(HostDeviceName).ToString();}           
        }
        public uint HostSlot { get; set; }
        public bool Locked { get; set; }
        public bool Plugged { get; set; }
        public bool Charging { get; set; }
        public bool IsOk { get; set; }
        public bool Restricted { get; set; }
        public PowerBankChargeLevel ChargeLevel { get; set; }
        public DateTime LastGetTime { get; set; }
        public DateTime LastPutTime { get; set; }        
        public DateTimeOffset ClientTime { get; set; }
        public float Price { get; set; }
        public float Cost { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool Taken { get; set; }
        public string UserId { get; set; }
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
