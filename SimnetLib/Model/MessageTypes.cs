using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimnetLib.Model
{
    public class MessageTypes
    {
  
        public static string PushPowerBank { get { return "15"; } }
        public static string PushPowerBankForce { get { return "11"; } }
        public static string QueryNetworkInfo { get { return "24"; } }
        public static string QueryTheInventory { get { return "13"; } }
        public static string QueryServer { get { return "18"; } }
        public static string QueryCabinetAPN { get { return "17"; } }
        public static string QuerySIMCardICCID { get { return "20"; } }
        public static string ResetCabinet { get { return "25"; } }
        public static string ReturnThePowerBank { get { return "22"; } }
        public static string ReportCabinetLogin { get { return "10"; } }
    }
}
