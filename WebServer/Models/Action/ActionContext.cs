using System.ComponentModel;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace WebServer.Models.Action
{
    public class ActionContext : DbContext
    {
        public string TableName { get; set; }
        public DbSet<Action> Actions { get; set; }
        //public DevActionContext(DbContextOptions<DevActionContext> options, IDevActionTable actionTable)
        public ActionContext(DbContextOptions<ActionContext> options)
        : base(options)
            {
                //TableName = "DevActions";
                Database.EnsureCreated();
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Action>().ToTable(TableName).HasIndex(u => u.ActionTime);
            modelBuilder.Entity<Action>().HasIndex(u => u.ActionTime);
            //modelBuilder.Entity<StressProfile>().HasIndex(u => new { u.NameSettings, u.NumberOfUsers, u.NumberOfInstrument, u.NumberOfOrdersFromUserPerSec }).IsUnique();
        }

        public void FillText(Action actLine)
        {
            
            string text="";
            //foreach (var actLine in Actions)
            {

                // System
                if ((actLine.ActionCode & 0x1000) == 0x1000)
                {
                    text = $"{GetEnumDescription((ActionsDescription)(actLine.ActionCode))}";
                }
                // System
                if ((actLine.ActionCode & 0x1500) == 0x1500)
                {
                    text = $"{GetEnumDescription((ActionsDescription)(actLine.ActionCode))}";
                }

                // Server
                if ((actLine.ActionCode & 0x0100)==0x0100)
                {                
                    text = $"Server {actLine.ActionServerId} - {GetEnumDescription((ActionsDescription)(actLine.ActionCode))}";
                }
                // Station
                if ((actLine.ActionCode & 0x0200) == 0x0200)
                {
                    text = $"Station {actLine.ActionStationId} - {GetEnumDescription((ActionsDescription)(actLine.ActionCode))}";
                }
                // Powerbank
                if ((actLine.ActionCode & 0x0300) == 0x0300)
                {
                    text = $"Powerbank {actLine.ActionPowerBankId} in device {actLine.ActionStationId} slot {actLine.ActionPowerBankSlot} - {GetEnumDescription((ActionsDescription)(actLine.ActionCode))} user {actLine.UserId}";
                }

                // User
                if ((actLine.ActionCode & 0x2000) == 0x2000)
                {
                    text = $"User - {actLine.UserId}: {GetEnumDescription((ActionsDescription)(actLine.ActionCode))}";
                }
                actLine.ActionText = text;
            }
        }



        private string GetEnumDescription(Enum value)
        {
            // Get the Description attribute value for the enum value
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }


        //public static string GetActionDescription(this int value)
        //{
        //    FieldInfo field = value.GetType().GetField(value.ToString());

        //    DescriptionAttribute attribute
        //            = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))
        //                as DescriptionAttribute;

        //    return attribute == null ? value.ToString() : attribute.Description;
        //}
        //public override int SaveChanges()
        //{
        //   ChangeTracker.DetectChanges();
        //  foreach (var enty in ChangeTracker.Entries())
        //    {
        //        if (enty.State == EntityState.Added
        //            || enty.State == EntityState.Modified)
        //        {
        //            enty.Property("LastUpdate").CurrentValue = DateTime.UtcNow;
        //        }
        //    }
        //     return base.SaveChanges();
        //}



    }
}
