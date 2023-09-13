namespace WebServer.Models.Device
{
    public class PowerBank
    {
        public ulong Id { get; set; }
        public bool Locked { get; set; }
        public bool Plugged { get; set; }
        public bool Charging { get; set; }
        public PowerBankChargeLevel ChargeLevel { get; set; }
        public DateTime LastGetTime { get; set; }
        public DateTime LastPutTime { get; set; }        
        public DateTimeOffset ClientTime { get; set; }
        public float Price { get; set; }

    }
}
