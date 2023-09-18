using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
            var scope = ScopeFactory.CreateScope();
            var dbDevice = scope.ServiceProvider.GetRequiredService<DeviceContext>();



            //var Servers = scope.ServiceProvider.GetRequiredService<Server>();            
            //            PowerBank[] powerBanks = new PowerBank[4];
            //            powerBanks[0] = new PowerBank { Id = 1, ChargeLevel = 0, Charging = true, ClientTime = DateTimeOffset.Now, LastGetTime = DateTime.Now, LastPutTime = DateTime.Now, Locked = true, Plugged = true, Price = 100 };
            //            powerBanks[1] = new PowerBank { Id = 2, ChargeLevel = 0, Charging = true, ClientTime = DateTimeOffset.Now, LastGetTime = DateTime.Now, LastPutTime = DateTime.Now, Locked = true, Plugged = true, Price = 100 };
            //            powerBanks[2] = new PowerBank { Id = 3 };
            //            powerBanks[3] = new PowerBank { Id = 4 };
            Server server = new Server("yaup.ru", 8884, "devclient", "Potato345!", 30);
            Device device = new Device(123, 156, false, 3, DateTime.Now, DateTime.Now, "10.0.0.1", "yaup.ru", "hhh.hh");

            try
            {
                dbDevice.Server.Add(server);
                dbDevice.SaveChanges();
            }
            catch (Exception ex)
            {
                HandleDbException(ex);
            }

            try
            {
                dbDevice.Device.Add(device);
                dbDevice.SaveChanges();
            }
            catch (Exception ex)
            {
                HandleDbException(ex);
            }

            servers = new List<Server>();
            ulong tmpId;
            foreach (var srv in dbDevice.Server)
            {
                if (!srv.Init)
                {
                    // servers.Add(new(srv.Host, srv.Port, srv.Login,srv.Password,srv.ReconnectTime)); ;
                    servers.Add(srv);
                    //srv.Init = true;
                    srv.EvConnected += Srv_EvConnected;
                    srv.EvDisconnected += Srv_EvDisconnected;
                    srv.Connect();


                }
                foreach (var dev in dbDevice.Device)
                {
                    tmpId = dev.Id;
                }

                while (!stoppingToken.IsCancellationRequested)
                {

                    //if (servers == null)
                    //{
                    //    servers = new List<Server>();
                    //    servers.Add(new Server());
                    //}


                    using (scope)
                    {
                        //   var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();
                        //    foreach (var pendingTask in db.Tasks.Where(t => !t.IsCompleted && t.DueDate < DateTime.Now))
                        //    {
                        //        pendingTask.ActionDate = DateTime.Now;
                        //        pendingTask.IsCompleted = true;
                        //    }
                        //    db.SaveChanges();
                    }
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            }




        }

        private void Srv_EvConnected(object sender)
        {
            Logger.LogInformation($"Connected to Server:{((Server)sender).Host}:{((Server)sender).Port}\n");
        }
        private void Srv_EvDisconnected(object sender)
        {
            Logger.LogInformation($"Disconnected from Server:{((Server)sender).Host}:{((Server)sender).Port}\n");
        }
    }

}
