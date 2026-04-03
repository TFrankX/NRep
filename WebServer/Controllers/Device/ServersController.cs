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
        [Authorize]
        public IActionResult Servers()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> Refresh()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                var user = await userManager.FindByIdAsync(userId);
                var roles = await userManager.GetRolesAsync(user);

                var allowAdminAndManager = roles.Contains("admin") || roles.Contains("manager");

                if (!allowAdminAndManager)
                {
                    return Json(new List<object>(), new JsonSerializerOptions { PropertyNamingPolicy = null });
                }

                var serversList = scanDevices.DevicesData.Servers.OrderBy(s => s.Host).ToList();
                var devices = scanDevices.DevicesData.Devices.ToList();

                // Build response with calculated device counts
                var result = serversList.Select(srv => new
                {
                    srv.Host,
                    srv.Port,
                    srv.Error,
                    srv.Connected,
                    // Count all devices belonging to this server
                    DevicesCount = devices.Count(d => d.HostDeviceId == srv.Id),
                    // Count online devices belonging to this server
                    OnlineDevicesCount = devices.Count(d => d.HostDeviceId == srv.Id && d.Online),
                    // Count not authorized devices (for backward compatibility)
                    NotAuthDevicesCount = devices.Count(d => d.HostDeviceId == srv.Id && !d.Online),
                    srv.ReconnectTime,
                    srv.ConnectTime,
                    srv.DisconnectTime
                }).ToList();

                return Json(result, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(ServersController)} -> {nameof(Refresh)} throw Exception");
                return null;
            }
        }


    }
}