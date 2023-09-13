using WebServer.Models.Device;

namespace WebServer.Workers
{
    public class ScanDevices:BackgroundService
    {
        public readonly IServiceScopeFactory ScopeFactory;
        public ScanDevices(IServiceScopeFactory scopeFactory)
        {
            ScopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = ScopeFactory.CreateScope())
                {
                   var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();
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
