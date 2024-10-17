using Microsoft.EntityFrameworkCore;
using WebServer.Models.Settings;

namespace WebServer.Models.Settings
{
    public class SettingsContext : DbContext
    {
        public DbSet<Set> Settings { get; set; }

       // public DbSet<ServerStatistic> ServerStatistics { get; set; }
        public SettingsContext(DbContextOptions<SettingsContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Set>().HasIndex(u => u.Name);
           // modelBuilder.Entity<StressProfile>().HasIndex(u => new { u.NameSettings, u.NumberOfUsers, u.NumberOfInstrument, u.NumberOfOrdersFromUserPerSec }).IsUnique();
        }
    }
}

