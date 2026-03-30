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
using System.Data;

namespace WebServer.Controllers.Device
{

    public class PowerBanksController : Controller
    {
        private readonly ILogger<PowerBanksController> Logger;
        private readonly UserManager<AppUser> userManager;
        private readonly ScanDevices scanDevices;

        public PowerBanksController(UserManager<AppUser> _userManager, ScanDevices scanDevices, ILogger<PowerBanksController> logger)
        {
            userManager = _userManager;
            Logger = logger;
            this.scanDevices = scanDevices;

        }

        [HttpGet]
        [Authorize]
        public IActionResult PowerBanks()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PushPB([FromBody] PowerBankToPush powerBankToPush)
        {

            if (powerBankToPush == null || string.IsNullOrEmpty(powerBankToPush.DeviceName) || string.IsNullOrEmpty(powerBankToPush.PowerBankNum))
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

            scanDevices.PushPowerBank( powerBankToPush?.DeviceName, Convert.ToUInt32(powerBankToPush.PowerBankNum), userName, roles);
            await Task.Delay(100);

            //return RedirectToAction("ServerDetails", "ServerDetails");
            return RedirectToAction("Devices", "Devices");
        }

        //[Authorize]
        //[HttpPost]
        //public IActionResult RemovePB([FromBody] PowerBankToPush powerBankToPush)
        //{
            
        //}


        [Authorize]
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


                var userId = userManager.GetUserId(User);
                var user = await userManager.FindByIdAsync(userId);
                var roles = await userManager.GetRolesAsync(user);
                
                var allowAdminAndManager = roles.Contains("admin") || roles.Contains("manager");
                List<WebServer.Models.Device.PowerBank> powerBankList;

                if (allowAdminAndManager)
                {
                    powerBankList = scanDevices.DevicesData.PowerBanks
                        .OrderBy(p => p.HostDeviceName)
                        .ToList();
                }
                else
                {
                    var deviceNames = scanDevices.DevicesData.Devices
                        .Where(p => p.Owners == user.UserName)
                        .Select(d => d.DeviceName)
                        .ToList();
                    powerBankList = scanDevices.DevicesData.PowerBanks
                        .Where(p => deviceNames.Contains(p.HostDeviceName))
                        .OrderBy(p => p.HostDeviceName)
                        .ToList();
                }

                return Json(powerBankList, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(PowerBanksController)} -> {nameof(Refresh)} throw Exception");
                return null;
            }
        }


    }
}