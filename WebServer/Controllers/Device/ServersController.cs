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
    public class ServersController : Controller
    {
        private readonly ILogger<ServersController> Logger;
        private readonly UserManager<AppUser> userManager;
        private readonly ScanDevices scanDevices;

        public ServersController(UserManager<AppUser> _userManager, ScanDevices scanDevices, ILogger<ServersController> logger)
        {
            userManager = _userManager;
            Logger = logger;
            this.scanDevices = scanDevices;

        }

        [HttpGet]
        [AllowAnonymous]
        [Authorize(Roles = "admin, manager, viewer, support")]
        public IActionResult Servers()
        {
            return View();
        }

        [Microsoft.AspNetCore.Mvc.HttpPost]
        [AllowAnonymous]
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
                return Json(serversTable.Servers, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(ServersController)} -> {nameof(Refresh)} throw Exception");
                return null;
            }
        }


    }
}