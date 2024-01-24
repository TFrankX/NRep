using System;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using ProtoBuf.Meta;
using SimnetLib;
using SimnetLib.Model;
using SimnetLib.Network;
namespace GuiClient
{
    public partial class Form1 : Form
    {

        //MQTTBus bus;
        //SimnetClient client;
        //string cmdPushPowerBank;
        //string rplPushPowerBank;
        //string rplPushPowerBankForce;
        //string rplQueryNetworkInfo;
        //string rplQueryTheInventory;
        //string rplhQueryServer;
        //string rplQueryCabinetAPN;
        //string rplQuerySIMCardICCID;
        //string rplResetCabinet;
        //string rptReturnThePowerBank;
        //string rptReportCabinetLogin;
        Device device;
        IniFile config;
        public Form1()
        {
            InitializeComponent();
        }

        public void addLog(string message)
        {
            string curTime = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff");
            rtbLog.Invoke(() => rtbLog.AppendText($"{curTime}  {message}\r\n"));
        }



        private void DeviceConnect(string host, uint port, string login, string pass)
        {

            config.Write("ServerHostName", tbServerAddress.Text, "Server");
            config.Write("ServerPort", tbServerPort.Text, "Server");
            config.Write("Login", tbLogin.Text, "Server");
            config.Write("Password", tbPass.Text, "Server");
            config.Write("DeviceName", tbDeviceName.Text, "Device");
            device.Connect(host, port, login, pass);
        }



        private void bConnect_Click(object sender, EventArgs e)
        {
           // string topic = "cabinet/Dev01/report/10";
           // string command = topic.Substring(topic.LastIndexOf("/") + 1, topic.Length - topic.LastIndexOf("/")-1);

            new Thread(() =>
            {
                DeviceConnect(tbServerAddress.Text, Convert.ToUInt32(tbServerPort.Text), tbLogin.Text, tbPass.Text); ;

                //Thread.Sleep(1000);

                //if (!device.IsConnected())
                //{
                //        addLog($"�� ������� ������������ � �������");
                //}


                //if (device.IsConnected())

                //{

                //    addLog($"������� ���������� � ������� {tbServerAddress.Text}:{tbServerPort.Text}");

                //    device.EvPushPowerBank += Device_EvPushPowerBank;

                //    device.EvPushPowerBankForce += Device_EvPushPowerBankForce;

                //    device.EvQueryTheInventory += Device_EvQueryTheInventory;

                //    device.EvReportCabinetLogin += Device_EvReportCabinetLogin;

                //    device.EvReturnThePowerBank += Device_EvReturnThePowerBank;

                //    device.EvQueryNetworkInfo += Device_EvQueryNetworkInfo;

                //    device.EvQueryServer += Device_EvQueryServer;

                //    device.EvQuerySIMCardICCID += Device_EvQuerySIMCardICCID;

                //    device.EvResetCabinet += Device_EvResetCabinet;

                //}
                //else
                //{
                //    addLog($"�� ������� ������������ � �������");
                //}

            }).Start();
        }

        private void ConnectError(object sender, string error)
        {
            //MessageBox.Show($"������ ����������� � �������: {error}");
            addLog($"������ ����������� � �������: {error}");
        }
        private void Connected(object sender)
        {
            if (device.IsConnected())
            {

                addLog($"������� ���������� � ������� {tbServerAddress.Text}:{tbServerPort.Text}");
                device.EvPushPowerBank += Device_EvPushPowerBank;
                device.EvPushPowerBankForce += Device_EvPushPowerBankForce;
                device.EvQueryTheInventory += Device_EvQueryTheInventory;
                device.EvReportCabinetLogin += Device_EvReportCabinetLogin;
                device.EvReturnThePowerBank += Device_EvReturnThePowerBank;
                device.EvQueryNetworkInfo += Device_EvQueryNetworkInfo;
                device.EvQueryServer += Device_EvQueryServer;
                device.EvQuerySIMCardICCID += Device_EvQuerySIMCardICCID;
                device.EvResetCabinet += Device_EvResetCabinet;
                device.EvSubSniffer += Device_EvSubSniffer;
                device.SubSniffer();
                device.Subcribe(tbDeviceName.Text);
            }
        }

        private void Device_EvSubSniffer(object sender,string topic,object message)
        {
            addLog($"������� ����� {topic}");
        }

        private void Disconnected(object sender)
        {
            addLog($"�������� ����� � �������� {tbServerAddress.Text}:{tbServerPort.Text}");
        }

        private void Device_EvPushPowerBankForce(object sender, string topic, RplPushPowerBankForce data)
        {
            addLog($"����� �� ������ '������������� ������ ���������'");
            addLog($"����� �����: {data.RlSlot}");
            addLog($"�������� ����� ����������: {data.RlPbid}");
            string resultsucces = data.RlResult == 1 ? "������" : "��������";
            addLog($"��������� ��������: {resultsucces}");

        }

        private void Device_EvResetCabinet(object sender, string topic, RplResetCabinet data)
        {
            addLog($"����� �� ������ '���������� ����������'");
        }

