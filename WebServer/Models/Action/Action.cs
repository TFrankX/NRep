using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebServer.Models.Device;

namespace WebServer.Models.Action
{

	public class Action
	{


        public Action(DateTime actionTime, int actionCode, string userId, ulong actionServerId, ulong actionStationId, ulong actionPowerBankId, uint actionPowerBankSlot, string actionText)
        {
            //ActionTime = DateTime.Now;
            ActionTime = actionTime;
            ActionCode = actionCode;
            UserId = userId;
            ActionServerId = actionServerId;
            ActionStationId = actionStationId;
            ActionPowerBankId = actionPowerBankId;
            ActionPowerBankSlot = actionPowerBankSlot;
            ActionText = actionText;
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

        public ulong ActionPowerBankId { get; set; }
        [NotMapped]
        public string ActionPowerBankId_Str
        {
            get { return ActionPowerBankId.ToString(); }
        }
        public uint ActionPowerBankSlot { get; set; }
        public string ActionText { get; set; }
    }



}

