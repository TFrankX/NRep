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
            Price = 12;
            Reserved = false;
            LastUpdate = DateTime.Now;
            UserId = "";
            UpdateInt = false;
            UpdateExt = false;
            PaymentInfo = "";
            SessionId = "";
        }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public ulong Id { get; set; }
        [NotMapped]
        public string Id_str
        {
            get { return Id.ToString(); }
        }
        [NotMapped]
        public string Name
        {
            get { return DecodePowerBankId(Id); }
        }

        /// <summary>
        /// Decodes PowerBank ID to human-readable name.
        /// ID format: first 4 bytes = ASCII prefix (e.g. "GDVF"), last 4 bytes = serial (hex digits as decimal)
        /// Example: 5135324334914536226 (0x4744564645000322) -> "GDVF45000322"
        /// </summary>
        private static string DecodePowerBankId(ulong id)
        {
            try
            {
                // Convert to bytes (big-endian)
                byte[] bytes = BitConverter.GetBytes(id);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);

                // First 4 bytes: ASCII prefix
                var prefix = new StringBuilder();
                for (int i = 0; i < 4 && i < bytes.Length; i++)
                {
                    if (bytes[i] >= 65 && bytes[i] <= 90) // A-Z
                        prefix.Append((char)bytes[i]);
                }

                // Last 4 bytes: each byte's hex representation becomes 2 decimal digits
                var serial = new StringBuilder();
                for (int i = 4; i < bytes.Length; i++)
                {
                    serial.Append(bytes[i].ToString("X2"));
                }

                return $"{prefix}{serial}";
            }
            catch
            {
                return id.ToString();
            }
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
        public float Cost { get; set; }  // Стоимость текущей/последней аренды
        public float TotalEarnings { get; set; }  // Накопительный доход от этого PowerBank
        public DateTime LastUpdate { get; set; }
        public bool Taken { get; set; }
        public string UserId { get; set; }
        public bool UpdateInt { get; set; }
        public bool UpdateExt { get; set; }
        public bool Reserved { get; set; }
        public DateTime? ReserveTime { get; set; }
        public string PaymentInfo { get; set; }
        public string SessionId { get; set; }
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
