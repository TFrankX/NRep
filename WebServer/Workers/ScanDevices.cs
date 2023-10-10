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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Formats.Asn1.AsnWriter;
using static System.Reflection.Metadata.BlobBuilder;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Device = WebServer.Models.Device.Device;

namespace WebServer.Workers
{
    public class ScanDevices : BackgroundService
    {
        public IDevicesData DevicesData { get; set; }
        //public List<Server> servers;
        //public List<WebServer.Models.Device.Device> devices;
        //public List<PowerBank> powerbanks;
        //public readonly IServiceScopeFactory ScopeFactory;
        private readonly ILogger<ScanDevices> Logger;
 
        IServiceScope scope;
        // DeviceContext dbDevice;

        //  public ScanDevices(List<Server> Servers, ILogger<ScanDevices> logger, IServiceScopeFactory scopeFactory)
        public ScanDevices(ILogger<ScanDevices> logger, IDevicesData devicesData, IServiceScopeFactory scopeFactory)
        {
            //ScopeFactory = scopeFactory;
            DevicesData = devicesData;
            Logger = logger;
            scope = scopeFactory.CreateScope();
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

            Server server = new Server("yaup.ru", 8884, "devclient", "Potato345!", 30);
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


                foreach (var powbank in DevicesData.PowerBanks)
                {
                    if (!powbank.Stored)
                    {
                        AddOrUpdate(dbDevice.PowerBank, dbDevice, c => c.Id, powbank);
                        powbank.Stored = true;
                    }
                }

                foreach (var dev in DevicesData.Devices)
                {
                 
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
                foreach (var srv in dbDevice.Server)
                {
                    srv.Connected = false;
                    srv.Stored = false;
                    DevicesData.Servers.Add(srv);
                }
                ReconnectServers();
                foreach (var powerbank in dbDevice.PowerBank)
                {
                    {
                        DevicesData.PowerBanks.Add(powerbank);
                        powerbank.Plugged = false;
                    }
                }
                foreach (var dev in dbDevice.Device)
                {
                        dev.Online = false;
                        DevicesData.Devices.Add(dev);
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].EvQueryTheInventory -= Srv_EvQueryTheInventory;
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].EvQueryTheInventory += Srv_EvQueryTheInventory;
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].EvReturnThePowerBank -= Srv_EvReturnThePowerBank;
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].EvReturnThePowerBank += Srv_EvReturnThePowerBank;
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].SubScript(dev.DeviceName);
                        DevicesData.Servers[DevicesData.Servers.FindIndex(item => item.Id == dev.HostDeviceId)].CmdQueryTheInventory(dev.DeviceName);
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
                        srv.EvConnected += Srv_EvConnected;
                        srv.EvConnectError += Srv_EvConnectError;
                        srv.EvDisconnected += Srv_EvDisconnected;
                        srv.EvSubSniffer += Srv_EvSniffer;
                        srv.Connect();
                        srv.RecentlyConnect = true;
                    }
                    else
                    {
                        if (srv.RecentlyConnect)
                        {

                            srv.EvQueryTheInventory -= Srv_EvQueryTheInventory;
                            srv.EvQueryTheInventory += Srv_EvQueryTheInventory;
                            srv.EvReturnThePowerBank -= Srv_EvReturnThePowerBank;
                            srv.EvReturnThePowerBank += Srv_EvReturnThePowerBank;

                            foreach(var dev in DevicesData.Devices)
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
                            foreach (var dev in DevicesData.Devices)
                            {
                                if ((dev.HostDeviceId == srv.Id) )
                                {
                                    if ((DateTime.Now - dev.LastOnlineTime).TotalSeconds > srv.OnlineTimeOut)
                                    {
                                        dev.Online = false;
                                        dev.Slots = 0;
                                        foreach (var powerbank in DevicesData.PowerBanks)
                                        {
                                            if (powerbank.HostDeviceName == dev.DeviceName)
                                            {
                                                powerbank.Plugged = false;
                                            }
                                        }
                                    }

                                    srv.CmdQueryTheInventory(dev.DeviceName);
                                }

                            }

                        }



                    }

                }

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

            if ((command == SimnetLib.Model.MessageTypes.ReportCabinetLogin) && (dev != ""))
            {
                if (DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev)) < 0)
                {
                    Device device = new Device(dev, ((Server)(sender)).Id, true);
                    device.LastOnlineTime = DateTime.Now;
                    DevicesData.Devices.Add(device);
                    Logger.LogInformation($"New device login - {dev} , try to add \n");
                }
                else
                {
                    DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].LastOnlineTime = DateTime.Now;
                    DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].Online = true;
                    DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.Id == GetGUID(dev))].Stored = false;
                    Logger.LogInformation($"Exist device login - {dev} \n");
                }
                ((Server)(sender)).EvQueryTheInventory -= Srv_EvQueryTheInventory;
                ((Server)(sender)).EvQueryTheInventory += Srv_EvQueryTheInventory;

                ((Server)(sender)).EvReturnThePowerBank -= Srv_EvReturnThePowerBank;
                ((Server)(sender)).EvReturnThePowerBank += Srv_EvReturnThePowerBank;


                ((Server)(sender)).SubScript(dev);
                ((Server)(sender)).CmdQueryTheInventory(dev);

   
            }
        }

        private void Srv_EvReturnThePowerBank(object sender, string topic, RptReturnThePowerBank data)
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
            Logger.LogInformation($"Powerbank {data.RlPbid} returned in device: {dev} , slot: {data.RlSlot}\n");

            device.Slots = device.Slots | ((uint)Math.Pow(2, data.RlSlot - 1));// & ~((uint)(Math.Pow(2, data.RlSlot - 1)));
            device.Online = true;
            device.LastOnlineTime = DateTime.Now;
            if (DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid) < 0)
            {
                DevicesData.PowerBanks.Add(new PowerBank(data.RlPbid, device.DeviceName, data.RlSlot, data.RlLock > 0 ? true : false, true,false, (PowerBankChargeLevel)data.RlQoe));
            }
            else
            {
                try
                {
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].HostDeviceName = device.DeviceName;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].HostSlot = data.RlSlot;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Locked = data.RlLock > 0 ? true : false;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Plugged = true;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Charging = data.RlLimited > 0 ? true : false;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].ChargeLevel = (PowerBankChargeLevel)data.RlQoe;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].IsOk = data.RlCode == 0 ? true : false;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].Stored = false;
                    DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == data.RlPbid)].LastPutTime = DateTime.Now;
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

            uint slots = 0;
            foreach (var pbank in data.RlBank1s)
            {

                if (pbank.RlIdok == 1)
                {

                    slots = slots | ((uint)Math.Pow(2,pbank.RlSlot - 1));
                    if (DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid) < 0)
                    {
                        DevicesData.PowerBanks.Add(new PowerBank(pbank.RlPbid, device.DeviceName, pbank.RlSlot, pbank.RlLock > 0 ? true : false, true, pbank.RlCharge > 0 ? true : false, (PowerBankChargeLevel)pbank.RlQoe));

                    }
                    else
                    {
                        try
                        {
                            DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].HostDeviceName = device.DeviceName;
                            DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].HostSlot = pbank.RlSlot;
                            DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].Locked = pbank.RlLock > 0 ? true : false;
                            DevicesData.PowerBanks[DevicesData.PowerBanks.FindIndex(item => item.Id == pbank.RlPbid)].Plugged = true;
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
            ((Server)sender).SubSniffer();
            ((Server)sender).Stored = false;
        }


        public void PushPowerBank(string deviceName, uint numberPB)
        {

            foreach (var server in DevicesData.Servers)
            {



                if (numberPB == 0)
                {
                    foreach (var powerBank in DevicesData.PowerBanks)
                    {
                        //  if ((powerBank.Plugged) && (scanDevices.DevicesData.Devices[scanDevices.DevicesData.Devices.FindIndex(item => item.Id == powerBank.HostDeviceId)].DeviceName == powerBankToPush.DeviceName))
                        if ((powerBank.Plugged) && (powerBank.HostDeviceName == deviceName))
                        {
                            server.CmdPushPowerBank(powerBank.HostSlot, powerBank.HostDeviceName);
                            powerBank.Plugged = false;
                            try
                            {
                                Device device = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.DeviceName == deviceName)];
                                device.Slots = device.Slots  & ~(uint)Math.Pow(2, powerBank.HostSlot - 1);

                            }
                            catch
                            {
                                Logger.LogInformation($"Get invalid device with topic: {deviceName}; Waiting somthing like - cabinet/<name of device>/...\n");
                                return;
                            }

                                
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var powerBank in DevicesData.PowerBanks)
                    {
                        if ((powerBank.Plugged) && (powerBank.HostDeviceName == deviceName) &&(powerBank.HostSlot == numberPB)) 
                        {
                            server.CmdPushPowerBank(numberPB, deviceName);
                            powerBank.Plugged = false;
                            try
                            {
                                Device device = DevicesData.Devices[DevicesData.Devices.FindIndex(item => item.DeviceName == deviceName)];
                                device.Slots = device.Slots & ~(uint)Math.Pow(2, powerBank.HostSlot - 1);

                            }
                            catch
                            {
                                Logger.LogInformation($"Get invalid device with topic: {deviceName}; Waiting somthing like - cabinet/<name of device>/...\n");
                                return;
                            }
                        }
                            
                    }
                }

                
            }


        }

        private void Srv_EvDisconnected(object sender)
        {

            ((Server)sender).DisconnectTime = DateTime.Now;

            ((Server)sender).ConnectTime = DateTime.Now;
            ((Server)sender).Connected = false;
             Logger.LogInformation($"Disconnected from Server:{((Server)sender).Host}:{((Server)sender).Port}\n");
            ((Server)sender).Stored = false;
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