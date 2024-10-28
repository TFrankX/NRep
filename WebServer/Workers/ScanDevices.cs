using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NuGet.Protocol.Plugins;
using SimnetLib;
using System;
using System.Data;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using WebServer.Controllers.Device;
using WebServer.Data;
using WebServer.Models.Device;
using WebServer.Models.Action;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Formats.Asn1.AsnWriter;
using static System.Reflection.Metadata.BlobBuilder;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Device = WebServer.Models.Device.Device;
using ProtoBuf.Meta;
using Microsoft.AspNetCore.Identity;
using WebServer.Models.Identity;
using System.Threading.Tasks;

namespace WebServer.Workers
{
    //public delegate void dReturnThePowerBank(object sender, string topic, RptReturnThePowerBank data);
    public delegate void dReturnThePowerBank(string deviceName, ulong pbId, uint slot, float cost);
    public class ScanDevices : BackgroundService
    {
        public IDevicesData DevicesData { get; set; }
        //public List<Server> servers;
        //public List<WebServer.Models.Device.Device> devices;
        //public List<PowerBank> powerbanks;
        //public readonly IServiceScopeFactory ScopeFactory;
        private readonly ILogger<ScanDevices> Logger;
        //private IActionTable actionTable; 
        public event dReturnThePowerBank EvReturnThePowerBank;
        IServiceScope scope;
        private IConfiguration config;
        private ActionProcess actionProcess;
        private readonly UserManager<AppUser> userManager;
        public long scanDevicePeriod = 300;//300 sec

                                      
        // DeviceContext dbDevice;

        //  public ScanDevices(List<Server> Servers, ILogger<ScanDevices> logger, IServiceScopeFactory scopeFactory)
        //  public ScanDevices(ILogger<ScanDevices> logger, IDevicesData devicesData, IServiceScopeFactory scopeFactory, UserManager<AppUser> _userManager)
        public ScanDevices(ILogger<ScanDevices> logger, IDevicesData devicesData, IServiceScopeFactory scopeFactory)
        {
            //ScopeFactory = scopeFactory;
            DevicesData = devicesData;
            Logger = logger;
            scope = scopeFactory.CreateScope();
 //           userManager = _userManager;

            config = new ConfigurationBuilder()
.           AddJsonFile("appsettings.json", optional: false, reloadOnChange: false).Build();
            actionProcess = new ActionProcess(scopeFactory);

        }


        public virtual void HandleDbException(Exception exception)
        {
            if (exception is DbUpdateConcurrencyException concurrencyEx)
            {
                // A custom exception of yours for concurrency issues
                // throw new ConcurrencyException();
                Logger.LogInformation($"Access database exception: {exception.Message}\n");

            }
            else if (exception is DbUpdateException dbUpdateEx)
            {
                if (dbUpdateEx.InnerException != null
                        && dbUpdateEx.InnerException != null)
                {
                    //if (dbUpdateEx.InnerException is System.Data.SqlClient.SqlException sqlException)
                    //{
                    //    switch (sqlException.Number)
                    //    {
                    //        case 2627:  // Unique constraint error
                    //        case 547:   // Constraint check violation
                    //        case 2601:  // Duplicated key row error
                    //                    // Constraint violation exception
                    //                    // A custom exception of yours for concurrency issues
                    //            Logger.LogInformation($"Database 'Device' concurrency exception: {exception.InnerException.Message}\n");
                    //            break;
                    //        //throw new ConcurrencyException();
                    //        default:
                    //            // A custom exception of yours for other DB issues
                    //            Logger.LogInformation($"Database 'Device' access exception: {exception.InnerException.Message}\n");
                    //            break;
                    //            //throw new DatabaseAccessException(
                    //            //  dbUpdateEx.Message, dbUpdateEx.InnerException);
                    //    }
                    //}
                    Logger.LogInformation($"Database 'Device' access exception: {exception.InnerException.Message}\n");
                    //throw new DatabaseAccessException(dbUpdateEx.Message, dbUpdateEx.InnerException);
                }
            }

            // If we're here then no exception has been thrown
            // So add another piece of code below for other exceptions not yet handled...
        }



