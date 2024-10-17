using System;
namespace WebServer.Models.Action
{
	public class ActionMessage
	{

        public DateTime ActionTime { get; set; }
        public string UserName { get; set; }
        public ulong ActionServerId { get; set; }
        public ulong ActionStationId { get; set; }
        public ulong ActionPowerBankId { get; set; }
        public string ActionText { get; set; }

        public ActionMessage()
		{
		}




	}
}

