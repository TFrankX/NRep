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



        private void DeviceConnect(string host, int port, string login, string pass)
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

            addLog($"Подключение к серверу...");
            new Thread(() =>
            {
                DeviceConnect(tbServerAddress.Text, Convert.ToInt32(tbServerPort.Text), tbLogin.Text, tbPass.Text); ;

                Thread.Sleep(1000);

                if (!device.IsConnected())
                {
                        addLog($"Не удалось подключиться к серверу");
                }


                //if (device.IsConnected())

                //{

                //    addLog($"Успешно подключено к серверу {tbServerAddress.Text}:{tbServerPort.Text}");

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
                //    addLog($"Не удалось подключиться к серверу");
                //}

            }).Start();
        }
        private void Connected()
        {
            if (device.IsConnected())
            {

                addLog($"Успешно подключено к серверу {tbServerAddress.Text}:{tbServerPort.Text}");

                device.EvPushPowerBank += Device_EvPushPowerBank;
                device.EvPushPowerBankForce += Device_EvPushPowerBankForce;
                device.EvQueryTheInventory += Device_EvQueryTheInventory;
                device.EvReportCabinetLogin += Device_EvReportCabinetLogin;
                device.EvReturnThePowerBank += Device_EvReturnThePowerBank;
                device.EvQueryNetworkInfo += Device_EvQueryNetworkInfo;
                device.EvQueryServer += Device_EvQueryServer;
                device.EvQuerySIMCardICCID += Device_EvQuerySIMCardICCID;
                device.EvResetCabinet += Device_EvResetCabinet;
                device.Subcribe(tbDeviceName.Text);
            }
        }

        private void Disconnected()
        {
            addLog($"Потеряна связь с сервером {tbServerAddress.Text}:{tbServerPort.Text}");
        }

        private void Device_EvPushPowerBankForce(RplPushPowerBankForce data)
        {
            addLog($"Ответ на запрос 'принудительно выдать повербанк'");
            addLog($"Номер слота: {data.RlSlot}");
            addLog($"Серийный номер повербанка: {data.RlPbid}");
            string resultsucces = data.RlResult == 1 ? "удачно" : "неудачно";
            addLog($"Результат операции: {resultsucces}");

        }

        private void Device_EvResetCabinet(RplResetCabinet data)
        {
            addLog($"Ответ на запрос 'перезапуск устройства'");
        }

        private void Device_EvQuerySIMCardICCID(RplQuerySIMCardICCID data)
        {
            addLog($"Ответ на запрос информации SIM-карты");
        }

        private void Device_EvQueryServer(RplQueryServer data)
        {
            addLog($"Ответ на запрос информации сервера");
            string serverType = data.RlType == 1 ? "Основной" : "Резервный";
            addLog($"Тип сервера: {serverType}");
            addLog($"Адрес сервера: {data.RlAdd}");
            addLog($"Порт сервера: {data.RlPort}");
        }

        private void Device_EvQueryNetworkInfo(RplQueryNetworkInfo data)
        {
            addLog($"Ответ на запрос информации сети");



            string networks;
            switch (data.RlType)
            {
                case 1:
                    networks = "предпочтительно WiFi";
                    break;
                case 2:
                    networks = "только WiFi";
                    break;
                case 3:
                    networks = "Предпочтительно 4G";
                    break;
                default:
                    networks = "только 4G";
                    break;
            }
            addLog($"Предпочтительный режим работы сети: {networks}");

            string networkMode;
            switch (data.RlMode)
            {
                case 0:
                    networkMode = "auto";
                    break;
                case 1:
                    networkMode = "предпочтительно 3G";
                    break;
                case 2:
                    networkMode = "только 2G";
                    break;
                case 3:
                    networkMode = "только 3G";
                    break;
                case 4:
                    networkMode = "только 4G";
                    break;
                default:
                    networkMode = "неизвестно";
                    break;
            }
            addLog($"Режим работы мобильной сети: {networkMode}");
            string connectstatus = data.RlStatus == 0 ? "успешно" : "нет подключения";
            addLog($"Результат подключения: {connectstatus}");


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
                    currentMode = "неизвестно";
                    break;
            }
            addLog($"Текущий режим подключения: {currentMode}");
            addLog($"Качество сигнала мобильной сети CSQ 0..31 (для WiFi=0): {data.RlCsq}");
            addLog($"Сила сигнала мобильной сети RSRP -140..-22 (для WiFi=0): {data.RlRsrp}");
            addLog($"Соотношение сигнал/шум для мобильной сети SINR -20..30 (для WiFi=0): {data.RlSinr}");
            addLog($"Сила сигнала WiFi RSSI (для мобильной сети=0): {data.RlWifi}");
        }

        private void Device_EvReturnThePowerBank(RptReturnThePowerBank data)
        {
            addLog($"Зарегистрировано возвращение повербанка в слот: {data.RlSlot}");
            addLog($"Серийный номер: {data.RlPbid}");
            string lockLevel = data.RlLock == 1 ? "Да" : "Нет";
            addLog($"Заблокирован: {lockLevel}");
            string charging = data.RlLimited == 1 ? "Да" : "Нет";
            addLog($"Заряжается: {charging}");
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
                    chargeLevel = "сбой получения данных о заряде";
                    break;
            }
            addLog($"Процент заряда: {chargeLevel}");
            string status = data.RlCode == 0 ? "Работоспособен" : "Неисправен";
            addLog($"Статус: {data.RlCode}");
            device.SrvReturnThePowerBank(data.RlSlot, 1, tbDeviceName.Text.Trim());

        }

        private void Device_EvReportCabinetLogin(RptReportCabinetLogin data)
        {
            addLog($"Зарегистрировано устройство на сервере:");
            addLog($"Поддерживаемое количество повербанков: {data.RlCount}");

            string networks;
            switch (data.RlNetmode)
            {
                case 1:
                    networks = "только 4G";
                    break;
                case 2:
                    networks = "только WiFi";
                    break;
                case 3:
                    networks = "4G и WiFi";
                    break;
                default:
                    networks = "неизвестно";
                    break;
            }
            addLog($"Поддерживаемые сети: {networks}");

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
                    networkMode = "неизвестно";
                    break;
            }
            addLog($"Режим работы сети: {networkMode}");
            addLog($"Качество сигнала мобильной сети CSQ 0..31 (для WiFi=0): {data.RlCsq}");
            addLog($"Сила сигнала мобильной сети RSRP -140..-22 (для WiFi=0): {data.RlRsrp}");
            addLog($"Соотношение сигнал/шум для мобильной сети SINR -20..30 (для WiFi=0): {data.RlSinr}");
            addLog($"Сила сигнала WiFi RSSI (для мобильной сети=0): {data.RlWifi}");
            addLog($"Версия ПО: {data.RlCommsoftver}");
            addLog($"Версия железа: {data.RlCommhardver}");
            addLog($"ICCID SIM-карты: {data.RlIccid}");
        }

        private void Device_EvQueryTheInventory(RplQueryTheInventory data)
        {
            addLog($"Ответ на запрос инвентаризации");
            foreach (var pbank in data.RlBank1s)
            {
                addLog($"--------- Повербанк номер {pbank.RlSlot.ToString()} ----------");
                
                if (pbank.RlIdok == 1)
                {

                    addLog($"S/N: {pbank.RlPbid}");
                    string readIDok = pbank.RlIdok == 1 ? "удачно" : "неудачно";
                    addLog($"readID прочитан {readIDok}");
                    string lockLevel = pbank.RlLock == 1 ? "Да" : "Нет";
                    addLog($"Заблокирован: {lockLevel}");
                    string charging = pbank.RlCharge == 1 ? "Да" : "Нет";
                    addLog($"Заряжается: {charging}");
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
                            chargeLevel = "сбой получения данных о заряде";
                            break;
                    }
                    addLog($"Процент заряда: {chargeLevel}");

                } 
                else
                {
                    addLog($"*** Отсутствует ***");
                }




            }
        }

        private void Device_EvPushPowerBank(RplPushPowerBank data)
        {
            addLog($"Ответ на запрос выдачи повербанка");
            addLog($"Номер слота: {data.RlSlot}");
            addLog($"Серийный номер повербанка: {data.RlPbid}");
            string resultsucces = data.RlResult == 1 ? "удачно" : "неудачно";
            addLog($"Результат операции: {resultsucces}");
        }

        private void bPushPowerBank_Click(object sender, EventArgs e)
        {
            addLog($"Команда: выдать повербанк");
            device.CmdPushPowerBank((uint)cbSlotNumber.SelectedIndex + 1,tbDeviceName.Text.Trim());
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            addLog("Запуск клиента");
            device = new Device();
            device.EvConnected += Connected;
            device.EvDisConnected += Disconnected;

            config = new IniFile("config.ini");

            tbServerAddress.Text = config.Read("ServerHostName", "Server");
            tbServerPort.Text = config.Read("ServerPort", "Server");
            tbLogin.Text = config.Read("Login", "Server");
            tbPass.Text = config.Read("Password", "Server");
            tbDeviceName.Text = config.Read("DeviceName", "Device");
        }

        private void bResetCabinet_Click(object sender, EventArgs e)
        {
            addLog($"Команда: сброс кабинета");
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

            addLog($"Команда: запрос информации о сервере");
            device.CmdQueryServer(1, tbDeviceName.Text.Trim());
        }

        private void bInventory_Click(object sender, EventArgs e)
        {
            addLog($"Команда: запрос инвентаризации");
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
            addLog($"Команда: запрос информации сети устройства");
            device.CmdQueryNetworkInfo(tbDeviceName.Text.Trim());
        }
    }
}