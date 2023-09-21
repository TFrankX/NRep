using System;

namespace WebServer.Models.Device
{
    public class PowerBank
    {
        public PowerBank(ulong id, ulong hostDeviceId, bool locked, bool plugged, bool charging, PowerBankChargeLevel chargeLevel)
        {
            Id = id;
            HostDeviceId = hostDeviceId;
            Locked = locked;
            Plugged = plugged;
            Charging = charging;
            ChargeLevel = chargeLevel;
            LastGetTime = DateTime.MinValue;
            LastPutTime = DateTime.MinValue;
            ClientTime = DateTimeOffset.MinValue;
            Price = 0;
            LastUpdate = DateTime.Now;
        }

        public ulong Id { get; set; }
        public ulong HostDeviceId { get; set; }
        public bool Locked { get; set; }
        public bool Plugged { get; set; }
        public bool Charging { get; set; }
        public PowerBankChargeLevel ChargeLevel { get; set; }
        public DateTime LastGetTime { get; set; }
        public DateTime LastPutTime { get; set; }        
        public DateTimeOffset ClientTime { get; set; }
        public float Price { get; set; }
        public DateTime LastUpdate { get; set; }


    }
}
