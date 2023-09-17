using WebServer.Models.Device;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebServer.Workers
{
    public class ScanDevices : BackgroundService
    {
        public List<Server> servers;
        public readonly IServiceScopeFactory ScopeFactory;
        public ScanDevices(List<Server> Servers, IServiceScopeFactory scopeFactory)
        {
            ScopeFactory = scopeFactory;
            servers = Servers;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            //var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();
            var scope = ScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();
            PowerBank[] powerBanks = new PowerBank[4];
            powerBanks[0] = new PowerBank { Id = 1, ChargeLevel = 0, Charging = true, ClientTime = DateTimeOffset.Now, LastGetTime = DateTime.Now, LastPutTime = DateTime.Now, Locked = true, Plugged = true, Price = 100 };
            powerBanks[1] = new PowerBank { Id = 2, ChargeLevel = 0, Charging = true, ClientTime = DateTimeOffset.Now, LastGetTime = DateTime.Now, LastPutTime = DateTime.Now, Locked = true, Plugged = true, Price = 100 };
            powerBanks[2] = new PowerBank { Id = 3 };
            powerBanks[3] = new PowerBank { Id = 4 };

            Device device = new Device(123,false,3,DateTime.Now,DateTime.Now,"10.0.0.1","yaup.ru","hhh.hh",powerBanks);
            db.Device.Add(device);
            db.SaveChanges();

            ulong tmpId;
            foreach (var dev in db.Device)
            {
                tmpId=dev.Id;
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
}
