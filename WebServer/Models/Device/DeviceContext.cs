using Microsoft.EntityFrameworkCore;

namespace WebServer.Models.Device
{
    public class DeviceContext : DbContext
    {
        public DbSet<Device> Device { get; set; }
        public DbSet<Server> Server { get; set; }
        public DbSet<PowerBank> PowerBanks { get; set; }
        public DeviceContext(DbContextOptions<DeviceContext> options)
        : base(options)
            {
                Database.EnsureCreated();
            }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Device>().HasIndex(u => u.Id);
            modelBuilder.Entity<Server>().HasIndex(u => u.Id);
            modelBuilder.Entity<PowerBank>().HasIndex(u => u.Id);
            //modelBuilder.Entity<StressProfile>().HasIndex(u => new { u.NameSettings, u.NumberOfUsers, u.NumberOfInstrument, u.NumberOfOrdersFromUserPerSec }).IsUnique();
        }
    }
}