        public TEntity AddOrUpdate<TEntity>(DbSet<TEntity> dbset, DbContext context, Func<TEntity, object> identifier, TEntity entity) where TEntity : class
        {
            TEntity result = dbset.Find(identifier.Invoke(entity));
            if (result != null)
            {
                context.Entry(result).CurrentValues.SetValues(entity);
                dbset.Update(result);
                return result;
            }
            else
            {
                dbset.Add(entity);
                return entity;
            }
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string server1address = config.GetSection("Server1")["address"];
            string server1portS = config.GetSection("Server1")["port"];
            string server1user = config.GetSection("Server1")["user"];
            string server1pass = config.GetSection("Server1")["pass"];
            string server1reconnectTimeS = config.GetSection("Server1")["reconnectTime"];
            string certCA = config.GetSection("Server1")["certCA"];
            string certCli = config.GetSection("Server1")["certCli"];
            string certPass = config.GetSection("Server1")["certPass"];
            uint server1port;
            uint server1reconnectTime;
            try
            {
                server1port = Convert.ToUInt16(server1portS);
            }
            
            catch
            {
                server1port = 8884;
            }

            try
            {
                server1reconnectTime = Convert.ToUInt16(server1reconnectTimeS);
            }

            catch
            {
                server1reconnectTime = 30;
            }

            // Server server = new Server("yaup.ru", 8884, "devclient", "Potato345!", 30);
            Server server = new Server(server1address, server1port, server1user, server1pass, server1reconnectTime,certCA,certCli,certPass);
            actionProcess.ActionSave((int)ActionsDescription.ServiceStart, "System", 0, 0, 0,0, "");
            using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
            {
                AddOrUpdate(dbDevice.Server, dbDevice, c => c.Id, server);
                server.Stored = true;
                dbDevice.SaveChanges();
            }

            //try
            //{
            //    using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
            //    {
            //        if ((dbDevice.Server.Any(o => o.Id != server.Id)) || (dbDevice.Server.Count() == 0))
            //        {
            //            dbDevice.Server.Add(server);
            //            dbDevice.SaveChanges();
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    //dbDevice.Entry(server).State = EntityState.Detached;
            //    HandleDbException(ex);
            //}


            InitDevices();

            while (!stoppingToken.IsCancellationRequested)
            {
                ReconnectServers();
                UpdateDb();
                await Task.Delay(TimeSpan.FromSeconds(10));
            }

        }



        private void UpdateDb()
        {

            using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
            {
               
                //dbActions.SaveChanges();
                //actionTable = scope.ServiceProvider.GetRequiredService<IDevActionTable>();

                foreach (var powbank in DevicesData.PowerBanks)
                {


                    //var actionTable = scope.ServiceProvider.GetRequiredService<IDevActionTable>();
                    //actionTable.Name = "Dev02"; //powbank.Id_str;

                  //  var dateTime = DateTime.Now;
                   // dbDevActions.SaveChanges();
                  //  var action = new WebServer.Models.Action.Action(dateTime, 777, 666, 888, 999, "Yes");
                  //  dbActions.Action.Add(action);
                    //dbActions.SaveChanges();
                    //AddOrUpdate(dbActions.Action, dbActions, c => c.ActionTime, action);




                    if (!powbank.Stored)
                    {

                        AddOrUpdate(dbDevice.PowerBank, dbDevice, c => c.Id, powbank);
                        powbank.Stored = true;
                    }
                }

                foreach (var dev in DevicesData.Devices)
                {
                    //var dbDevActions = scope.ServiceProvider.GetRequiredService<ActionContext>();
                    //dbDevActions.SaveChanges();
                    //var dbActions = scope.ServiceProvider.GetRequiredService<ActionContext>();
                    //var dateTime = DateTime.Now;

                    //var action = new WebServer.Models.Action.Action(dateTime, 777, 666, 888, 999, "Yes");
                    //AddOrUpdate(dbActions.Action, dbActions, c => c.ActionTime, action);
                    //dbActions.Action.Add(action);
                    //dbActions.SaveChanges();

                    if (!dev.Stored)
                    {
                        AddOrUpdate(dbDevice.Device, dbDevice, c => c.Id, dev);
                        dev.Stored = true;
                    }
                }

                foreach (var srv in DevicesData.Servers)
                {
                    if (!srv.Stored)
                    {
                        AddOrUpdate(dbDevice.Server, dbDevice,c => c.Id, srv);
                         srv.Stored = true;
                    }
                }

                dbDevice.SaveChanges();

            }

        }


