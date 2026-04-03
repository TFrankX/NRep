using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebServer.Models.Device;
using WebServer.Models.Identity;
using WebServer.Models.Action;
using WebServer.Workers;
using WebServer.Data;
using Microsoft.EntityFrameworkCore;

namespace WebServer.Controllers.Device
{
    public class PowerBankToPush 
    {
       // public string ServerId { get; set; } 
        public string DeviceName { get; set; }
        public string PowerBankNum { get; set; }
    }
    public class DeviceToCanReg
    {
        public string DeviceId { get; set; }
    }
    public class DeviceToReg
    {        
        public string DeviceId { get; set; }
    }
    public class DeviceToAct
    {
        public string DeviceId { get; set; }
    }

    public class DeviceToOwn
    {
        public string DeviceId { get; set; }
        public string Owner { get; set; }
    }

    public class DeviceToTypeOfUse
    {
        public string DeviceId { get; set; }
        public string TypeOfUse { get; set; }
    }

    public class DeviceToDescription
    {
        public string DeviceId { get; set; }
        public string Description { get; set; }
    }
    public class DeleteDeviceRequest
    {
        public string DeviceId { get; set; }
    }

    public class DevicesController : Controller
    {
        private readonly ILogger<DevicesController> Logger;
        private readonly UserManager<AppUser> userManager;
        private readonly ScanDevices scanDevices;
        private readonly DeviceContext _deviceContext;
        private readonly IConfiguration _configuration;
        private readonly ActionProcess _actionProcess;

        public DevicesController(UserManager<AppUser> _userManager, ScanDevices scanDevices, ILogger<DevicesController> logger, DeviceContext deviceContext, IConfiguration configuration, ActionProcess actionProcess)
        {
            userManager = _userManager;
            Logger = logger;
            this.scanDevices = scanDevices;
            _deviceContext = deviceContext;
            _configuration = configuration;
            _actionProcess = actionProcess;
        }

