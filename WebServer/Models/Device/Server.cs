using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;
using SimnetLib;
namespace WebServer.Models.Device
{
    public delegate void dConnected(object sender);
    public delegate void dDisconnected(object sender);
    public class Server
    {
        public event dConnected EvConnected;
        public event dDisconnected EvDisconnected;

        public Server(string host, uint port,string login,string password, uint reconnectTime)
        {
            Host= host;
            Port= port;
            Login= login;
            Password= password;
            Id = GetGUID($"{host}:{port}");
            ReconnectTime = reconnectTime;
            //Devices = new List<Device>();
            LastUpdate = DateTime.Now;
            Init = false;
            Connected = false;
            device = new SimnetLib.Device();
            //Connect();
        }
        public ulong Id { get; set; }
        public string Host { get; set; }
        public uint Port { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }

        public uint ReconnectTime { get; set; }
        //public List<Device> Devices { get; set; }
        public  DateTime LastUpdate { get; private set; }
        [NotMapped]
        public bool Connected { get; set; }

        [NotMapped]
        public bool Init { get; set; }
        
        [NotMapped]
        SimnetLib.Device device;



        private ulong GetGUID(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            Byte[] bId = new Guid(hash).ToByteArray();                
            ulong result = BitConverter.ToUInt64(bId, 0);           
            return result;
        } 


        public void Connect()
        {
            device.EvConnected += Device_EvConnected;
            device.EvDisconnected += Device_EvDisconnected;
            device.Connect(this.Host, this.Port, this.Login, this.Password);
        }

        private void Device_EvDisconnected()
        {
            EvDisconnected?.Invoke(this);
            this.Connected = false;
        }

        private void Device_EvConnected()
        {
            EvConnected?.Invoke(this);
            this.Connected = true;
        }
    }


}