        private void Device_EvQuerySIMCardICCID(object sender, string topic, RplQuerySIMCardICCID data)
        {
            addLog($"����� �� ������ ���������� SIM-�����");
        }

        private void Device_EvQueryServer(object sender, string topic, RplQueryServer data)
        {
            addLog($"����� �� ������ ���������� �������");
            string serverType = data.RlType == 1 ? "��������" : "���������";
            addLog($"��� �������: {serverType}");
            addLog($"����� �������: {data.RlAdd}");
            addLog($"���� �������: {data.RlPort}");
        }

        private void Device_EvQueryNetworkInfo(object sender, string topic, RplQueryNetworkInfo data)
        {
            addLog($"����� �� ������ ���������� ����");



            string networks;
            switch (data.RlType)
            {
                case 1:
                    networks = "��������������� WiFi";
                    break;
                case 2:
                    networks = "������ WiFi";
                    break;
                case 3:
                    networks = "��������������� 4G";
                    break;
                default:
                    networks = "������ 4G";
                    break;
            }
            addLog($"���������������� ����� ������ ����: {networks}");

            string networkMode;
            switch (data.RlMode)
            {
                case 0:
                    networkMode = "auto";
                    break;
                case 1:
                    networkMode = "��������������� 3G";
                    break;
                case 2:
                    networkMode = "������ 2G";
                    break;
                case 3:
                    networkMode = "������ 3G";
                    break;
                case 4:
                    networkMode = "������ 4G";
                    break;
                default:
                    networkMode = "����������";
                    break;
            }
            addLog($"����� ������ ��������� ����: {networkMode}");
            string connectstatus = data.RlStatus == 0 ? "�������" : "��� �����������";
            addLog($"��������� �����������: {connectstatus}");


            string currentMode;
            switch (data.RlConn)
            {
                case 0:
                    currentMode = "WiFi";
                    break;
                case 1:
                    currentMode = "2G";
                    break;
                case 2:
                    currentMode = "3G";
                    break;
                case 3:
                    currentMode = "4G";
                    break;
                default:
                    currentMode = "����������";
                    break;
            }
            addLog($"������� ����� �����������: {currentMode}");
            addLog($"�������� ������� ��������� ���� CSQ 0..31 (��� WiFi=0): {data.RlCsq}");
            addLog($"���� ������� ��������� ���� RSRP -140..-22 (��� WiFi=0): {data.RlRsrp}");
            addLog($"����������� ������/��� ��� ��������� ���� SINR -20..30 (��� WiFi=0): {data.RlSinr}");
            addLog($"���� ������� WiFi RSSI (��� ��������� ����=0): {data.RlWifi}");
        }

        private void Device_EvReturnThePowerBank(object sender, string topic, RptReturnThePowerBank data)
        {
            addLog($"���������������� ����������� ���������� � ����: {data.RlSlot}");
            addLog($"�������� �����: {data.RlPbid}");
            string lockLevel = data.RlLock == 1 ? "��" : "���";
            addLog($"������������: {lockLevel}");
            string charging = data.RlLimited == 1 ? "��" : "���";
            addLog($"����������: {charging}");
            string chargeLevel;
            switch (data.RlQoe)
            {
                case 0:
                    chargeLevel = "0%..20%";
                    break;
                case 1:
                    chargeLevel = "20%..40%";
                    break;
                case 2:
                    chargeLevel = "40%..60%";
                    break;
                case 3:
                    chargeLevel = "60%..80%";
                    break;
                case 4:
                    chargeLevel = "80%..100%";
                    break;
                case 5:
                    chargeLevel = "100%";
                    break;
                default:
                    chargeLevel = "���� ��������� ������ � ������";
                    break;
            }
            addLog($"������� ������: {chargeLevel}");
            string status = data.RlCode == 0 ? "��������������" : "����������";
            addLog($"������: {data.RlCode}");
            device.SrvReturnThePowerBank(data.RlSlot, 1, tbDeviceName.Text.Trim());

        }

        private void Device_EvReportCabinetLogin(object sender, string topic, RptReportCabinetLogin data)
        {
            addLog($"���������������� ���������� �� �������:");
            addLog($"�������������� ���������� �����������: {data.RlCount}");

            string networks;
            switch (data.RlNetmode)
            {
                case 1:
                    networks = "������ 4G";
                    break;
                case 2:
                    networks = "������ WiFi";
                    break;
                case 3:
                    networks = "4G � WiFi";
                    break;
                default:
                    networks = "����������";
                    break;
            }
            addLog($"�������������� ����: {networks}");

            string networkMode;
            switch (data.RlNetmode)
            {
                case 0:
                    networkMode = "WiFi";
                    break;
                case 1:
                    networkMode = "2G";
                    break;
                case 2:
                    networkMode = "3G";
                    break;
                case 3:
                    networkMode = "4G";
                    break;
                default:
                    networkMode = "����������";
                    break;
            }
            addLog($"����� ������ ����: {networkMode}");
            addLog($"�������� ������� ��������� ���� CSQ 0..31 (��� WiFi=0): {data.RlCsq}");
            addLog($"���� ������� ��������� ���� RSRP -140..-22 (��� WiFi=0): {data.RlRsrp}");
            addLog($"����������� ������/��� ��� ��������� ���� SINR -20..30 (��� WiFi=0): {data.RlSinr}");
            addLog($"���� ������� WiFi RSSI (��� ��������� ����=0): {data.RlWifi}");
            addLog($"������ ��: {data.RlCommsoftver}");
            addLog($"������ ������: {data.RlCommhardver}");
            addLog($"ICCID SIM-�����: {data.RlIccid}");
        }

