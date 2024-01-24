using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebServer.Models.Device;
using WebServer.Models.Identity;
using WebServer.Workers;
using WebServer.Data;
namespace WebServer.Controllers.Device
{
    public class PowerBankToPush 
    {
       // public string ServerId { get; set; } 
        public string DeviceName { get; set; }
        public string PowerBankNum { get; set; }
    }
    public class DevicesController : Controller
    {
        private readonly ILogger<DevicesController> Logger;
        private readonly UserManager<AppUser> userManager;
        private readonly ScanDevices scanDevices;

        public DevicesController(UserManager<AppUser> _userManager, ScanDevices scanDevices, ILogger<DevicesController> logger)
        {
            userManager = _userManager;
            Logger = logger;
            this.scanDevices = scanDevices;

        }

        [HttpGet]
        [AllowAnonymous]
        [Authorize(Roles = "admin, manager, viewer, support")]
        public IActionResult Devices()
        {
            return View();
        }

        [Microsoft.AspNetCore.Mvc.HttpPost]
        [AllowAnonymous]
        [HttpPost]
        public IActionResult PushPB([FromBody] PowerBankToPush powerBankToPush)
        {

            if (powerBankToPush == null || string.IsNullOrEmpty(powerBankToPush.DeviceName)  || string.IsNullOrEmpty(powerBankToPush.PowerBankNum))
                RedirectToAction("Devices");

            //var userId = userManager.GetUserId(User);
            //if (string.IsNullOrEmpty(userId))
            //    return Unauthorized();

            //userSettingsCache.AddFilterContainersParam(userId, serverDetails.ServerIP);

            scanDevices.PushPowerBank(powerBankToPush?.DeviceName, Convert.ToUInt32(powerBankToPush.PowerBankNum),"");
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
            Thread.Sleep(100);

            //return RedirectToAction("ServerDetails", "ServerDetails");
            return RedirectToAction("Devices", "Devices");
        }


        public async Task<ActionResult> Refresh()
        {
            try
            {
                //var userId = userManager.GetUserId(User);
                //var user = await userManager.FindByIdAsync(userId);
                //var roles = await userManager.GetRolesAsync(user);
                //var filterNotMoney = roles.Contains("support") || roles.Contains("manager");
                //var allowAdminAndManager = roles.Contains("admin") || roles.Contains("manager");
                //var allowAdminManagerSupport = roles.Contains("support") || roles.Contains("admin") || roles.Contains("manager");
                //var allowAdmin = roles.Contains("admin");

                DevicesData serversTable = new DevicesData(scanDevices.DevicesData.Servers, scanDevices.DevicesData.Devices, scanDevices.DevicesData.PowerBanks);

                // serversTable.AddRange(new List<Server>
                // {                                 
                //       { new Server ( "yaup.ru", 8884, "devclient", "Potato345!", 30 ) },
                // });

                //serversTable.Servers = serversTable.Servers.OrderBy(c => c.Host).ToList();
                serversTable.Sort();
                return Json(serversTable.Devices, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(DevicesController)} -> {nameof(Refresh)} throw Exception");
                return null;
            }
        }


    }
}