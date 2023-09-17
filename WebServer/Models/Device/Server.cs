using System.ComponentModel;
using SimnetLib;
namespace WebServer.Models.Device
{
    public class Server
    {
        public Server(string host, uint port,string login,string password, uint reconnectTime)
        {
            Host= host;
            Port= port;
            Login= login;
            Password= password;
            ServerId = $"{host}:{port}";
            ReconnectTime = reconnectTime;
            Devices = new List<Device>();
            LastUpdate = DateTime.Now;
            Connect();
        }
        public string ServerId { get; set; }
        public string Host { get; set; }
        public uint Port { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }

        public uint ReconnectTime { get; set; }
        public List<Device> Devices { get; set; }
        public  DateTime LastUpdate { get; private set; }


        public void Connect()
        {
           
        }
    }


}
