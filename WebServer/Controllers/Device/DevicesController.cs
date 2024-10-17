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
        //[AllowAnonymous]
        [Authorize]
        public IActionResult Devices()
        {
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


            scanDevices.SetTypeOfUse(DeviceToTypeOfUse.DeviceId, typeOfUse, userName, roles);

            Thread.Sleep(100);


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


            scanDevices.SetOwner(DeviceToOwn.DeviceId, DeviceToOwn.Owner, userName, roles);

            Thread.Sleep(100);


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

            Thread.Sleep(100);


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

            Thread.Sleep(100);


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

            Thread.Sleep(100);


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
            Thread.Sleep(100);

            //return RedirectToAction("ServerDetails", "ServerDetails");
            return RedirectToAction("Devices", "Devices");
        }

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
                DevicesData serversTable;

                var allowAdminAndManager = roles.Contains("admin") || roles.Contains("manager");
                if (allowAdminAndManager)
                {
                     serversTable = new DevicesData(scanDevices.DevicesData.Servers, scanDevices.DevicesData.Devices, scanDevices.DevicesData.PowerBanks);
                }
                else
                {
                    var deviceList = scanDevices.DevicesData.Devices.Where(p => p.Owners == user.UserName).ToList<WebServer.Models.Device.Device>();
                    var powerBankList = scanDevices.DevicesData.PowerBanks.Where(p => deviceList.Select(b => b.DeviceName).Contains(p.HostDeviceName)).ToList<WebServer.Models.Device.PowerBank>();
                    //foreach (var device in deviceList)
                    //{
                    //    foreach
                    //}
                     serversTable = new DevicesData(null, deviceList, powerBankList);
                }
                

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