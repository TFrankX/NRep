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
namespace WebServer.Controllers.User
{

    public class UserController : Controller
    {
        private readonly ILogger<UserController> Logger;
        private readonly UserManager<AppUser> userManager;
        private readonly ScanDevices scanDevices;

        public UserController(UserManager<AppUser> _userManager, ScanDevices scanDevices, ILogger<UserController> logger)
        {
            userManager = _userManager;
            Logger = logger;
            this.scanDevices = scanDevices;

        }

        [HttpGet]
        [AllowAnonymous]
        [Authorize(Roles = "admin, manager, viewer, support")]
        public IActionResult User()
        {
            return View();
        }

        public async Task<ActionResult> Refresh()
        {
            try
            {


                DevicesData serversTable = new DevicesData(scanDevices.DevicesData.Servers, scanDevices.DevicesData.Devices, scanDevices.DevicesData.PowerBanks);

                serversTable.Sort();
                return Json(serversTable.Devices, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(UserController)} -> {nameof(Refresh)} throw Exception");
                return null;
            }
        }


    }
}