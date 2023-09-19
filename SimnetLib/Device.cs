using MQTTnet.Extensions.ManagedClient;
using SimnetLib.Model;
using SimnetLib.Network;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimnetLib
{
    public delegate void dPushPowerBank(RplPushPowerBank data);
    public delegate void dPushPowerBankForce(RplPushPowerBankForce data);
    public delegate void dQueryNetworkInfo(RplQueryNetworkInfo data);
    public delegate void dQueryTheInventory(RplQueryTheInventory data);
    public delegate void dQueryServer(RplQueryServer data);
    public delegate void dQueryCabinetAPN(RplQueryCabinetAPN data);
    public delegate void dQuerySIMCardICCID(RplQuerySIMCardICCID data);
    public delegate void dResetCabinet(RplResetCabinet data);
    public delegate void dReturnThePowerBank(RptReturnThePowerBank data);
    public delegate void dReportCabinetLogin(RptReportCabinetLogin data);
    public delegate void dConnected(object sender);
    public delegate void dDisconnected(object sender);
    public delegate void dConnectError(object sender,string error);
    public delegate void dSubSniffer(string topic);



    public class Device
    {
        MQTTBus bus;
        SimnetClient client;
        private string cmdPushPowerBank;
        private string cmdPushPowerBankForce;
        private string cmdQueryNetworkInfo;
        private string cmdQueryTheInventory;
        private string cmdQueryServer;
        private string cmdQueryCabinetAPN;
        private string cmdQuerySIMCardICCID;
        private string cmdResetCabinet;
        private string srvReturnThePowerBank;
        private string rplPushPowerBank;
        private string rplPushPowerBankForce;
        private string rplQueryNetworkInfo;
        private string rplQueryTheInventory;
        private string rplQueryServer;
        private string rplQueryCabinetAPN;
        private string rplQuerySIMCardICCID;
        private string rplResetCabinet;
        private string rptReturnThePowerBank;
        private string rptReportCabinetLogin;
        private string host;
        private uint port;
        private string deviceName;
        private string login;
        private string pass;
        private string clientName="DeviceLibrary";

        public string Host { get { return host; } set { host = value; } }
        public uint Port { get { return port; } set { port = value; } }
        //public string DeviceName { get { return deviceName; } set { deviceName = value; } }
        public string Login { get { return login; } set { login = value; } }
        public string Pass { get { return pass; } set { pass = value; } }
        public string ClientName { get { return clientName; } set { clientName = value; } }


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
        public event dConnected EvConnected;
        public event dDisconnected EvDisconnected;
        public event dConnectError EvConnectError;
        public event dSubSniffer EvSubSniffer;

        public Device()
        {
            bus = new MQTTBus();
            client = new SimnetClient(bus, clientName);
            client.EvConnected += Connected;
            client.EvDisconnected += Disconnected;
            client.EvConnectError += ConnectError;
        }



        public Device(string clientName):this()
        {
            this.clientName = clientName;
        }

        public Device(string clientName, string host, uint port) : this(clientName)
        {
            this.clientName = clientName;
            this.host = host;
            this.port = port;
            //this.deviceName =deviceName;
        }

        public Device(string clientName, string host, uint port, string login, string pass) : this(clientName, host,port)
        {
            this.login = login;
            this.pass = pass;
        }

        public void Connect()
        {
            client.Connect(this.host, this.port, this.login, this.pass);
            Thread.Sleep(1000);        

        }

        public void Connect(string host, uint port)
        {
            this.host = host;
            this.port = port;
            Connect();
        }

        public  void Connect(string host, uint port,string login, string pass)
        {
            this.login = login;
            this.pass = pass;
            Connect( host,  port);
        }


        public void SubSniffer()
        {
            client.Subscribe<object>("cabinet/#", (sender, topic, message) =>
            {
                EvSubSniffer?.Invoke(topic);
            });
        }


        public void Subcribe(string deviceName)
        {

            rplPushPowerBank = $"cabinet/{deviceName}/reply/{MessageTypes.PushPowerBank}";
            rplPushPowerBankForce = $"cabinet/{deviceName}/reply/{MessageTypes.PushPowerBankForce}";
            rplQueryNetworkInfo = $"cabinet/{deviceName}/reply/{MessageTypes.QueryNetworkInfo}";
            rplQueryTheInventory = $"cabinet/{deviceName}/reply/{MessageTypes.QueryTheInventory}";
            rplQueryServer = $"cabinet/{deviceName}/reply/{MessageTypes.QueryServer}";
            rplQueryCabinetAPN = $"cabinet/{deviceName}/reply/{MessageTypes.QueryCabinetAPN}";
            rplQuerySIMCardICCID = $"cabinet/{deviceName}/reply/{MessageTypes.QuerySIMCardICCID}";
            rplResetCabinet = $"cabinet/{deviceName}/reply/{MessageTypes.ResetCabinet}";
            rptReturnThePowerBank = $"cabinet/{deviceName}/report/{MessageTypes.ReturnThePowerBank}";
            rptReportCabinetLogin = $"cabinet/{deviceName}/report/{MessageTypes.ReportCabinetLogin}";

            if (client.IsConnected())
            {

                client.Subscribe<RplPushPowerBank>(rplPushPowerBank, (sender, topic, message) =>
                {
                    EvPushPowerBank?.Invoke(message);
                });

                client.Subscribe<RplPushPowerBankForce>(rplPushPowerBankForce, (sender, topic, message) =>
                {
                    EvPushPowerBankForce?.Invoke(message);
                });

                client.Subscribe<RplQueryNetworkInfo>(rplQueryNetworkInfo, (sender, topic, message) =>
                {
                    EvQueryNetworkInfo?.Invoke(message);
                });

                client.Subscribe<RplQueryTheInventory>(rplQueryTheInventory, (sender, topic, message) =>
                {
                    EvQueryTheInventory?.Invoke(message);
                });

                client.Subscribe<RplQueryServer>(rplQueryServer, (sender, topic, message) =>
                {
                    EvQueryServer?.Invoke(message);
                });

                client.Subscribe<RplQueryCabinetAPN>(rplQueryCabinetAPN, (sender, topic, message) =>
                {
                    EvQueryCabinetAPN?.Invoke(message);
                });

                client.Subscribe<RplQuerySIMCardICCID>(rplQuerySIMCardICCID, (sender, topic, message) =>
                {
                    EvQuerySIMCardICCID?.Invoke(message);
                });

                client.Subscribe<RplResetCabinet>(rplResetCabinet, (sender, topic, message) =>
                {
                    EvResetCabinet?.Invoke(message);
                });

                client.Subscribe<RptReturnThePowerBank>(rptReturnThePowerBank, (sender, topic, message) =>
                {
                    EvReturnThePowerBank?.Invoke(message);
                });

                client.Subscribe<RptReportCabinetLogin>(rptReportCabinetLogin, (sender, topic, message) =>
                {
                    EvReportCabinetLogin?.Invoke(message);
                });

            }

        }

        //bool SendMessage(string topic, CmdPushPowerBank data)
        //{
        //    if (client.IsConnected())
        //    {
        //        client.Publish(topic, data);
        //        return true;
        //    }
        //    else return false; 
        //}

        public void Connected(object sender)
        {
            //Subcribe(deviceName);
            EvConnected?.Invoke(this);
        }
        public void Disconnected(object sender)
        {
            //Subcribe(deviceName);
            EvDisconnected?.Invoke(this);
        }
        public void ConnectError(object sender,string error)
        {
            //Subcribe(deviceName);
            EvConnectError?.Invoke(this,error);
        }

        public bool CmdPushPowerBank(uint slotNumber, string deviceName) 
        {
            cmdPushPowerBank = $"cabinet/{deviceName}/cmd/{MessageTypes.PushPowerBank}";
            var sCmdPushPowerBank = new CmdPushPowerBank
            {
                RlSlot = slotNumber,
                RlSeq = 1
            };
            if (client.IsConnected())
            {
                client.Publish(cmdPushPowerBank, sCmdPushPowerBank);
                return true;
            }
            else return false;

            //return SendMessage(cmdPushPowerBank, sCmdPushPowerBank);
        }
        public bool CmdPushPowerBankForce(uint slotNumber, string deviceName)
        {
            cmdPushPowerBankForce = $"cabinet/{deviceName}/cmd/{MessageTypes.PushPowerBankForce}";
            var sCmdPushPowerBankForce = new CmdPushPowerBankForce
            {
                RlSlot = slotNumber,
                RlSeq = 1
            };
            if (client.IsConnected())
            {
                client.Publish(cmdPushPowerBankForce, sCmdPushPowerBankForce);
                return true;
            }
            else return false;
            //return SendMessage(cmdPushPowerBankForce, sCmdPushPowerBankForce);
        }
        public bool CmdQueryNetworkInfo(string deviceName) 
        {
            cmdQueryNetworkInfo = $"cabinet/{deviceName}/cmd/{MessageTypes.QueryNetworkInfo}";
            var sCmdQueryNetworkInfo = new CmdQueryNetworkInfo
            {
                RlSeq = 1
            };
            if (client.IsConnected())
            {
                client.Publish(cmdQueryNetworkInfo, sCmdQueryNetworkInfo);
                return true;
            }
            else return false;


            //return SendMessage(cmdQueryNetworkInfo, sCmdQueryNetworkInfo);
        }

        public  bool CmdQueryTheInventory(string deviceName)
         {
            cmdQueryTheInventory = $"cabinet/{deviceName}/cmd/{MessageTypes.QueryTheInventory}";
            var sCmdQueryTheInventory = new CmdQueryTheInventory
             {
                 RlSeq = 1
             };
            if (client.IsConnected())
            {
                client.Publish(cmdQueryTheInventory, sCmdQueryTheInventory);
                return true;
            }
            else return false;

             //return SendMessage(cmdQueryTheInventory, sQueryTheInventory);
         }
        public bool CmdQueryServer(uint serverType, string deviceName)
        {
            cmdQueryServer = $"cabinet/{deviceName}/cmd/{MessageTypes.QueryServer}";
            var sCmdQueryServer = new CmdQueryServer
            {
                RlType = serverType,
                RlSeq = 1

            };
            if (client.IsConnected())
            {
                client.Publish(cmdQueryServer, sCmdQueryServer);
                return true;
            }
            else return false;

            //return SendMessage(cmdQueryServer, sQueryServer);

        }

        public bool CmdQueryCabinetAPN(string deviceName)
        {
            cmdQueryCabinetAPN = $"cabinet/{deviceName}/cmd/{MessageTypes.QueryCabinetAPN}";
            var sCmdQueryCabinetAPN = new CmdQueryCabinetAPN
            {
                RlSeq = 1
            };
            if (client.IsConnected())
            {
                client.Publish(cmdQueryCabinetAPN, sCmdQueryCabinetAPN);
                return true;
            }
            else return false;
            //return SendMessage(cmdQueryCabinetAPN, sQueryCabinetAPN);
        }
 
        
        public bool CmdQuerySIMCardICCID(string deviceName)
        {
            cmdQuerySIMCardICCID = $"cabinet/{deviceName}/cmd/{MessageTypes.QuerySIMCardICCID}";
            var sCmdQuerySIMCardICCID = new CmdQuerySIMCardICCID
             {
                 RlSeq = 1
             };
            if (client.IsConnected())
            {
                client.Publish(cmdQueryCabinetAPN, sCmdQuerySIMCardICCID);
                return true;
            }
            else return false;
            //     return SendMessage(cmdQuerySIMCardICCID, sQuerySIMCardICCID);
        }
        public bool CmdResetCabinet(string deviceName)
        {
            cmdResetCabinet = $"cabinet/{deviceName}/cmd/{MessageTypes.ResetCabinet}";
            var sCmdResetCabinet = new CmdResetCabinet
             {
                 RlSeq = 1
             };
            if (client.IsConnected())
            {
                client.Publish(cmdResetCabinet, sCmdResetCabinet);
                return true;
            }
            else return false;

            //     return SendMessage(cmdResetCabinet, sResetCabinet);
        }


        public bool SrvReturnThePowerBank(uint slot,uint result,string deviceName) 
        {
            srvReturnThePowerBank = $"cabinet/{deviceName}/cmd/{MessageTypes.ReturnThePowerBank}";
            var sSrvReturnThePowerBank = new SrvReturnThePowerBank
            {
                RlSlot = slot,
                RlResult = result,
                RlSeq = 1
            };
            if (client.IsConnected())
            {
                client.Publish(srvReturnThePowerBank, sSrvReturnThePowerBank);
                return true;
            }
            else return false;
        }


        public bool IsConnected()
        {
            return client.IsConnected();
        }
        //public static string PushPowerBank { get { return "15"; } }
        //public static string PushPowerBankForce { get { return "11"; } }
        //public static string QueryNetworkInfo { get { return "24"; } }
        //public static string QueryTheInventory { get { return "13"; } }
        //public static string QueryServer { get { return "18"; } }
        //public static string QueryCabinetAPN { get { return "17"; } }
        //public static string QuerySIMCardICCID { get { return "20"; } }
        //public static string ResetCabinet { get { return "25"; } }
        //public static string ReturnThePowerBank { get { return "22"; } }
        //public static string ReportCabinetLogin { get { return "10"; } }




    }
}
