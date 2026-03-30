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
using System.Drawing;
using WebServer.Controllers.User;
using WebServer.Services.Stripe;
using Stripe.Forwarding;
using WebServer.Models.Stripe;
using WebServer.Models.User;
using WebServer.Utils.Requests;
using Stripe;
using WebServer.Services;
using WebServer.Services.Pricing;

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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPricingService _pricingService;

        // Lock для thread-safe операций с PowerBanks
        private readonly object _powerBanksLock = new object();

        // DeviceContext dbDevice;

        //  public ScanDevices(List<Server> Servers, ILogger<ScanDevices> logger, IServiceScopeFactory scopeFactory)
        //  public ScanDevices(ILogger<ScanDevices> logger, IDevicesData devicesData, IServiceScopeFactory scopeFactory, UserManager<AppUser> _userManager)
        public ScanDevices(ILogger<ScanDevices> logger, IDevicesData devicesData, IServiceScopeFactory scopeFactory, IPricingService pricingService)
        {
            _scopeFactory = scopeFactory;
            DevicesData = devicesData;
            Logger = logger;
            scope = scopeFactory.CreateScope();
            _pricingService = pricingService;
            //           userManager = _userManager;


            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.IsNullOrEmpty(environmentName))
                environmentName = "Development";

            config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: false)
            .AddSecretsConfiguration()
            .Build();


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
            try
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
            catch (Exception ex)
            {
              
               Logger.LogError(ex, "An error in update database");             
                return null;

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
                try
                {
                    ReconnectServers();
                    UpdateDb();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Critical error in ScanDevices worker");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
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
                    try
                    {
                        if (powbank.SessionId != "" && powbank.SessionId != "\"\"")
                        {
                            // Get device to determine TypeOfUse and pricing plan
                            var pbDevice = DevicesData.Devices.FirstOrDefault(d => d.Id == powbank.HostDeviceId);
                            var typeOfUse = pbDevice?.TypeOfUse ?? TypeOfUse.PayByCard;
                            var pricingPlan = _pricingService.GetPlan(typeOfUse);

                            powbank.Cost = _pricingService.CalculateCost(typeOfUse, powbank.LastGetTime, powbank.Taken);
                            if ((DateTime.Now - powbank.LastGetTime).Days >= pricingPlan.MaxDaysBeforeCapture)
                            {

                                var stripeCapture = new StripeCapture
                                {
                                    SessionId = powbank.SessionId,
                                    StationId = powbank.HostDeviceId,
                                    PowerBankId = powbank.Id,
                                    Amount = pricingPlan.HoldAmount,
                                };

                                using (var stripeScope = _scopeFactory.CreateScope())
                                {
                                    var stripeRoutines = stripeScope.ServiceProvider.GetRequiredService<IStripeRoutines>();
                                    stripeRoutines.MakePostPayment(stripeCapture);
                                }
                                powbank.SessionId = "";
                            }

                        }


                        if (!powbank.Stored)
                        {

                            AddOrUpdate(dbDevice.PowerBank, dbDevice, c => c.Id, powbank);
                            powbank.Stored = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error processing powerbank");
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
                try
                {
                    dbDevice.SaveChanges();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An error in update database");
                }

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
                        DevicesData.PowerBanks.Add(powerbank);
                        var hostDevice = DevicesData.Devices.GetById(powerbank.HostDeviceId);
                        if (hostDevice != null)
                        {
                            var servId = hostDevice.HostDeviceId;
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
                        var server = DevicesData.Servers.GetById(dev.HostDeviceId);
                        if (server == null) continue;

                        server.EvQueryTheInventory -= Srv_EvQueryTheInventory;
                        server.EvQueryTheInventory += Srv_EvQueryTheInventory;
                        server.EvReturnThePowerBank -= Srv_EvReturnThePowerBank;
                        server.EvReturnThePowerBank += Srv_EvReturnThePowerBank;
                        server.EvReportCabinetLogin -= Srv_EvReportCabinetLogin;
                        server.EvReportCabinetLogin += Srv_EvReportCabinetLogin;
                        server.SubScript(dev.DeviceName);
                        server.CmdQueryTheInventory(dev.DeviceName);
                    }

                }
                catch
                {

                }


            }
        }

        /// <summary>
        /// Перезагружает данные из базы данных.
        /// Очищает текущие коллекции и загружает свежие данные из БД.
        /// Используется для подхвата изменений, внесённых напрямую в БД.
        /// </summary>
        public async Task<(int devices, int powerBanks)> ReloadFromDatabaseAsync()
        {
            Logger.LogInformation("Starting database reload...");

            using var dbDevice = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<DeviceContext>();

            // Сохраняем текущее состояние серверов (соединения)
            var serverConnections = DevicesData.Servers.ToDictionary(s => s.Id, s => s.Connected);

            // Очищаем только Devices и PowerBanks, серверы остаются (они управляют соединениями)
            DevicesData.Devices.Clear();
            DevicesData.PowerBanks.Clear();

            int devicesCount = 0;
            int powerBanksCount = 0;

            try
            {
                // Загружаем устройства
                foreach (var dev in dbDevice.Device)
                {
                    dev.Online = false;
                    dev.Stored = true; // Уже в БД
                    DevicesData.Devices.Add(dev);
                    devicesCount++;
                }
                Logger.LogInformation($"Loaded {devicesCount} devices from database");

                // Загружаем павербанки
                foreach (var powerbank in dbDevice.PowerBank)
                {
                    powerbank.Stored = true; // Уже в БД
                    DevicesData.PowerBanks.Add(powerbank);
                    powerBanksCount++;
                }
                Logger.LogInformation($"Loaded {powerBanksCount} powerbanks from database");

                // Переподписываемся на события устройств
                foreach (var dev in DevicesData.Devices)
                {
                    var server = DevicesData.Servers.GetById(dev.HostDeviceId);
                    if (server == null) continue;

                    server.EvQueryTheInventory -= Srv_EvQueryTheInventory;
                    server.EvQueryTheInventory += Srv_EvQueryTheInventory;
                    server.EvReturnThePowerBank -= Srv_EvReturnThePowerBank;
                    server.EvReturnThePowerBank += Srv_EvReturnThePowerBank;
                    server.EvReportCabinetLogin -= Srv_EvReportCabinetLogin;
                    server.EvReportCabinetLogin += Srv_EvReportCabinetLogin;
                    server.SubScript(dev.DeviceName);
                    server.CmdQueryTheInventory(dev.DeviceName);
                }

                Logger.LogInformation("Database reload completed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during database reload");
                throw;
            }

            return (devicesCount, powerBanksCount);
        }


        private void ReconnectServers()
        {
            var now = DateTime.Now;

            foreach (var srv in DevicesData.Servers.ToList())
            {
                if (srv == null)
                    continue;

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

                        foreach (var dev in DevicesData.Devices.ToList())
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

                        foreach (var dev in DevicesData.Devices.ToList())
                        {
                            if (dev.HostDeviceId == srv.Id)
                            {
                                if ((now - dev.LastOnlineTime).TotalSeconds > srv.OnlineTimeOut)
                                {
                                    if (dev.Online == true)
                                    {
                                        actionProcess.ActionSave(
                                            (int)ActionsDescription.StationDisconnect,
                                            "System",
                                            dev.HostDeviceId,
                                            dev.Id,
                                            0,
                                            0,
                                            "");
                                    }

                                    dev.Online = false;

                                    //dev.Slots = 0;
                                    //foreach (var powerbank in DevicesData.PowerBanks)
                                    //{
                                    //   if (powerbank.HostDeviceName == dev.DeviceName)
                                    //   {
                                    //       powerbank.Plugged = false;
                                    //   }
                                    //}

                                    srv.EvQueryTheInventory -= Srv_EvQueryTheInventory;
                                    srv.EvQueryTheInventory += Srv_EvQueryTheInventory;

                                    srv.EvReturnThePowerBank -= Srv_EvReturnThePowerBank;
                                    srv.EvReturnThePowerBank += Srv_EvReturnThePowerBank;

                                    //srv.EvReportCabinetLogin -= Srv_EvReportCabinetLogin;
                                    //srv.EvReportCabinetLogin += Srv_EvReportCabinetLogin;
                                }

                                srv.SubScript(dev.DeviceName);

                                if (dev.LastInventoryTime + (scanDevicePeriod * TimeSpan.TicksPerSecond) < now.Ticks)
                                {
                                    dev.LastInventoryTime = now.Ticks;
                                    srv.CmdQueryTheInventory(dev.DeviceName);
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

                

                var deviceId = GetGUID(dev);
                var existingDevice = DevicesData.Devices.GetById(deviceId);

                if (existingDevice == null)
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
                    existingDevice.LastOnlineTime = DateTime.Now;
                    existingDevice.Online = true;
                    existingDevice.Stored = false;
                    actionProcess.ActionSave((int)ActionsDescription.StationConnect, existingDevice.Owners, existingDevice.HostDeviceId, existingDevice.Id, 0, 0, "");
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
            Device? device;
            try
            {
                dev = topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).Substring(0, topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).IndexOf('/'));
                device = DevicesData.Devices.FirstOrDefault(item => item.DeviceName == dev);
                if (device == null)
                {
                    Logger.LogInformation($"Device not found: {dev}\n");
                    return;
                }
            }
            catch
            {
                Logger.LogInformation($"Get invalid device with topic: {topic}; Waiting somthing like - cabinet/<name of device>/...\n");
                return;
            }
            Logger.LogInformation($"Powerbank {data.RlPbid} returned in device: {dev} , slot: {data.RlSlot}\n");

            device.Slots = device.Slots | ((uint)Math.Pow(2, data.RlSlot - 1));
            device.Online = true;
            device.LastOnlineTime = DateTime.Now;

            lock (_powerBanksLock)
            {
                var pb = DevicesData.PowerBanks.GetById(data.RlPbid);
                if (pb == null)
                {
                    DevicesData.PowerBanks.Add(new PowerBank(data.RlPbid, device.DeviceName, data.RlSlot, data.RlLock > 0, true, false, (PowerBankChargeLevel)data.RlQoe));
                    actionProcess.ActionSave((int)ActionsDescription.PowerBankFindNew, "System", device.HostDeviceId, device.Id, data.RlPbid, data.RlSlot, "");
                }
                else
                {
                    try
                    {

                        // Если powerbank уже plugged в этом же месте - ничего не делаем
                        if (pb.Plugged && pb.HostDeviceName == device.DeviceName && pb.HostSlot == data.RlSlot)
                        {
                            return;
                        }

                   

                    pb.HostDeviceName = device.DeviceName;
                    pb.HostSlot = data.RlSlot;
                    pb.Locked = data.RlLock > 0 ? true : false;
                    pb.Plugged = true;
                    pb.Charging = data.RlLimited > 0 ? true : false;
                    pb.ChargeLevel = (PowerBankChargeLevel)data.RlQoe;
                    pb.IsOk = data.RlCode == 0 ? true : false;
                    pb.LastPutTime = DateTime.Now;
                    //pb.Cost = pb.Price * (DateTime.Now - pb.LastGetTime).Minutes / 60;
                    //pb.Cost = (float)Math.Round(pb.Taken ? (2 + Math.Round(((DateTime.Now - pb.LastGetTime).Hours) * pb.Price / 6)) : pb.Cost);
                    pb.Cost = _pricingService.CalculateCost(device.TypeOfUse, pb.LastGetTime, pb.Taken);
                    //if (DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Taken)
                    //{
                    //float cost = DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Price * (DateTime.Now - DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].LastGetTime).Minutes / 60;
                    //!!   EvReturnThePowerBank(device.DeviceName, data.RlPbid, data.RlSlot, DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Cost);

                    // Если павербанк был взят пользователем - записываем событие возврата
                    var wasTaken = pb.Taken;
                    var returnUserId = pb.UserId;

                    pb.Taken = false;
                    pb.Stored = false;
                    //}
                    if (pb.SessionId != "")
                    {
                        try
                        {

                            //Если сразу снимаем деньги а потом возвращаем:
                            //var refundRequest = new RefundRequest
                            //{
                            //    SessionId = pb.SessionId,
                            //    StationId = device.Id,
                            //    PowerBankId = pb.Id,
                            //    Amount = pb.Price - pb.Cost,
                            //};
                            //var refundService = _stripeRoutines.RefundPayment(refundRequest);



                            //Если сначла захватываем а потом списываем:
                            if (pb.Cost > 0)
                            {

                                var stripeCapture = new StripeCapture
                                {
                                    SessionId = pb.SessionId,
                                    StationId = device.Id,
                                    PowerBankId = pb.Id,
                                    Amount = pb.Cost,
                                };
                                using (var stripeScope = _scopeFactory.CreateScope())
                                {
                                    var stripeRoutines = stripeScope.ServiceProvider.GetRequiredService<IStripeRoutines>();
                                    stripeRoutines.MakePostPayment(stripeCapture);
                                }

                            }
                            else
                            {
                                var stripeCapture = new StripeCapture
                                {
                                    SessionId = pb.SessionId,
                                    StationId = device.Id,
                                    PowerBankId = pb.Id,
                                    Amount = pb.Cost,
                                };
                                using (var stripeScope = _scopeFactory.CreateScope())
                                {
                                    var stripeRoutines = stripeScope.ServiceProvider.GetRequiredService<IStripeRoutines>();
                                    stripeRoutines.ReleaseHeldPayment(stripeCapture);
                                }
                            }



                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Problem with refund: {ex}; \n");
                            return;

                        }
                    }


                        pb.SessionId = "";
                        pb.Stored = false;

                        // Записываем событие: PowerBankReturn если был взят пользователем, иначе PowerBankInsert
                        var userId = string.IsNullOrEmpty(returnUserId) ? "" : returnUserId;
                        if (wasTaken)
                        {
                            actionProcess.ActionSave((int)ActionsDescription.PowerBankReturn, userId, device.HostDeviceId, device.Id, data.RlPbid, data.RlSlot, $"Cost: {pb.Cost:F2}");
                            Logger.LogInformation($"PowerBank {data.RlPbid} returned by user {userId}, cost: {pb.Cost:F2}");
                        }
                        else
                        {
                            actionProcess.ActionSave((int)ActionsDescription.PowerBankInsert, userId, device.HostDeviceId, device.Id, data.RlPbid, data.RlSlot, "");
                        }
                    }
                    catch
                    {
                        Logger.LogInformation($"Invalid device id: {data.RlPbid}; \n");
                        return;
                    }
                }
            } // end lock

            ((Server)(sender)).SrvReturnThePowerBank(data.RlSlot, 1, dev);
             device.Online = true;
             device.LastOnlineTime = DateTime.Now;
             device.Stored = false;
        }
        

        private void Srv_EvQueryTheInventory(object sender, string topic, RplQueryTheInventory data)
        {
            string dev = "";
            Device? device;
            try
            {
                dev = topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).Substring(0, topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).IndexOf('/'));
                device = DevicesData.Devices.FirstOrDefault(item => item.DeviceName == dev);
                if (device == null)
                {
                    Logger.LogInformation($"Device not found: {dev}\n");
                    return;
                }
            }
            catch
            {
                Logger.LogInformation($"Get invalid device with topic: {topic}; Waiting somthing like - cabinet/<name of device>/...\n");
                return;
            }

            Logger.LogInformation($"Get inventory info from device: {dev} \n");

            // Создаём set ID powerbank'ов из текущего inventory для быстрой проверки
            var inventoryPbIds = new HashSet<ulong>(data.RlBank1s.Where(p => p.RlPbid != 0).Select(p => p.RlPbid));

            uint slots = 0;

            lock (_powerBanksLock)
            {
                // 1. Помечаем powerbank'и которые были plugged в этом устройстве, но их нет в inventory - значит взяли
                var devicePowerbanks = DevicesData.PowerBanks.Where(p => p.HostDeviceName == device.DeviceName && p.Plugged).ToList();
                foreach (var devicePowerbank in devicePowerbanks)
                {
                    if (!inventoryPbIds.Contains(devicePowerbank.Id))
                    {
                        // Powerbank был plugged в этом устройстве, но его нет в inventory - значит взяли
                        devicePowerbank.Stored = false;
                        devicePowerbank.Plugged = false;
                        devicePowerbank.LastGetTime = DateTime.Now;
                        devicePowerbank.Taken = true;
                        var userId = string.IsNullOrEmpty(devicePowerbank.UserId) ? "unknown" : devicePowerbank.UserId;
                        actionProcess.ActionSave((int)ActionsDescription.PowerBankTake, userId, device.HostDeviceId, device.Id, devicePowerbank.Id, devicePowerbank.HostSlot, "");
                        Logger.LogInformation($"PowerBank {devicePowerbank.Id} taken from device {dev} slot {devicePowerbank.HostSlot}");
                    }
                }

                // 2. Обрабатываем powerbank'и из inventory
                foreach (var pbank in data.RlBank1s)
                {
                    if (pbank.RlPbid != 0)
                    {
                        slots = slots | ((uint)Math.Pow(2, pbank.RlSlot - 1));

                        var pb = DevicesData.PowerBanks.GetById(pbank.RlPbid);
                        if (pb == null)
                        {
                            // Новый powerbank - добавляем
                            DevicesData.PowerBanks.Add(new PowerBank(pbank.RlPbid, device.DeviceName, pbank.RlSlot,
                                pbank.RlLock > 0, true, pbank.RlCharge > 0, (PowerBankChargeLevel)pbank.RlQoe));
                            actionProcess.ActionSave((int)ActionsDescription.PowerBankFindNew, "System", device.HostDeviceId, device.Id, pbank.RlPbid, pbank.RlSlot, "");
                            Logger.LogInformation($"New PowerBank {pbank.RlPbid} found in device {dev} slot {pbank.RlSlot}");
                        }
                        else
                        {

                            // Если powerbank был не plugged (или был в другом устройстве) - это insert
                            if (!pb.Plugged || pb.HostDeviceName != device.DeviceName)
                            {
                                var userId = string.IsNullOrEmpty(pb.UserId) ? "" : pb.UserId;
                                actionProcess.ActionSave((int)ActionsDescription.PowerBankInsert, userId, device.HostDeviceId, device.Id, pbank.RlPbid, pbank.RlSlot, "");
                                Logger.LogInformation($"PowerBank {pbank.RlPbid} inserted in device {dev} slot {pbank.RlSlot}");
                            }

                            // Обновляем данные powerbank'а
                            pb.HostDeviceName = device.DeviceName;
                            pb.HostSlot = pbank.RlSlot;
                            pb.Locked = pbank.RlLock > 0;
                            pb.Plugged = true;
                            pb.Taken = false;
                            pb.Charging = pbank.RlCharge > 0;
                            pb.ChargeLevel = (PowerBankChargeLevel)pbank.RlQoe;
                            pb.Stored = false;
                        }
                    }
                }
            }

            device.Slots = slots;
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
            var device = DevicesData.Devices.FirstOrDefault(item => item.Id_str == deviceIdToSetTypeOfUse);
            if (device == null) return 404;

            device.TypeOfUse = (TypeOfUse)TypeOfUse;
            device.Stored = false;
            return 200;
        }

        public int SetOwner(string deviceIdToOwn, string Owner, string? userId, List<string> roles)
        {
            var device = DevicesData.Devices.FirstOrDefault(item => item.Id_str == deviceIdToOwn);
            if (device == null) return 404;

            device.Owners = Owner;
            device.Stored = false;
            return 200;
        }

        public int SetDescription(string deviceId, string description, string? userId, List<string> roles)
        {
            var device = DevicesData.Devices.FirstOrDefault(item => item.Id_str == deviceId);
            if (device == null) return 404;

            device.Description = description;
            device.Stored = false;
            return 200;
        }

        public int Activate(string deviceIdToAct, string? userId, List<string> roles)
        {
            var device = DevicesData.Devices.FirstOrDefault(item => item.Id_str == deviceIdToAct);
            if (device == null) return 404;

            device.Activated = !device.Activated;
            device.Stored = false;
            return 200;
        }


        public int Register(string deviceIdToReg, string? userId, List<string> roles)
        {
            var device = DevicesData.Devices.FirstOrDefault(item => item.Id_str == deviceIdToReg);
            if (device == null) return 404;

            device.Registered = !device.Registered;
            device.Stored = false;
            return 200;
        }

        public int CanReg(string deviceIdToCanReg, string? userId, List<string> roles)
        {
            var device = DevicesData.Devices.FirstOrDefault(item => item.Id_str == deviceIdToCanReg);
            if (device == null) return 404;

            device.CanRegister = !device.CanRegister;
            device.Stored = false;
            return 200;
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

                    foreach (var powerBank in DevicesData.PowerBanks.ToList())
                    {
                        if ((powerBank.Plugged) && (powerBank.HostDeviceName == deviceName))
                        {
                            if ((int)powerBank.ChargeLevel >= maxСharge)
                            {
                                maxСharge = (int)powerBank.ChargeLevel;
                                powerBankPush = powerBank;
                            }
                        }
                    }

                //foreach (var powerBank in DevicesData.PowerBanks)
                //{

                //   if ((powerBank.Plugged) && (powerBank.HostDeviceName == deviceName))
                //  {



                if (powerBankPush != null)
                    {
                        var device = DevicesData.Devices.FirstOrDefault(item => item.DeviceName == deviceName);
                        if (device == null)
                        {
                            Logger.LogInformation($"Device not found: {deviceName}\n");
                            return 502;
                        }

                        var server = DevicesData.Servers.GetById(device.HostDeviceId);
                        if (server == null)
                        {
                            Logger.LogInformation($"Server not found for device: {deviceName}\n");
                            return 502;
                        }

                        device.Slots = device.Slots & ~(uint)Math.Pow(2, powerBankPush.HostSlot - 1);
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
                        powerBankPush.Stored = false;
                        taken = true;
                        pushPbId = powerBankPush.Id;

                        server.CmdQueryTheInventory(device.DeviceName);
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
                    Logger.LogInformation($"PushPowerBank: Looking for powerbank in slot {numberPB} device {deviceName}");

                    var powerBank = DevicesData.PowerBanks.FirstOrDefault(p =>
                        p.Plugged && p.HostDeviceName == deviceName && p.HostSlot == numberPB);

                    if (powerBank != null)
                    {
                        Logger.LogInformation($"PushPowerBank: Found powerbank {powerBank.Id}");

                        var device = DevicesData.Devices.FirstOrDefault(item => item.DeviceName == deviceName);
                        if (device == null)
                        {
                            Logger.LogInformation($"Device not found: {deviceName}\n");
                            return 502;
                        }

                        var server = DevicesData.Servers.GetById(device.HostDeviceId);
                        if (server == null)
                        {
                            Logger.LogInformation($"Server not found for device: {deviceName}\n");
                            return 502;
                        }

                        device.Slots = device.Slots & ~(uint)Math.Pow(2, powerBank.HostSlot - 1);
                        actionProcess.ActionSave((int)ActionsDescription.PowerBankTake, userId, device.HostDeviceId, device.Id, powerBank.Id, powerBank.HostSlot, "");
                        powerBank.Plugged = false;
                        server.CmdPushPowerBank(numberPB, deviceName);
                        powerBank.LastGetTime = DateTime.Now;
                        if (allowAdminAndManager)
                        {
                            powerBank.Price = 0;
                        }
                        powerBank.UserId = userId;
                        powerBank.Taken = true;
                        taken = true;
                        pushPbId = powerBank.Id;
                        powerBank.Stored = false;

                        server.CmdQueryTheInventory(device.DeviceName);
                    }
                    else
                    {
                        Logger.LogWarning($"PushPowerBank: Powerbank not found for slot {numberPB} device {deviceName}. Checking all powerbanks in device...");
                        foreach (var pb in DevicesData.PowerBanks.Where(p => p.HostDeviceName == deviceName))
                        {
                            Logger.LogInformation($"  - PB {pb.Id}: Slot={pb.HostSlot}, Plugged={pb.Plugged}, Taken={pb.Taken}, Reserved={pb.Reserved}");
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