using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProtoBuf.Meta;
using SimnetLib;
using System;
using System.Data;
using WebServer.Models.Device;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebServer.Workers
{
    public class ScanDevices : BackgroundService
    {
        public List<Server> servers;
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
            Models.Device.Device device = new Models.Device.Device(123, 156, false, 3, DateTime.Now, DateTime.Now, "10.0.0.1", "yaup.ru", "hhh.hh");

            try
            {
                using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
                {
                    if (dbDevice.Server.Any(o => o.Id != server.Id))
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

            try
            {

                using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
                {
                    if (dbDevice.Device.Any(o => o.Id != device.Id))
                    {
                        dbDevice.Device.Add(device);
                        dbDevice.SaveChanges();
                    }
                }

            }
            catch (Exception ex)
            {
                //dbDevice.Entry(device).State = EntityState.Detached;
                HandleDbException(ex);
            }

            servers = new List<Server>();
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
                if ((srv!=null))
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
        private void Srv_EvSniffer(object sender,string topic)
        {
            try
            {

                string dev = topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).Substring(0, topic.Substring(topic.IndexOf("cabinet") + 8, topic.Length - 8).IndexOf('/'));
                ((Server)(sender)).EvQueryTheInventory += Srv_EvQueryTheInventory;
                ((Server)(sender)).CmdQueryTheInventory(dev);
                Logger.LogInformation($"New device - {dev} , try to add \n");
            }
            catch
            {
                Logger.LogInformation($"Get invalid device with topic: {topic}; Waiting somthing like - cabinet/<name of device>/...\n");
            }
            
          //  ((Server)(sender)).CmdQueryTheInventory(topic.IndexOf("Dev"));
        }

        private void Srv_EvQueryTheInventory(object sender, RplQueryTheInventory data)
        {
            
        }

        private void Srv_EvConnected(object sender)
        {
            ((Server)sender).ConnectTime = DateTime.Now;
            try
            {
                using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
                {
                    dbDevice.SaveChanges();
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
                    dbDevice.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                HandleDbException(ex);
            }
            Logger.LogInformation($"Disconnected from Server:{((Server)sender).Host}:{((Server)sender).Port}\n");
        }
        private void Srv_EvConnectError(object sender,string error)
        {
            ((Server)sender).Error= error;
            ((Server)sender).ConnectTime = DateTime.Now;
            try
            {
                using (var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>())
                {
                    dbDevice.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                HandleDbException(ex);
            }
            Logger.LogInformation($"Error in connect to {((Server)sender).Host}:{((Server)sender).Port} : {error}\n");
        }
    }

}
