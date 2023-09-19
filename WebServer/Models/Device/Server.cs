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
    public delegate void dConnectError(object sender,string error);
    public class Server
    {
        public event dConnected EvConnected;
        public event dDisconnected EvDisconnected;
        public event dConnectError EvConnectError;

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
            //deviceSub = new SimnetLib.Device();
            Error = "";
            //Connect();
        }
        public ulong Id { get; set; }
        public string Host { get; set; }
        public uint Port { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Error { get; set; }
        public uint ReconnectTime { get; set; }
        //public List<Device> Devices { get; set; }
        public DateTime ConnectTime { get; set; }
        public DateTime DisconnectTime { get; set; }
        public  DateTime LastUpdate { get; private set; }
        [NotMapped]
        public bool Connected { get; set; }

        [NotMapped]
        public bool Init { get; set; }
        
        [NotMapped]
        SimnetLib.Device device, deviceSub;



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
            device.EvConnectError += Device_EvConnectError;
            device.Connect(this.Host, this.Port, this.Login, this.Password);
            //deviceSub.Connect(this.Host, this.Port, this.Login, this.Password);
        }

        public void Subscript()
        {
            if (this.Connected)
            {

            }
        }

        private void Device_EvDisconnected(object sender)
        {
            EvDisconnected?.Invoke(this);
            this.Connected = false;
        }

        private void Device_EvConnected(object sender)
        {
            EvConnected?.Invoke(this);
            this.Connected = true;
        }

        private void Device_EvConnectError(object sender,string error)
        {
            EvConnectError?.Invoke(this,error);
            this.Connected = false;
        }
    }


}
