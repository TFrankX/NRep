﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;
using ProtoBuf.Meta;
using SimnetLib;
namespace WebServer.Models.Device
{
    public delegate void dConnected(object sender);
    public delegate void dDisconnected(object sender);
    public delegate void dConnectError(object sender,string error);
    public delegate void dPushPowerBank(object sender, RplPushPowerBank data);
    public delegate void dPushPowerBankForce(object sender, RplPushPowerBankForce data);
    public delegate void dQueryNetworkInfo(object sender, RplQueryNetworkInfo data);
    public delegate void dQueryTheInventory(object sender, RplQueryTheInventory data);
    public delegate void dQueryServer(object sender, RplQueryServer data);
    public delegate void dQueryCabinetAPN(object sender, RplQueryCabinetAPN data);
    public delegate void dQuerySIMCardICCID(object sender, RplQuerySIMCardICCID data);
    public delegate void dResetCabinet(object sender, RplResetCabinet data);
    public delegate void dReturnThePowerBank(object sender, RptReturnThePowerBank data);
    public delegate void dReportCabinetLogin(object sender, RptReportCabinetLogin data);
    public delegate void dSubSniffer(object sender, string topic);
    public class Server
    {
        public event dConnected EvConnected;
        public event dDisconnected EvDisconnected;
        public event dConnectError EvConnectError;
        public event dPushPowerBank EvPushPowerBank;
        public event dPushPowerBankForce EvPushPowerBankForce;
        public event dQueryNetworkInfo EvQueryNetworkInfo;
        public event dQueryTheInventory EvQueryTheInventory;
        public event dQueryServer EvQueryServer;
        public event dQueryCabinetAPN EvQueryCabinetAPN;
        public event dQuerySIMCardICCID EvQuerySIMCardICCID;
        public event dResetCabinet EvResetCabinet;
        public event dReturnThePowerBank EvReturnThePowerBank;
        public event dReportCabinetLogin EvReportCabinetLogin;
        public event dSubSniffer EvSubSniffer;
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
            servermqtt = new SimnetLib.Device();
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
        SimnetLib.Device servermqtt, deviceSub;



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
            servermqtt.EvConnected += Device_EvConnected;
            servermqtt.EvDisconnected += Device_EvDisconnected;
            servermqtt.EvConnectError += Device_EvConnectError;
            servermqtt.Connect(this.Host, this.Port, this.Login, this.Password);
            //deviceSub.Connect(this.Host, this.Port, this.Login, this.Password);
        }

        public void SubSniffer()
        {
            if (this.Connected)
            {
                //device.EvPushPowerBank += Device_EvPushPowerBank;
                //device.EvPushPowerBankForce += Device_EvPushPowerBankForce;
                //device.EvQueryTheInventory += Device_EvQueryTheInventory;
                //device.EvReportCabinetLogin += Device_EvReportCabinetLogin;
                //device.EvReturnThePowerBank += Device_EvReturnThePowerBank;
                //device.EvQueryNetworkInfo += Device_EvQueryNetworkInfo;
                //device.EvQueryServer += Device_EvQueryServer;
                //device.EvQuerySIMCardICCID += Device_EvQuerySIMCardICCID;
                //device.EvResetCabinet += Device_EvResetCabinet;
                servermqtt.EvSubSniffer += Device_EvSubSniffer;
                servermqtt.SubSniffer();
            }
        }

        private void Device_EvSubSniffer(string topic)
        {
            EvSubSniffer?.Invoke(this,topic);
        }

        private void Device_EvDisconnected(object sender)
        {
            this.Connected = false;
            EvDisconnected?.Invoke(this);
        }

        private void Device_EvConnected(object sender)
        {
            this.Connected = true;
            EvConnected?.Invoke(this);
        }

        private void Device_EvConnectError(object sender,string error)
        {
            this.Connected = false;
            EvConnectError?.Invoke(this,error);
        }


        public bool CmdPushPowerBank(uint slotNumber, string deviceName)
        {
            return servermqtt.CmdPushPowerBank(slotNumber, deviceName);
        }
        public bool CmdPushPowerBankForce(uint slotNumber, string deviceName)
        {
            return servermqtt.CmdPushPowerBankForce(slotNumber, deviceName);
        }
        public bool CmdQueryNetworkInfo(string deviceName)
        {
            return servermqtt.CmdQueryNetworkInfo(deviceName);
        }

        public bool CmdQueryTheInventory(string deviceName)
        {
            return servermqtt.CmdQueryTheInventory(deviceName);
        }
        public bool CmdQueryServer(uint serverType, string deviceName)
        {
            return servermqtt.CmdQueryServer(serverType, deviceName);

        }

        public bool CmdQueryCabinetAPN(string deviceName)
        {
            return servermqtt.CmdQueryCabinetAPN(deviceName);
        }


        public bool CmdQuerySIMCardICCID(string deviceName)
        {
            return servermqtt.CmdQuerySIMCardICCID(deviceName);
        }
        public bool CmdResetCabinet(string deviceName)
        {
            return servermqtt.CmdResetCabinet(deviceName);
        }


        public bool SrvReturnThePowerBank(uint slot, uint result, string deviceName)
        {
            return servermqtt.SrvReturnThePowerBank(slot, result, deviceName);
        }

    }


}
