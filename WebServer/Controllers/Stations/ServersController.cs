using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebServer.Models.Device;
using WebServer.Models.Identity;

namespace WebServer.Controllers.Stations
{
    public class ServersController : Controller
    {
        private readonly ILogger<ServersController> Logger;
        private readonly UserManager<AppUser> userManager;

        public ServersController(UserManager<AppUser> _userManager, ILogger<ServersController> logger)
        {
            userManager = _userManager;
            Logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "admin, manager, viewer, support")]
        public IActionResult Stations()
        {
            return View();
        }

        [Microsoft.AspNetCore.Mvc.HttpPost]
        public async Task<ActionResult> Refresh()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                var user = await userManager.FindByIdAsync(userId);
                var roles = await userManager.GetRolesAsync(user);
                var filterNotMoney = roles.Contains("support") || roles.Contains("manager");
                var allowAdminAndManager = roles.Contains("admin") || roles.Contains("manager");
                var allowAdminManagerSupport = roles.Contains("support") || roles.Contains("admin") || roles.Contains("manager");
                var allowAdmin = roles.Contains("admin");

                List<Server> serversTable = new List<Server>();

    
                 serversTable.AddRange(new List<Server>
                 {                                 
                       { new Server ( "yaup.ru", 8884, "devclient", "Potato345!", 30 ) },
                //     { new Server {HostName = "SERV2", ServerIP = "10.56.32.141", IsRunAllContainers = true, RunAllContainersProgress = 85, RunAllContainersStatus = "Start testhuj"} },
                //     { new Server {HostName = "SERV3", ServerIP = "10.56.32.116", IsStopAllContainers = true, StopAllContainersProgress = 100, StopAllContainersStatus = "Done!", DiskFreeSpace = 12, ServerErrors = "Teshuihui pizdec"} },
                 });

                serversTable = serversTable.OrderBy(c => c.Host).ToList();
                return Json(serversTable, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(ServersController)} -> {nameof(Refresh)} throw Exception");
                return null;
            }
        }


    }
}