        [HttpGet]
        //[AllowAnonymous]
        [Authorize]
        public IActionResult Devices()
        {
            ViewBag.ServerAddress = _configuration["Server1:address"] ?? "a-charger.com";
            return View();
        }


        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        public async Task<IActionResult> SetTypeOfUse([FromBody] DeviceToTypeOfUse DeviceToTypeOfUse)
        {

            if (DeviceToTypeOfUse == null || string.IsNullOrEmpty(DeviceToTypeOfUse.DeviceId))
            {
                RedirectToAction("Devices");
            }

            int typeOfUse = 0;

            Int32.TryParse(DeviceToTypeOfUse.TypeOfUse, out typeOfUse);

            var userId = userManager.GetUserId(User);
            var userName = userManager.GetUserName(User);
            List<string> roles = new List<string>();

            if (string.IsNullOrEmpty(userId))
            {
                userId = "unknown";

            }
            else
            {
                var user = await userManager.FindByIdAsync(userId);
                var rolesTask = await userManager.GetRolesAsync(user);
                roles = rolesTask.ToList();
            }

            // Try to update in memory first (online device)
            var result = scanDevices.SetTypeOfUse(DeviceToTypeOfUse.DeviceId, typeOfUse, userName, roles);

            // If not found in memory, update directly in DB (offline device)
            if (result == 404)
            {
                if (ulong.TryParse(DeviceToTypeOfUse.DeviceId, out ulong deviceId))
                {
                    var dbDevice = await _deviceContext.Device.FirstOrDefaultAsync(d => d.Id == deviceId);
                    if (dbDevice != null)
                    {
                        dbDevice.TypeOfUse = (TypeOfUse)typeOfUse;
                        await _deviceContext.SaveChangesAsync();
                        Logger.LogInformation($"SetTypeOfUse for offline device {deviceId} to {typeOfUse} by {userName}");
                    }
                }
            }

            await Task.Delay(100);


            return RedirectToAction("Devices", "Devices");
        }


        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        public async Task<IActionResult> SetOwner([FromBody] DeviceToOwn DeviceToOwn)
        {

            if (DeviceToOwn == null || string.IsNullOrEmpty(DeviceToOwn.DeviceId) || (DeviceToOwn.Owner==null))
            {
                RedirectToAction("Devices");
            }
            var userId = userManager.GetUserId(User);
            var userName = userManager.GetUserName(User);
            List<string> roles = new List<string>();
            if (string.IsNullOrEmpty(userId))
            {
                userId = "unknown";

            }
            else
            {
                var user = await userManager.FindByIdAsync(userId);
                var rolesTask = await userManager.GetRolesAsync(user);
                roles = rolesTask.ToList();
            }

            // Try to update in memory first (online device)
            var result = scanDevices.SetOwner(DeviceToOwn.DeviceId, DeviceToOwn.Owner, userName, roles);

            // If not found in memory, update directly in DB (offline device)
            if (result == 404)
            {
                if (ulong.TryParse(DeviceToOwn.DeviceId, out ulong deviceId))
                {
                    var dbDevice = await _deviceContext.Device.FirstOrDefaultAsync(d => d.Id == deviceId);
                    if (dbDevice != null)
                    {
                        dbDevice.Owners = DeviceToOwn.Owner;
                        await _deviceContext.SaveChangesAsync();
                        Logger.LogInformation($"SetOwner for offline device {deviceId} to '{DeviceToOwn.Owner}' by {userName}");
                    }
                }
            }

            await Task.Delay(100);


            return RedirectToAction("Devices", "Devices");
        }

        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        public async Task<IActionResult> SetDescription([FromBody] DeviceToDescription deviceToDescription)
        {
            if (deviceToDescription == null || string.IsNullOrEmpty(deviceToDescription.DeviceId))
            {
                return RedirectToAction("Devices");
            }

            var userId = userManager.GetUserId(User);
            var userName = userManager.GetUserName(User);
            List<string> roles = new List<string>();

            if (string.IsNullOrEmpty(userId))
            {
                userId = "unknown";
            }
            else
            {
                var user = await userManager.FindByIdAsync(userId);
                var rolesTask = await userManager.GetRolesAsync(user);
                roles = rolesTask.ToList();
            }

            // Try to update in memory first (online device)
            var result = scanDevices.SetDescription(deviceToDescription.DeviceId, deviceToDescription.Description ?? "", userName, roles);

            // If not found in memory, update directly in DB (offline device)
            if (result == 404)
            {
                if (ulong.TryParse(deviceToDescription.DeviceId, out ulong deviceId))
                {
                    var dbDevice = await _deviceContext.Device.FirstOrDefaultAsync(d => d.Id == deviceId);
                    if (dbDevice != null)
                    {
                        dbDevice.Description = deviceToDescription.Description ?? "";
                        await _deviceContext.SaveChangesAsync();
                        Logger.LogInformation($"SetDescription for offline device {deviceId} to '{deviceToDescription.Description}' by {userName}");
                    }
                }
            }

            await Task.Delay(100);

            return RedirectToAction("Devices", "Devices");
        }

        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        public async Task<IActionResult> Activate([FromBody] DeviceToAct DeviceToAct)
        {

            if (DeviceToAct == null || string.IsNullOrEmpty(DeviceToAct.DeviceId))
            {
                RedirectToAction("Devices");
            }
            var userId = userManager.GetUserId(User);
            var userName = userManager.GetUserName(User);
            List<string> roles = new List<string>();
            if (string.IsNullOrEmpty(userId))
            {
                userId = "unknown";

            }
            else
            {
                var user = await userManager.FindByIdAsync(userId);
                var rolesTask = await userManager.GetRolesAsync(user);
                roles = rolesTask.ToList();
            }


            scanDevices.Activate(DeviceToAct.DeviceId, userName, roles);

            await Task.Delay(100);


            return RedirectToAction("Devices", "Devices");
        }

        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        public async Task<IActionResult> CanReg([FromBody] DeviceToCanReg DeviceToCanReg)
        {

            if (DeviceToCanReg == null || string.IsNullOrEmpty(DeviceToCanReg.DeviceId))
            {
                RedirectToAction("Devices");
            }
            var userId = userManager.GetUserId(User);
            var userName = userManager.GetUserName(User);
            List<string> roles = new List<string>();
            if (string.IsNullOrEmpty(userId))
            {
                userId = "unknown";

            }
            else
            {
                var user = await userManager.FindByIdAsync(userId);
                var rolesTask = await userManager.GetRolesAsync(user);
                roles = rolesTask.ToList();
            }


            scanDevices.CanReg(DeviceToCanReg.DeviceId, userName, roles);

            await Task.Delay(100);


            return RedirectToAction("Devices", "Devices");
        }
        
        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] DeviceToReg DeviceToReg)
        {

            if (DeviceToReg == null || string.IsNullOrEmpty(DeviceToReg.DeviceId))
            {
                RedirectToAction("Devices");
            }
            var userId = userManager.GetUserId(User);
            var userName = userManager.GetUserName(User);
            List<string> roles = new List<string>();
            if (string.IsNullOrEmpty(userId))
            {
                userId = "unknown";

            }
            else
            {
                var user = await userManager.FindByIdAsync(userId);
                var rolesTask = await userManager.GetRolesAsync(user);
                roles = rolesTask.ToList();
            }


            scanDevices.Register(DeviceToReg.DeviceId, userName, roles);

            await Task.Delay(100);


            return RedirectToAction("Devices", "Devices");
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PushPB([FromBody] PowerBankToPush powerBankToPush)
        {

            if (powerBankToPush == null || string.IsNullOrEmpty(powerBankToPush.DeviceName)  || string.IsNullOrEmpty(powerBankToPush.PowerBankNum))
                RedirectToAction("Devices");

            var userId = userManager.GetUserId(User);
            var userName = userManager.GetUserName(User);
            List<string> roles=new List<string>();
            if (string.IsNullOrEmpty(userId))
            {
                userId = "unknown";
                
            }
            else
            {
                var user = await userManager.FindByIdAsync(userId);
                var rolesTask = await userManager.GetRolesAsync(user);
                roles = rolesTask.ToList();
            }
            //else
            //{

            //}




            scanDevices.PushPowerBank(powerBankToPush?.DeviceName, Convert.ToUInt32(powerBankToPush.PowerBankNum), userName,roles);
            //foreach (var server in scanDevices.DevicesData.Servers)
            //{


            //    if (server.Id.ToString() == powerBankToPush?.ServerId)
            //    {
            //        if ((Convert.ToUInt32(powerBankToPush?.PowerBankNum)) == 0)
            //        {
            //            foreach (var powerBank in scanDevices.DevicesData.PowerBanks)
            //            {
            //                //  if ((powerBank.Plugged) && (scanDevices.DevicesData.Devices[scanDevices.DevicesData.Devices.FindIndex(item => item.Id == powerBank.HostDeviceId)].DeviceName == powerBankToPush.DeviceName))
            //                if ((powerBank.Plugged) && (powerBank.HostDeviceName== powerBankToPush?.DeviceName))
            //                {
            //                    //server.CmdPushPowerBank(powerBank.HostSlot, powerBankToPush.DeviceName);

            //                    break;
            //                }
            //            }
            //        }
            //        else
            //        {
            //          //  server.CmdPushPowerBank(Convert.ToUInt32(powerBankToPush?.PowerBankNum), powerBankToPush?.DeviceName);

            //        }

            //    }
            // }
            await Task.Delay(100);

            //return RedirectToAction("ServerDetails", "ServerDetails");
            return RedirectToAction("Devices", "Devices");
        }

        [Authorize]
        public async Task<ActionResult> Refresh()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                var user = await userManager.FindByIdAsync(userId);

                var roles = await userManager.GetRolesAsync(user);
                List<WebServer.Models.Device.Device> deviceList;

                var allowAdminAndManager = roles.Contains("admin") || roles.Contains("manager");
                if (allowAdminAndManager)
                {
                    deviceList = scanDevices.DevicesData.Devices.OrderBy(d => d.DeviceName).ToList();
                }
                else
                {
                    deviceList = scanDevices.DevicesData.Devices
                        .Where(p => p.Owners == user.UserName)
                        .OrderBy(d => d.DeviceName)
                        .ToList();
                }

                // Get fresh powerbanks data
                var allPowerBanks = scanDevices.DevicesData.PowerBanks.ToList();

                // Create extended device data with slot charge levels
                var devicesWithSlots = deviceList.Select(d => new {
                    d.Id_str,
                    d.DeviceName,
                    d.HostDeviceId_str,
                    d.Online,
                    d.Slots,
                    d.Activated,
                    d.CanRegister,
                    d.Registered,
                    d.TypeOfUse,
                    d.Description,
                    d.Owners,
                    d.SimId,
                    d.LastOnlineTime,
                    // Add slot charge levels from fresh data
                    SlotInfo = GetSlotInfo(d.DeviceName, allPowerBanks, d.Online)
                }).ToList();

                return Json(devicesWithSlots, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(DevicesController)} -> {nameof(Refresh)} throw Exception");
                return null;
            }
        }

        private object[] GetSlotInfo(string deviceName, List<WebServer.Models.Device.PowerBank> powerBanks, bool isDeviceOnline)
        {
            var result = new object[4];
            for (int i = 0; i < 4; i++)
            {
                // Find powerbank for this slot (plugged OR taken from this slot)
                var pb = powerBanks?.FirstOrDefault(p => p.HostDeviceName == deviceName && p.HostSlot == i + 1 && (p.Plugged || p.Taken));
                if (pb != null)
                {
                    // Convert enum to percentage
                    int chargePercent = (int)pb.ChargeLevel switch
                    {
                        1 => 10,   // ChargeLev0_20
                        2 => 30,   // ChargeLev20_40
                        3 => 50,   // ChargeLev40_60
                        4 => 70,   // ChargeLev60_80
                        5 => 100,  // ChargeLev80_100 - show as full
                        6 => 100,  // ChargeLev100
                        _ => 0
                    };

                    // Determine status like in Statistics
                    string status;
                    bool hasRentalHistory = pb.LastGetTime > new DateTime(2000, 1, 1);

                    // If station is offline, show offline status with last known state
                    if (!isDeviceOnline)
                    {
                        if (pb.Taken)
                            status = "Offline";
                        else if (pb.Plugged && hasRentalHistory)
                            status = "Returned (Offline)";
                        else if (pb.Plugged)
                            status = "In station (Offline)";
                        else
                            status = "Offline";
                    }
                    else if (pb.Taken)
                    {
                        status = "On hands";
                    }
                    else if (pb.Plugged && hasRentalHistory)
                    {
                        status = "Returned";
                    }
                    else if (pb.Plugged)
                    {
                        status = "In station";
                    }
                    else
                    {
                        status = "Unknown";
                    }

                    // Calculate duration
                    string duration = "-";
                    if (hasRentalHistory)
                    {
                        var durationSpan = pb.Taken ? DateTime.Now - pb.LastGetTime : pb.LastPutTime - pb.LastGetTime;
                        if (durationSpan.TotalMinutes > 0)
                        {
                            duration = $"{(int)durationSpan.TotalHours}h {(int)(durationSpan.TotalMinutes % 60)}m";
                        }
                    }

                    result[i] = new {
                        Slot = i + 1,
                        HasPowerBank = true,
                        PowerBankId = pb.Name,
                        PowerBankIdNum = pb.Id_str,  // Numeric ID for delete operation
                        ChargeLevel = chargePercent,
                        Charging = pb.Charging,
                        Plugged = pb.Plugged,
                        Locked = pb.Locked,
                        IsOk = pb.IsOk,
                        Taken = pb.Taken,
                        UserId = pb.UserId ?? "",
                        LastGetTime = pb.LastGetTime,
                        LastPutTime = pb.LastPutTime,
                        Duration = duration,
                        Status = status
                    };
                }
                else
                {
                    result[i] = new {
                        Slot = i + 1,
                        HasPowerBank = false,
                        PowerBankId = "",
                        ChargeLevel = 0,
                        Charging = false,
                        Plugged = false,
                        Locked = false,
                        IsOk = true,
                        Taken = false,
                        UserId = "",
                        LastGetTime = (DateTime?)null,
                        LastPutTime = (DateTime?)null,
                        Duration = "-",
                        Status = "Empty"
                    };
                }
            }
            return result;
        }

        [HttpPost]
        [Authorize(Roles = "admin,manager")]
        public async Task<ActionResult> DeleteDevice([FromBody] DeleteDeviceRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.DeviceId) || !ulong.TryParse(request.DeviceId, out ulong deviceId))
                {
                    return Json(new { success = false, message = "Invalid Device ID" });
                }

                // Get current user info
                var currentUser = await userManager.GetUserAsync(User);
                var userName = currentUser?.UserName ?? "unknown";

                // Find device in memory
                var memDevice = scanDevices.DevicesData.Devices.FirstOrDefault(d => d.Id == deviceId);
                string deviceName = "";

                if (memDevice != null)
                {
                    // Only allow deletion of offline devices
                    if (memDevice.Online)
                    {
                        return Json(new { success = false, message = "Cannot delete an online station. Only offline stations can be deleted." });
                    }
                    deviceName = memDevice.DeviceName;
                }

                // Delete from DB
                var dbDevice = await _deviceContext.Device.FirstOrDefaultAsync(d => d.Id == deviceId);
                if (dbDevice != null)
                {
                    if (string.IsNullOrEmpty(deviceName))
                    {
                        deviceName = dbDevice.DeviceName;
                    }
                    _deviceContext.Device.Remove(dbDevice);
                    await _deviceContext.SaveChangesAsync();
                    Logger.LogInformation($"Deleted Device {deviceId} ({deviceName}) from database");
                }

                // Delete from memory
                if (memDevice != null)
                {
                    scanDevices.DevicesData.Devices.Remove(memDevice);
                    Logger.LogInformation($"Removed Device {deviceId} ({deviceName}) from memory");
                }

                // Log to Actions
                _actionProcess.ActionSave(
                    (int)ActionsDescription.StationRemove,
                    userName,
                    0,  // serverId
                    deviceId,
                    0,  // powerBankId
                    0,  // slot
                    $"Station {deviceName} (ID: {deviceId}) - deleted from system by user {userName}"
                );

                return Json(new { success = true, message = "Station deleted successfully" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(DevicesController)} -> {nameof(DeleteDevice)} throw Exception");
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}