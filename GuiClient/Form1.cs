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

        MQTTBus bus;
        SimnetClient client;
        string cmdPushPowerBank;
        string rplPushPowerBank;
        string rplPushPowerBankForce;
        string rplQueryNetworkInfo;
        string rplQueryTheInventory;
        string rplhQueryServer;
        string rplQueryCabinetAPN;
        string rplQuerySIMCardICCID;
        string rplResetCabinet;
        string rptReturnThePowerBank;
        string rptReportCabinetLogin;
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



        private void DeviceConnect(string host, int port, string deviceName, string login, string pass)
        {

            config.Write("ServerHostName", tbServerAddress.Text, "Server");
            config.Write("ServerPort", tbServerPort.Text, "Server");
            config.Write("Login", tbLogin.Text, "Server");
            config.Write("Password", tbPass.Text, "Server");
            config.Write("DeviceName", tbDeviceName.Text, "Device");
            device.Connect(host, port, deviceName, login, pass);
        }



        private void bConnect_Click(object sender, EventArgs e)
        {

            addLog($"����������� � �������...");
            new Thread(() =>
            {
                DeviceConnect(tbServerAddress.Text, Convert.ToInt32(tbServerPort.Text), tbDeviceName.Text, tbLogin.Text, tbPass.Text); ;

                Thread.Sleep(1000);

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

                }
                else
                {
                    addLog($"�� ������� ������������ � �������");
                }

            }).Start();
        }

        private void Device_EvPushPowerBankForce(RplPushPowerBankForce data)
        {
            addLog($"����� �� ������ '������������� ������ ���������'");
        }

        private void Device_EvResetCabinet(RplResetCabinet data)
        {
            addLog($"����� �� ������ '���������� ����������'");
        }

        private void Device_EvQuerySIMCardICCID(RplQuerySIMCardICCID data)
        {
            addLog($"����� �� ������ ���������� SIM-�����");
        }

        private void Device_EvQueryServer(RplQueryServer data)
        {
            addLog($"����� �� ������ ���������� �������");
            string serverType = data.RlType == 1 ? "��������" : "���������";
            addLog($"��� �������: {serverType}");
            addLog($"����� �������: {data.RlAdd}");
            addLog($"���� �������: {data.RlPort}");
        }

        private void Device_EvQueryNetworkInfo(RplQueryNetworkInfo data)
        {
            addLog($"����� �� ������ ���������� ����");
        }

        private void Device_EvReturnThePowerBank(RptReturnThePowerBank data)
        {
            addLog($"���������������� ����������� ����������");
            addLog($"����� �����: {data.RlSlot}");
            addLog($"�������� �����: {data.RlPbid}");
            addLog($"����������: {data.RlVol}");
            device.SrvReturnThePowerBank(data.RlSlot, 1);

        }

        private void Device_EvReportCabinetLogin(RptReportCabinetLogin data)
        {
            addLog($"���������������� ���������� �� �������:");
            addLog($"������ ��: {data.RlCommsoftver}");
        }

        private void Device_EvQueryTheInventory(RplQueryTheInventory data)
        {
            addLog($"����� �� ������ ��������������");
            foreach (var pbank in data.RlBank1s)
            {
                addLog($"--------- ��������� ����� {pbank.RlSlot.ToString()} ----------");
                addLog($"S/N: {pbank.RlPbid}");
                string readIDok = pbank.RlIdok == 1 ? "������" : "��������";
                addLog($"readID �������� {readIDok}");
                string lockLevel = pbank.RlLock == 1 ? "�������" : "������";
                addLog($"������� ����������: {lockLevel}");
                string charging = pbank.RlCharge == 1 ? "��" : "���";
                addLog($"����������: {pbank.RlCharge}");
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
                        chargeLevel = "fail";
                        break;
                }
                addLog($"������� ������: {chargeLevel}");

            }
        }

        private void Device_EvPushPowerBank(RplPushPowerBank data)
        {
            addLog($"����� �� ������ ������ ����������");
            addLog($"����� �����: {data.RlSlot}");
            addLog($"�������� ����� ����������: {data.RlPbid}");
            addLog($"��������� �������� (0-��������, 1-������): {data.RlResult}");
        }

        private void bPushPowerBank_Click(object sender, EventArgs e)
        {
            addLog($"�������: ������ ���������");
            device.CmdPushPowerBank((uint)cbSlotNumber.SelectedIndex + 1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            addLog("������ �������");
            device = new Device();
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
            device.CmdResetCabinet();
        }

        private void bCabinetInfo_Click(object sender, EventArgs e)
        {
            addLog($"�������: ������ ���������� ��������");
            device.CmdQueryNetworkInfo();
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
            device.CmdQueryServer(1);
        }

        private void bInventory_Click(object sender, EventArgs e)
        {
            addLog($"�������: ������ ��������������");
            device.CmdQueryTheInventory();
        }

        private void rtbLog_TextChanged(object sender, EventArgs e)
        {
            rtbLog.SelectionStart = rtbLog.Text.Length;
            // scroll it automatically
            rtbLog.ScrollToCaret();
        }
    }
}