        private void Device_EvQueryTheInventory(object sender, string topic, RplQueryTheInventory data)
        {
            addLog($"����� �� ������ ��������������");
            foreach (var pbank in data.RlBank1s)
            {
                addLog($"--------- ��������� ����� {pbank.RlSlot.ToString()} ----------");

                if (pbank.RlIdok == 1)
                {

                    addLog($"S/N: {pbank.RlPbid}");
                    string readIDok = pbank.RlIdok == 1 ? "������" : "��������";
                    addLog($"readID �������� {readIDok}");
                    string lockLevel = pbank.RlLock == 1 ? "��" : "���";
                    addLog($"������������: {lockLevel}");
                    string charging = pbank.RlCharge == 1 ? "��" : "���";
                    addLog($"����������: {charging}");
                    string chargeLevel;
                    switch (pbank.RlQoe)
                    {
                        case 0:
                            chargeLevel = "0%..20%";
                            break;
                        case 1:
                            chargeLevel = "20%..40%";
                            break;
                        case 2:
                            chargeLevel = "40%..60%";
                            break;
                        case 3:
                            chargeLevel = "60%..80%";
                            break;
                        case 4:
                            chargeLevel = "80%..100%";
                            break;
                        case 5:
                            chargeLevel = "100%";
                            break;
                        default:
                            chargeLevel = "���� ��������� ������ � ������";
                            break;
                    }
                    addLog($"������� ������: {chargeLevel}");

                }
                else
                {
                    addLog($"*** ����������� ***");
                }




            }
        }

        private void Device_EvPushPowerBank(object sender, string topic, RplPushPowerBank data)
        {
            addLog($"����� �� ������ ������ ����������");
            addLog($"����� �����: {data.RlSlot}");
            addLog($"�������� ����� ����������: {data.RlPbid}");
            string resultsucces = data.RlResult == 1 ? "������" : "��������";
            addLog($"��������� ��������: {resultsucces}");
        }

        private void bPushPowerBank_Click(object sender, EventArgs e)
        {
            addLog($"�������: ������ ���������");
            device.CmdPushPowerBank((uint)cbSlotNumber.SelectedIndex + 1, tbDeviceName.Text.Trim());
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            addLog("������ �������");
            device = new Device();
            device.EvConnected += Connected;
            device.EvDisconnected += Disconnected;
            device.EvConnectError += ConnectError;


            config = new IniFile("config.ini");

            tbServerAddress.Text = config.Read("ServerHostName", "Server");
            tbServerPort.Text = config.Read("ServerPort", "Server");
            tbLogin.Text = config.Read("Login", "Server");
            tbPass.Text = config.Read("Password", "Server");
            tbDeviceName.Text = config.Read("DeviceName", "Device");
        }

        private void bResetCabinet_Click(object sender, EventArgs e)
        {
            addLog($"�������: ����� ��������");
            device.CmdResetCabinet(tbDeviceName.Text.Trim());
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            config.Write("ServerHostName", tbServerAddress.Text, "Server");
            config.Write("ServerPort", tbServerPort.Text, "Server");
            config.Write("Login", tbLogin.Text, "Server");
            config.Write("Password", tbPass.Text, "Server");
            config.Write("DeviceName", tbDeviceName.Text, "Device");
        }

        private void bServerInfo_Click(object sender, EventArgs e)
        {

            addLog($"�������: ������ ���������� � �������");
            device.CmdQueryServer(1, tbDeviceName.Text.Trim());
        }

        private void bInventory_Click(object sender, EventArgs e)
        {
            addLog($"�������: ������ ��������������");
            device.CmdQueryTheInventory(tbDeviceName.Text.Trim());
        }

        private void rtbLog_TextChanged(object sender, EventArgs e)
        {
            rtbLog.SelectionStart = rtbLog.Text.Length;
            // scroll it automatically
            rtbLog.ScrollToCaret();
        }

        private void bNetworkInfo_Click(object sender, EventArgs e)
        {
            addLog($"�������: ������ ���������� ���� ����������");
            device.CmdQueryNetworkInfo(tbDeviceName.Text.Trim());
        }

        private void bSniffer_Click(object sender, EventArgs e)
        {
            if (bSniffer.Font.Bold == false)
            {
                bSniffer.Font = new Font(bSniffer.Font.Name, bSniffer.Font.Size, FontStyle.Bold);
            }
            else
            {
                bSniffer.Font = new Font(bSniffer.Font.Name, bSniffer.Font.Size, FontStyle.Regular);
            }
        }


    }
}