using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProtoBuf.Meta;
using SimnetLib;
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using WebServer.Models.Device;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebServer.Workers
{
    public class ScanDevices : BackgroundService
    {
        public List<Server> servers;
        public List<WebServer.Models.Device.Device> devices;
        public List<PowerBank> powerbanks;
        public readonly IServiceScopeFactory ScopeFactory;
        private readonly ILogger<ScanDevices> Logger;
        IServiceScope scope;
        // DeviceContext dbDevice;

        //  public ScanDevices(List<Server> Servers, ILogger<ScanDevices> logger, IServiceScopeFactory scopeFactory)
        public ScanDevices(ILogger<ScanDevices> logger, IServiceScopeFactory scopeFactory)
        {
            ScopeFactory = scopeFactory;
            //servers = Servers;
            Logger = logger;
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


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            //var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();
            scope = ScopeFactory.CreateScope();
            //dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>();



            //var Servers = scope.ServiceProvider.GetRequiredService<Server>();            
            //            PowerBank[] powerBanks = new PowerBank[4];
            //            powerBanks[0] = new PowerBank { Id = 1, ChargeLevel = 0, Charging = true, ClientTime = DateTimeOffset.Now, LastGetTime = DateTime.Now, LastPutTime = DateTime.Now, Locked = true, Plugged = true, Price = 100 };
            //            powerBanks[1] = new PowerBank { Id = 2, ChargeLevel = 0, Charging = true, ClientTime = DateTimeOffset.Now, LastGetTime = DateTime.Now, LastPutTime = DateTime.Now, Locked = true, Plugged = true, Price = 100 };
            //            powerBanks[2] = new PowerBank { Id = 3 };
            //            powerBanks[3] = new PowerBank { Id = 4 };
            Server server = new Server("yaup.ru", 8884, "devclient", "Potato345!", 30);

            //Models.Device.Device device = new Models.Device.Device("Derv", 156, true);
            try
            {
                using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
                {
                    if ((dbDevice.Server.Any(o => o.Id != server.Id)) || (dbDevice.Server.Count() == 0))
                    {
                        dbDevice.Server.Add(server);
                        dbDevice.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                //dbDevice.Entry(server).State = EntityState.Detached;
                HandleDbException(ex);
            }


            //try
            //{
            //    Models.Device.Device device = new Models.Device.Device(123, 156, false, 3, DateTime.Now, DateTime.Now, "10.0.0.1", "yaup.ru", "hhh.hh");
            //    using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
            //    {
            //        if (dbDevice.Device.Any(o => o.Id != device.Id))
            //        {
            //            dbDevice.Device.Add(device);
            //            dbDevice.SaveChanges();
            //        }
            //    }

            //}
            //catch (Exception ex)
            //{
            //    //dbDevice.Entry(device).State = EntityState.Detached;
            //    HandleDbException(ex);
            //}

            servers = new List<Server>();
            devices = new List<WebServer.Models.Device.Device>();
            powerbanks = new List<PowerBank>();
            ulong tmpId;

            InitServers();

            while (!stoppingToken.IsCancellationRequested)
            {
                ReconnectServers();
                await Task.Delay(TimeSpan.FromSeconds(10));
            }

        }

        private void InitServers()
        {

            using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
            {
                foreach (var srv in dbDevice.Server)
                {
                    if (!srv.Init)
                    {
                        // servers.Add(new(srv.Host, srv.Port, srv.Login,srv.Password,srv.ReconnectTime)); ;
                        servers.Add(srv);
                        //srv.Init = true;
                        //srv.EvConnected += Srv_EvConnected;
                        //srv.EvDisconnected += Srv_EvDisconnected;
                        //srv.Connect();
                    }
                }
            }
        }

        private void ReconnectServers()
        {
            foreach (var srv in servers)
            {
                if ((srv != null))
                {
                    if (!srv.Connected)
                    {
                        srv.EvConnected += Srv_EvConnected;
                        srv.EvConnectError += Srv_EvConnectError;
                        srv.EvDisconnected += Srv_EvDisconnected;
                        srv.EvSubSniffer += Srv_EvSniffer;
                        srv.Connect();
                    }


                }

            }

        }
        private void Srv_EvSniffer(object sender, string topic, object message)
        {
            try
            {

                string dev = topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).Substring(0, topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).IndexOf('/'));
                string command = topic.Substring(topic.LastIndexOf("/") + 1, topic.Length - topic.LastIndexOf("/") - 1);
                if (command == SimnetLib.Model.MessageTypes.ReportCabinetLogin)
                {
                    if (devices.FindIndex(item => item.Id == GetGUID(dev))<0)
                    {
                        devices.Add(new Models.Device.Device(dev, ((Server)(sender)).Id, true));
                    }
                    else
                    {
                        devices[devices.FindIndex(item => item.Id == GetGUID(dev))].LastOnlineTime = DateTime.Now;
                    }



                    using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
                    {
                        if (dbDevice.Device.Any(o => o.Id != GetGUID(dev)) || (dbDevice.Device.Count() == 0))
                        {
                            dbDevice.Device.Add(new Models.Device.Device(dev, ((Server)(sender)).Id, true));
                            dbDevice.SaveChanges();
                            ((Server)(sender)).SubScript(dev);
                            ((Server)(sender)).EvQueryTheInventory += Srv_EvQueryTheInventory;
                            ((Server)(sender)).CmdQueryTheInventory(dev);

                            Logger.LogInformation($"New device login - {dev} , try to add \n");
                        }
                        else
                        {
                            var device = dbDevice.Device.FirstOrDefault(item => item.Id == GetGUID(dev));
                            if (device != null)
                            {
                                device.LastOnlineTime = DateTime.Now;
                                dbDevice.SaveChanges();
                                ((Server)(sender)).SubScript(dev);
                                ((Server)(sender)).EvQueryTheInventory += Srv_EvQueryTheInventory;
                                ((Server)(sender)).CmdQueryTheInventory(dev);
                            }
                            Logger.LogInformation($"Exist device login - {dev} \n");
                        }
                    }
                }
            }
            catch
            {
                Logger.LogInformation($"Get invalid device with topic: {topic}; Waiting somthing like - cabinet/<name of device>/...\n");
            }


        }

        private void Srv_EvQueryTheInventory(object sender, string topic, RplQueryTheInventory data)
        {
            string dev = topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).Substring(0, topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).IndexOf('/'));
            Logger.LogInformation($"Get inventory info from device: {dev} \n");
            try
            {

                using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
                {
                    uint slots = 0;
                    foreach (var pbank in data.RlBank1s)
                    {

                        if (pbank.RlIdok == 1)
                        {

                            if (powerbanks.FindIndex(item => item.Id == pbank.RlPbid) < 0)
                            {
                                powerbanks.Add(new PowerBank(pbank.RlPbid, ((Server)(sender)).Id, pbank.RlLock > 0 ? true : false, true, pbank.RlCharge > 0 ? true : false, (PowerBankChargeLevel)pbank.RlQoe));
                            }
                            else
                            {
                                powerbanks[powerbanks.FindIndex(item => item.Id == pbank.RlPbid)].Locked = pbank.RlLock > 0 ? true : false;
                                powerbanks[powerbanks.FindIndex(item => item.Id == pbank.RlPbid)].Plugged = true;
                                powerbanks[powerbanks.FindIndex(item => item.Id == pbank.RlPbid)].Charging = pbank.RlCharge > 0 ? true : false;
                                powerbanks[powerbanks.FindIndex(item => item.Id == pbank.RlPbid)].ChargeLevel = (PowerBankChargeLevel)pbank.RlQoe;

                            }


                            if (dbDevice.PowerBank.Any(o => o.Id != pbank.RlPbid) || (dbDevice.PowerBank.Count() == 0))
                            {

                                slots = slots | (2^(pbank.RlSlot-1));
                                Logger.LogInformation($"Get info about powerbank in slot: {pbank.RlSlot.ToString()} \n");


                                    dbDevice.PowerBank.Add(new PowerBank(pbank.RlPbid, ((Server)(sender)).Id, pbank.RlLock > 0 ? true : false, true, pbank.RlCharge > 0 ? true : false, (PowerBankChargeLevel)pbank.RlQoe)); ;
//                                  bool locked, bool plugged, bool charging, PowerBankChargeLevel chargeLevel


                            }
                            else
                            {

                                var powerbank = dbDevice.PowerBank.FirstOrDefault(item => item.Id == pbank.RlPbid);
                                if (powerbank != null)
                                {
                                    powerbank.Locked = pbank.RlLock > 0 ? true : false;
                                    powerbank.Plugged = true;
                                    powerbank.Charging = pbank.RlCharge > 0 ? true : false;
                                    powerbank.ChargeLevel = (PowerBankChargeLevel)pbank.RlQoe;
                                }


                            }
                            dbDevice.SaveChanges();

                        }
                    }
                }

                    
                 
            }
            catch
            {
                Logger.LogInformation($"Error saving powerbanks from device {dev} info to database\n");
            }
        }

        private void Srv_EvConnected(object sender)
        {
            ((Server)sender).ConnectTime = DateTime.Now;
            try
            {
                using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
                {

                    var server = dbDevice.Server.FirstOrDefault(item => item.Id == ((Server)(sender)).Id);
                    if (server != null)
                    {
                        server.Connected = true;
                        dbDevice.SaveChanges();
                    }

                }
            }
            catch (Exception ex)
            {
                HandleDbException(ex);
            }

            Logger.LogInformation($"Connected to Server:{((Server)sender).Host}:{((Server)sender).Port}\n");
            ((Server)sender).SubSniffer();
        }
        private void Srv_EvDisconnected(object sender)
        {

            ((Server)sender).DisconnectTime = DateTime.Now;
            try
            {
                using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
                {
                    var server = dbDevice.Server.FirstOrDefault(item => item.Id == ((Server)(sender)).Id);
                    if (server != null)
                    {
                        server.Connected = false;
                        server.Error = "Success";
                        dbDevice.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                HandleDbException(ex);
            }
            Logger.LogInformation($"Disconnected from Server:{((Server)sender).Host}:{((Server)sender).Port}\n");
        }
        private void Srv_EvConnectError(object sender, string error)
        {
            ((Server)sender).Error = error;
            ((Server)sender).ConnectTime = DateTime.Now;
            try
            {
                using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
                {

                    var server = dbDevice.Server.FirstOrDefault(item => item.Id == ((Server)(sender)).Id);
                    if (server != null)
                    {
                        server.Error = error;
                        dbDevice.SaveChanges();
                    }

                }
            }
            catch (Exception ex)
            {
                HandleDbException(ex);
            }
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