using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using WebServer.Models.Device;

namespace WebServer.Models.Action
{

	public class Action
	{
        // Empty constructor for EF Core
        public Action() { }

        public Action(DateTime actionTime, int actionCode, string userId, ulong actionServerId, ulong actionStationId, ulong actionPowerBankId, uint actionPowerBankSlot, string actionText)
        {
            ActionTime = actionTime;
            ActionCode = actionCode;
            UserId = userId;
            ActionServerId = actionServerId;
            ActionStationId = actionStationId;
            ActionPowerBankId = actionPowerBankId;
            ActionPowerBankSlot = actionPowerBankSlot;
            ActionText = actionText;
            PaymentAmount = 0;
            PaymentInfo = "";
        }

        public Action(DateTime actionTime, int actionCode, string userId, ulong actionServerId, ulong actionStationId, ulong actionPowerBankId, uint actionPowerBankSlot, string actionText, float paymentAmount, string paymentInfo)
        {
            ActionTime = actionTime;
            ActionCode = actionCode;
            UserId = userId;
            ActionServerId = actionServerId;
            ActionStationId = actionStationId;
            ActionPowerBankId = actionPowerBankId;
            ActionPowerBankSlot = actionPowerBankSlot;
            ActionText = actionText;
            PaymentAmount = paymentAmount;
            PaymentInfo = paymentInfo;
        }



        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public DateTime ActionTime { get; set; }
        public int ActionCode { get; set; }
        public string UserId { get; set; }
        public ulong ActionServerId { get; set; }
        [NotMapped]
        public string ActionServerId_Str
        {
            get { return ActionServerId.ToString(); }
        }
        public ulong ActionStationId { get; set; }
        [NotMapped]
        public string ActionStationId_Str
        {
            get { return ActionStationId.ToString(); }
        }
        [NotMapped]
        public string DeviceName { get; set; }

        public ulong ActionPowerBankId { get; set; }
        [NotMapped]
        public string ActionPowerBankId_Str
        {
            get { return ActionPowerBankId.ToString(); }
        }
        [NotMapped]
        public string PowerBankName
        {
            get { return DecodePowerBankId(ActionPowerBankId); }
        }
        public uint ActionPowerBankSlot { get; set; }

        /// <summary>
        /// Decodes PowerBank ID to human-readable name.
        /// ID format: first 4 bytes = ASCII prefix (e.g. "GDVF"), last 4 bytes = serial (hex digits as decimal)
        /// Example: 5135324334914536226 (0x4744564645000322) -> "GDVF45000322"
        /// </summary>
        private static string DecodePowerBankId(ulong id)
        {
            if (id == 0) return "";
            try
            {
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
        public string ActionText { get; set; }
        public float? PaymentAmount { get; set; }
        public string? PaymentInfo { get; set; }
    }



}