        private void InitDevices()
        {
            using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
            {

                try
                {
                    foreach (var srv in dbDevice.Server)
                    {
                        srv.Connected = false;
                        srv.Stored = false;
                        DevicesData.Servers.Add(srv);
                       //- actionProcess.ActionSave((int)ActionsDescription.ServiceInitServer, "System", srv.Id, 0, 0, 0, "");

                    }
                    ReconnectServers();

                }
                catch
                {


                }

                try
                {

                    foreach (var dev in dbDevice.Device)
                    {
                        dev.Online = false;

                        DevicesData.Devices.Add(dev);
                        //- actionProcess.ActionSave((int)ActionsDescription.ServiceInitDevice, "System", dev.HostDeviceId, dev.Id, 0, 0, "");


                    }


                }
                catch (Exception ex)
                {

                    Logger.LogInformation($"Device cannot add from the db: {ex.ToString()} \n");
                }
                try
                {

                    foreach (var powerbank in dbDevice.PowerBank)
                    {
                        {
                            DevicesData.PowerBanks.Add(powerbank);
                            //powerbank.Plugged = false;
                            var servId = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == powerbank.HostDeviceId)].HostDeviceId;
                            //var servHost = DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == devId)].Id;
                           //- actionProcess.ActionSave((int)ActionsDescription.ServiceInitPowerBank, "System", servId, powerbank.HostDeviceId, powerbank.Id, powerbank.HostSlot, "");
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.LogInformation($"Powerbank cannot add from the db: {ex.ToString()}\n");
                }

                try
                {

                    foreach (var dev in dbDevice.Device)
                    {


                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].EvQueryTheInventory -= Srv_EvQueryTheInventory;
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].EvQueryTheInventory += Srv_EvQueryTheInventory;
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].EvReturnThePowerBank -= Srv_EvReturnThePowerBank;
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].EvReturnThePowerBank += Srv_EvReturnThePowerBank;
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].EvReportCabinetLogin -= Srv_EvReportCabinetLogin;
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].EvReportCabinetLogin += Srv_EvReportCabinetLogin;
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].SubScript(dev.DeviceName);
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].CmdQueryTheInventory(dev.DeviceName);
                    }

                }
                catch
                {

                }


            }
        }



        private void ReconnectServers()
        {
            foreach (var srv in DevicesData.Servers)
            {
                if ((srv != null))
                {
                    if (!srv.Connected)
                    {






                        srv.EvConnected -= Srv_EvConnected;
                        srv.EvConnected += Srv_EvConnected;
                        srv.EvConnectError -= Srv_EvConnectError;
                        srv.EvConnectError += Srv_EvConnectError;
                        srv.EvDisconnected -= Srv_EvDisconnected;
                        srv.EvDisconnected += Srv_EvDisconnected;


                        srv.Connect();

                        //srv.EvReportCabinetLogin -= Srv_EvReportCabinetLogin;
                        //srv.EvReportCabinetLogin += Srv_EvReportCabinetLogin;

                        //srv.EvSubSniffer -= Srv_EvSniffer;
                        //srv.EvSubSniffer += Srv_EvSniffer;

                        srv.RecentlyConnect = true;
                    }
                    else
                    {
                        if (srv.RecentlyConnect)
                        {



                            //srv.SubScriptLogin();


                            srv.EvQueryTheInventory -= Srv_EvQueryTheInventory;
                            srv.EvQueryTheInventory += Srv_EvQueryTheInventory;
                            srv.EvReturnThePowerBank -= Srv_EvReturnThePowerBank;
                            srv.EvReturnThePowerBank += Srv_EvReturnThePowerBank;
                            //srv.EvReportCabinetLogin -= Srv_EvReportCabinetLogin;
                            //srv.EvReportCabinetLogin += Srv_EvReportCabinetLogin;

                            foreach (var dev in DevicesData.Devices)
                            {
                                if (dev.HostDeviceId == srv.Id)
                                {
                                    srv.SubScript(dev.DeviceName);
                                    srv.CmdQueryTheInventory(dev.DeviceName);
                                }
                            }
                            srv.RecentlyConnect = false;
                        }
                        else
                        {
                            //srv.EvSubSniffer -= Srv_EvSniffer;
                            //srv.EvSubSniffer += Srv_EvSniffer;
                            //srv.EvReportCabinetLogin -= Srv_EvReportCabinetLogin;
                            //srv.EvReportCabinetLogin += Srv_EvReportCabinetLogin;
                            foreach (var dev in DevicesData.Devices)
                            {
                                if ((dev.HostDeviceId == srv.Id) )
                                {
                                    if ((DateTime.Now - dev.LastOnlineTime).TotalSeconds > srv.OnlineTimeOut)
                                    {
                                        if (dev.Online == true)
                                        {
                                            actionProcess.ActionSave((int)ActionsDescription.StationDisconnect, "System", dev.HostDeviceId, dev.Id, 0, 0, "");
                                        }
                                        dev.Online = false;
                                        //dev.Slots = 0;
                                        //foreach (var powerbank in DevicesData.PowerBanks)
                                        //{
                                        //   if (powerbank.HostDeviceName == dev.DeviceName)
                                        //    {
                                        //        powerbank.Plugged = false;
                                        //    }
                                        //}


                                        srv.EvQueryTheInventory -= Srv_EvQueryTheInventory;
                                        srv.EvQueryTheInventory += Srv_EvQueryTheInventory;
                                        srv.EvReturnThePowerBank -= Srv_EvReturnThePowerBank;
                                        srv.EvReturnThePowerBank += Srv_EvReturnThePowerBank;
                                        //srv.EvReportCabinetLogin -= Srv_EvReportCabinetLogin;
                                        //srv.EvReportCabinetLogin += Srv_EvReportCabinetLogin;

                                    }

                                    srv.SubScript(dev.DeviceName);
                                    if (dev.LastInventoryTime + (scanDevicePeriod * TimeSpan.TicksPerSecond) < DateTime.Now.Ticks)
                                    {
                                        dev.LastInventoryTime = DateTime.Now.Ticks;
                                        srv.CmdQueryTheInventory(dev.DeviceName);
                                    }
                                }

                            }

                        }



                    }

                }

            }

        }


        private void Srv_EvReportCabinetLogin(object sender, string topic, RptReportCabinetLogin data)
        {


            string dev = "";
            string command = "";
            try
            {

                dev = topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).Substring(0, topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).IndexOf('/'));
                command = topic.Substring(topic.LastIndexOf("/") + 1, topic.Length - topic.LastIndexOf("/") - 1);
            }
            catch
            {
                Logger.LogInformation($"Get invalid device with topic: {topic}; Waiting somthing like - cabinet/<name of device>/...\n");
                return;
            }
            if (((command == SimnetLib.Model.MessageTypes.ReportCabinetLogin)) && (dev != ""))
            // if (((command == SimnetLib.Model.MessageTypes.ReportCabinetLogin) || (command == SimnetLib.Model.MessageTypes.QueryTheInventory)) && (dev != ""))
            {


                //string simId;
                //try
                //{
                //    RptReportCabinetLogin loginInfo = (RptReportCabinetLogin)message;
                //    simId = loginInfo.RlIccid;
                //}
                //catch
                //{
                //    simId = "";
                //}

                

                if (DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev)) < 0)
                {
                    Device device = new Device(dev, ((Server)(sender)).Id, true);
                    device.HostDeviceId = ((Server)(sender)).Id;
                    device.LastOnlineTime = DateTime.Now;
                    device.Owners = "";
                    device.Stored = false;
                    device.SimId = data.RlIccid;
                    DevicesData.Devices.Add(device);
                    Logger.LogInformation($"New device login - {dev} , try to add \n");
                    actionProcess.ActionSave((int)ActionsDescription.StationFindNew, "System", device.HostDeviceId, device.Id, 0, 0, "");
                    actionProcess.ActionSave((int)ActionsDescription.StationConnect, "System", device.HostDeviceId, device.Id, 0, 0, "");
                }
                else
                {
                    DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].LastOnlineTime = DateTime.Now;
                    DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].Online = true;
                    DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].Stored = false;
                    actionProcess.ActionSave((int)ActionsDescription.StationConnect, DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].Owners, DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].HostDeviceId, DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].Id, 0, 0, "");
                    Logger.LogInformation($"Exist device login - {dev} \n");
                }
                ((Server)(sender)).EvQueryTheInventory -= Srv_EvQueryTheInventory;
                ((Server)(sender)).EvQueryTheInventory += Srv_EvQueryTheInventory;

                ((Server)(sender)).EvReturnThePowerBank -= Srv_EvReturnThePowerBank;
                ((Server)(sender)).EvReturnThePowerBank += Srv_EvReturnThePowerBank;
                ((Server)(sender)).EvReportCabinetLogin -= Srv_EvReportCabinetLogin;
                ((Server)(sender)).EvReportCabinetLogin += Srv_EvReportCabinetLogin;


                ((Server)(sender)).SubScript(dev);
                ((Server)(sender)).CmdQueryTheInventory(dev);


            }



        }

        private void Srv_EvSniffer(object sender, string topic, object message)
        {

            string dev="";
            string command = "";
            try
            {

                dev = topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).Substring(0, topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).IndexOf('/'));
                command = topic.Substring(topic.LastIndexOf("/") + 1, topic.Length - topic.LastIndexOf("/") - 1);
            }
            catch
            {
                Logger.LogInformation($"Get invalid device with topic: {topic}; Waiting somthing like - cabinet/<name of device>/...\n");
                return;
            }
            if (((command == SimnetLib.Model.MessageTypes.ReportCabinetLogin) ) && (dev != ""))
            // if (((command == SimnetLib.Model.MessageTypes.ReportCabinetLogin) || (command == SimnetLib.Model.MessageTypes.QueryTheInventory)) && (dev != ""))
            {

                
                //string simId;
                //try
                //{
                //    RptReportCabinetLogin loginInfo = (RptReportCabinetLogin)message;
                //    simId = loginInfo.RlIccid;
                //}
                //catch
                //{
                //    simId = "";
                //}
                    


                //if (DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev)) < 0)
                //{
                //    Device device = new Device(dev, ((Server)(sender)).Id, true);
                //    device.HostDeviceId = ((Server)(sender)).Id;
                //    device.LastOnlineTime = DateTime.Now;
                //    device.Owners = "";
                //    device.Stored = false;
                //    DevicesData.Devices.Add(device);
                //    Logger.LogInformation($"New device login - {dev} , try to add \n");
                //    actionProcess.ActionSave((int)ActionsDescription.StationFindNew, "System", device.HostDeviceId, device.Id, 0,0, "");
                //    actionProcess.ActionSave((int)ActionsDescription.StationConnect, "System", device.HostDeviceId, device.Id, 0,0, "");
                //}
                //else
                //{
                //    DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].LastOnlineTime = DateTime.Now;
                //    DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].Online = true;
                //    DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].Stored = false;
                //    actionProcess.ActionSave((int)ActionsDescription.StationConnect, DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].Owners, DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].HostDeviceId, DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].Id, 0,0, "");
                //    Logger.LogInformation($"Exist device login - {dev} \n");
                //}
                //((Server)(sender)).EvQueryTheInventory -= Srv_EvQueryTheInventory;
                //((Server)(sender)).EvQueryTheInventory += Srv_EvQueryTheInventory;

                //((Server)(sender)).EvReturnThePowerBank -= Srv_EvReturnThePowerBank;
                //((Server)(sender)).EvReturnThePowerBank += Srv_EvReturnThePowerBank;


                //((Server)(sender)).SubScript(dev);
                //((Server)(sender)).CmdQueryTheInventory(dev);

   
            }
        }

        private void Srv_EvReturnThePowerBank(object sender, string topic, RptReturnThePowerBank data)
        {
            //EvReturnThePowerBank?.Invoke(this, topic, data);
            string dev = "";
            Device device;
            try
            {
                dev = topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).Substring(0, topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).IndexOf('/'));
                device = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.DeviceName == dev)];
            }
            catch
            {
                Logger.LogInformation($"Get invalid device with topic: {topic}; Waiting somthing like - cabinet/<name of device>/...\n");
                return;
            }
            Logger.LogInformation($"Powerbank {data.RlPbid} returned in device: {dev} , slot: {data.RlSlot}\n");

            device.Slots = device.Slots | ((uint)Math.Pow(2, data.RlSlot - 1));// & ~((uint)(Math.Pow(2, data.RlSlot - 1)));
            device.Online = true;
            device.LastOnlineTime = DateTime.Now;
            
            if (DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid) < 0)
            {
                DevicesData.PowerBanks.Add(new PowerBank(data.RlPbid, device.DeviceName, data.RlSlot, data.RlLock > 0 ? true : false, true,false, (PowerBankChargeLevel)data.RlQoe));
                actionProcess.ActionSave((int)ActionsDescription.PowerBankFindNew, "System", device.HostDeviceId, device.Id, data.RlPbid, data.RlSlot, "");
            }
            else
            {
                try
                {
                    if (DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Plugged)
                    {
                        return;
                    }

                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].HostDeviceName = device.DeviceName;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].HostSlot = data.RlSlot;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Locked = data.RlLock > 0 ? true : false;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Plugged = true;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Charging = data.RlLimited > 0 ? true : false;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].ChargeLevel = (PowerBankChargeLevel)data.RlQoe;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].IsOk = data.RlCode == 0 ? true : false;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Stored = false;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].LastPutTime = DateTime.Now;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Cost = DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Price * (DateTime.Now - DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].LastGetTime).Minutes / 60;

                    //if (DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Taken)
                    //{
                        //float cost = DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Price * (DateTime.Now - DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].LastGetTime).Minutes / 60;
                    //!!   EvReturnThePowerBank(device.DeviceName, data.RlPbid, data.RlSlot, DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Cost);
                        DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Taken = false;
                    //}

                    actionProcess.ActionSave((int)ActionsDescription.PowerBankInsert, DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].UserId, device.HostDeviceId, device.Id, data.RlPbid,data.RlSlot, "");
                }
                catch
                {
                    Logger.LogInformation($"Invalid device id: {data.RlPbid}; \n");
                    return;
                }
            }
             ((Server)(sender)).SrvReturnThePowerBank(data.RlSlot, 1, dev);
             device.Online = true;
             device.LastOnlineTime = DateTime.Now;
             device.Stored = false;
        }
        

        private void Srv_EvQueryTheInventory(object sender, string topic, RplQueryTheInventory data)
        {



            string dev = "";
            Device device;
            try
            {
               dev = topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).Substring(0, topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).IndexOf('/'));
               device = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.DeviceName == dev)];
              
            }
            catch
            {
                Logger.LogInformation($"Get invalid device with topic: {topic}; Waiting somthing like - cabinet/<name of device>/...\n");
                return;
            }

      
            Logger.LogInformation($"Get inventory info from device: {dev} \n");

            //ulong hostedId = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.DeviceName == dev)].Id;


            try
            {
                var devicePowerbanks = DevicesData.PowerBanks.Where(p => p.HostDeviceName == device.DeviceName).ToList();
                //var devicePowerbanks = DevicesData.PowerBanks.ToList();
                foreach (var devicePowerbank in devicePowerbanks)
                {
                    //bool pbPresent = false;
                    foreach (var pbank in data.RlBank1s)
                    {
                        if ((devicePowerbank.HostSlot == pbank.RlSlot) && devicePowerbank.Plugged && (pbank.RlPbid!=devicePowerbank.Id))
                        {
                            //pbPresent = true;

                            devicePowerbank.Plugged = false;
                            devicePowerbank.LastGetTime = DateTime.Now;
                            devicePowerbank.Taken = true;
                            actionProcess.ActionSave((int)ActionsDescription.PowerBankTake, "unknown", device.HostDeviceId, device.Id, devicePowerbank.Id, devicePowerbank.HostSlot, "");
                        }
                    }


                }
                

            }
            catch
            {
                Logger.LogInformation($"Error in the inventarisation...\n");
            }
            


            uint slots = 0;
            foreach (var pbank in data.RlBank1s)
            {

                //if ((pbank.RlIdok == 1) && (pbank.RlPbid!=0))
                if ((pbank.RlPbid != 0))
                {

                    slots = slots | ((uint)Math.Pow(2,pbank.RlSlot - 1));
                    if (DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid) < 0) 
                    {
                        DevicesData.PowerBanks.Add(new PowerBank(pbank.RlPbid, device.DeviceName, pbank.RlSlot, pbank.RlLock > 0 ? true : false, true, pbank.RlCharge > 0 ? true : false, (PowerBankChargeLevel)pbank.RlQoe));
                        actionProcess.ActionSave((int)ActionsDescription.PowerBankFindNew, "System", device.HostDeviceId, device.Id, pbank.RlPbid, pbank.RlSlot,"");
                    }
                    else
                    {
                        try
                        {

                            if (DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].Plugged == false)
                            {
                                actionProcess.ActionSave((int)ActionsDescription.PowerBankInsert, DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].UserId, device.HostDeviceId, device.Id, pbank.RlPbid, pbank.RlSlot,"");
                            }

                            DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].HostDeviceName = device.DeviceName;
                            DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].HostSlot = pbank.RlSlot;
                            DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].Locked = pbank.RlLock > 0 ? true : false;
                            DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].Plugged = true;
                            DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].Taken = false;
                            DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].Charging = pbank.RlCharge > 0 ? true : false;
                            DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].ChargeLevel = (PowerBankChargeLevel)pbank.RlQoe;
                            DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].Stored = false;
                            //DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].LastPutTime = DateTime.Now;

                           

                        }
                        catch
                        {
                            Logger.LogInformation($"Invalid device id: {pbank.RlPbid}; \n");
                            return;
                        }
                    }
                }
            }

             

            device.Online = true;
            device.LastOnlineTime = DateTime.Now;
            device.Slots = slots;
            device.Stored = false;
        }

        private void Srv_EvConnected(object sender)
        {
            ((Server)sender).ConnectTime = DateTime.Now;
            ((Server)sender).Connected = true;
            Logger.LogInformation($"Connected to Server:{((Server)sender).Host}:{((Server)sender).Port}\n");
            //((Server)sender).SubSniffer();
            ((Server)sender).EvReportCabinetLogin -= Srv_EvReportCabinetLogin;
            ((Server)sender).EvReportCabinetLogin += Srv_EvReportCabinetLogin;
            ((Server)sender).SubScriptLogin();
            ((Server)sender).Stored = false;
            actionProcess.ActionSave((int)ActionsDescription.ServerConnect, "System", ((Server)sender).Id, 0, 0, 0,"");

        }


        public int SetTypeOfUse(string deviceIdToSetTypeOfUse, int TypeOfUse, string? userId, List<string> roles)
        {
            try
            {
                Device device = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id_str == deviceIdToSetTypeOfUse)];
                device.TypeOfUse = (TypeOfUse)TypeOfUse;
                return 200;
            }
            catch
            {
                return 404;

            }

        }

        public int SetOwner(string deviceIdToOwn, string Owner, string? userId, List<string> roles)
        {
            try
            {
                Device device = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id_str == deviceIdToOwn)];
                device.Owners = Owner;
                return 200;
            }
            catch
            {
                return 404;

            }

        }

        public int Activate(string deviceIdToAct, string? userId, List<string> roles)
        {
            try
            {
                Device device = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id_str == deviceIdToAct)];
                if (!device.Activated)
                {
                    device.Activated = true;
                }
                else
                {
                    device.Activated = false;
                }



                return 200;
            }
            catch
            {
                return 404;

            }

        }


        public int Register(string deviceIdToReg, string? userId, List<string> roles)
        {
            try
            {
                Device device = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id_str == deviceIdToReg)];
                if (!device.Registered)
                {
                    device.Registered = true;
                }
                else
                {
                    device.Registered = false;
                }



                return 200;
            }
            catch
            {
                return 404;

            }

        }


        public int CanReg(string deviceIdToCanReg, string? userId, List<string> roles)
        {
            try
            {
                Device device = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id_str == deviceIdToCanReg)];
                if (!device.CanRegister)
                {
                    device.CanRegister = true;
                }
                else
                {
                    device.CanRegister = false;
                }



                return 200;
            }
            catch
            {
                return 404;

            }

        }
        //public int RemovePowerBank(string deviceName, uint numberPB, string? userId)
        //{

        //    //uint index;
        //    bool taken = false;
        //    if (numberPB == 0)
        //    {
        //        return 404;
        //    }
        //    PowerBank? powerBankPush = null;

        //    foreach (var server in DevicesData.Servers)
        //    {

        //        foreach (var powerBank in DevicesData.PowerBanks)
        //        {
        //            if ((powerBank.HostDeviceName == deviceName) && (powerBank.HostSlot == numberPB))
        //            {

        //                try
        //                {
        //                //    Device device = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.DeviceName == deviceName)];
        //                //    device.Slots = device.Slots & ~(uint)Math.Pow(2, powerBank.HostSlot - 1);

        //                //    DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)

        //                //    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)]
        //                //    taken = true;
        //                }
        //                catch
        //                {
        //                    Logger.LogInformation($"Get invalid device with topic: {deviceName}; Waiting somthing like - cabinet/<name of device>/...\n");
        //                    return 502;
        //                }
        //            }

        //        }



        //    }
        //    if (taken)
        //    {
        //        return 200;
        //    }
        //    else
        //    {
        //        return 404;
        //    }


        //}
        public ulong PushPowerBank(string deviceName, uint numberPB, string? userId, List<string> roles)
        {
            bool taken=false;
            int maxСharge = 1;
            //uint index;
            PowerBank? powerBankPush = null;
            var allowAdminAndManager = false;
            var allowAdminManagerSupport = false;
            var allowAdmin = false;
            ulong pushPbId=404;
            try
            {

                if (!string.IsNullOrEmpty(userId))
                {

                    ////var result = AsyncContext.RunTask(MyAsyncMethod).Result;
                    ////var userId = userManager.GetUserId(User);
                    //var usTask = Task.Run(async () => await userManager.FindByIdAsync(userId));
                    //var user = usTask.Result;
                    ////var user = await userManager.FindByIdAsync(userId);
                    //var rlTask = Task.Run(async () => await userManager.GetRolesAsync(user));
                    //var roles = rlTask.Result;
                    var filterNotMoney = roles.Contains("support") || roles.Contains("manager");
                    allowAdminAndManager = roles.Contains("admin") || roles.Contains("manager");
                    allowAdminManagerSupport = roles.Contains("support") || roles.Contains("admin") || roles.Contains("manager");
                    allowAdmin = roles.Contains("admin");

                }
                else
                {
                    userId = "huj";
                }

            }
            catch
            {
                Logger.LogInformation($"Error in getting user \n");
            }






                if (numberPB == 0)
                {

                    foreach (var powerBank in DevicesData.PowerBanks)
                    {
                        
                        
                        if ((powerBank.Plugged) && (powerBank.HostDeviceName == deviceName))
                        {
                            if ((int)powerBank.ChargeLevel >= maxСharge)
                            {
                                maxСharge = (int)powerBank.ChargeLevel;
                                powerBankPush = powerBank;
                                //index = powerBank.HostSlot;
                            }
                        }
                    }

                    //foreach (var powerBank in DevicesData.PowerBanks)
                    //{

                    //   if ((powerBank.Plugged) && (powerBank.HostDeviceName == deviceName))
                    //  {



                    if (powerBankPush != null)
                    {


                        try
                        {
                            Device device = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.DeviceName == deviceName)];
                            device.Slots = device.Slots & ~(uint)Math.Pow(2, powerBankPush.HostSlot - 1);
                            Server server = DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == device.HostDeviceId)];
                            actionProcess.ActionSave((int)ActionsDescription.PowerBankTake, userId, device.HostDeviceId, device.Id, powerBankPush.Id, powerBankPush.HostSlot, "");
                            powerBankPush.Plugged = false;
                            server.CmdPushPowerBank(powerBankPush.HostSlot, powerBankPush.HostDeviceName);                          
                            powerBankPush.LastGetTime = DateTime.Now;
                            if (allowAdminAndManager)
                            {


                                powerBankPush.Price = 0;

                            }
                            powerBankPush.UserId = userId;
                            powerBankPush.Taken = true;
                            taken = true;
                            pushPbId = powerBankPush.Id;

                            DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == device.HostDeviceId)].CmdQueryTheInventory(device.DeviceName);                            
                        }
                        catch
                        {
                            Logger.LogInformation($"Get invalid device with topic: {deviceName}; Waiting somthing like - cabinet/<name of device>/...\n");
                            return 502;
                        }
                    }
                    else
                    {
                        Logger.LogInformation($"Avialable powerbank not found...\n");
                        return 503;
                    }
                                
                          //  break;
                      //  }

                   // }

                }
                else
                {
                    foreach (var powerBank in DevicesData.PowerBanks)
                    {
                        if ((powerBank.Plugged) && (powerBank.HostDeviceName == deviceName) &&(powerBank.HostSlot == numberPB)) 
                        {

                            try
                            {
                                Device device = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.DeviceName == deviceName)];
                                device.Slots = device.Slots & ~(uint)Math.Pow(2, powerBank.HostSlot - 1);
                                device.Stored = false;
                                Server server = DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == device.HostDeviceId)];
                                actionProcess.ActionSave((int)ActionsDescription.PowerBankTake, userId, device.HostDeviceId, device.Id, powerBank.Id, powerBank.HostSlot, "");
                                powerBank.Plugged = false;
                                server.CmdPushPowerBank(numberPB, deviceName);                                
                                powerBank.Stored = false;
                                powerBank.LastGetTime = DateTime.Now;
                                if (allowAdminAndManager)
                                {


                                    powerBank.Price = 0;

                                }
                                powerBank.UserId = userId;
                                powerBank.Taken = true;
                                taken = true;
                                pushPbId = powerBank.Id;

                            DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == device.HostDeviceId)].CmdQueryTheInventory(device.DeviceName);

                            }
                            catch
                            {
                                Logger.LogInformation($"Get invalid device with topic: {deviceName}; Waiting somthing like - cabinet/<name of device>/...\n");
                                return 502;
                            }
                        }
                            
                    }
                }




            return pushPbId;


            //if (taken)
            //{                                          
            //    return pushPbId;
            //}
            //else
            //{
            //    return 404;
            //}


        }

        private void Srv_EvDisconnected(object sender)
        {

            ((Server)sender).DisconnectTime = DateTime.Now;

            ((Server)sender).ConnectTime = DateTime.Now;
            ((Server)sender).Connected = false;
             Logger.LogInformation($"Disconnected from Server:{((Server)sender).Host}:{((Server)sender).Port}\n");
            ((Server)sender).Stored = false;
            actionProcess.ActionSave((int)ActionsDescription.ServerDisconnect, "System", ((Server)sender).Id, 0, 0, 0, "");
        }
        private void Srv_EvConnectError(object sender, string error)
        {
            ((Server)sender).Error = error;
            ((Server)sender).ConnectTime = DateTime.Now;
            ((Server)sender).Stored = false;
            Logger.LogInformation($"Error in connect to {((Server)sender).Host}:{((Server)sender).Port} : {error}\n");
        }

        private ulong GetGUID(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            Byte[] bId = new Guid(hash).ToByteArray();
            ulong result = BitConverter.ToUInt64(bId, 0);
            return result;
        }


    }
}