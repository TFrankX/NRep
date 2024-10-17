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
using System;

namespace WebServer.Controllers.Device
{
  
    public class ActionsController : Controller
    {
        private readonly ILogger<DevicesController> Logger;
        private readonly UserManager<AppUser> userManager;
        private readonly ScanDevices scanDevices;
        private WebServer.Models.Action.ActionContext db;

        public ActionsController(UserManager<AppUser> _userManager, ScanDevices scanDevices,ILogger<DevicesController> logger, WebServer.Models.Action.ActionContext context)
        {
            userManager = _userManager;
            Logger = logger;
            db = context;
            this.scanDevices = scanDevices;
        }

        [HttpGet]
        //[AllowAnonymous]
        [Authorize]
        public IActionResult ActionsUser()
        {
            return View();
        }


        [HttpGet]
        [Authorize(Roles = "admin,manager")]
        public IActionResult ActionsAdmin()
        {
            return View();
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

                // DevicesData serversTable = new DevicesData(scanDevices.DevicesData.Servers, scanDevices.DevicesData.Devices, scanDevices.DevicesData.PowerBanks);

                // serversTable.AddRange(new List<Server>
                // {                                 
                //       { new Server ( "yaup.ru", 8884, "devclient", "Potato345!", 30 ) },
                // });

                //serversTable.Servers = serversTable.Servers.OrderBy(c => c.Host).ToList();
                //   actionsTable.Sort();
                //!       var actionsTable = new List<Action>();





                //try
                //{
                //    var actionsTable = (db.Action.Where(x => x.ActionStationId == ServerId.Server)
                //                                .Where(x => DateTime.Compare(x.UpdateTime, DateTimeBegin) >= 0 && DateTime.Compare(x.UpdateTime, DateTimeEnd) <= 0)
                //                               .Select(x => new { x.UpdateTime, x.DiskFreeSpace, x.FreePhysicalMemory })
                //                               .ToList())


                //    if (DebugMode)
                //        Logger.LogInformation($"ServerController -> GetServersData -> Output: {JsonConvert.SerializeObject(ServersData)}\n");

                //    return Json(ServersData, new JsonSerializerOptions { PropertyNamingPolicy = null });
                //}
                //catch (Exception ex)
                //{
                //    Logger.LogError($"ServerController -> GetServersData throw exception: {ex.Message} {ex.InnerException?.Message}");

                //    throw ex;
                //}



                // var actionsTable = new List<WebServer.Models.Action.Action>();
                //var actionTable = db;

                try
                {

                    //var actionsTable = (db.Action
                    //                            .Where(x => DateTime.Compare(x.ActionTime, DateTime.Now.Subtract(new TimeSpan(30, 0, 0, 0))) >= 0 && DateTime.Compare(x.ActionTime, DateTime.Now) <= 0)
                    //                           .ToList());


                    var userId = userManager.GetUserId(User);
                    var user = await userManager.FindByIdAsync(userId);
                    var roles = await userManager.GetRolesAsync(user);
                    var filterNotMoney = roles.Contains("support") || roles.Contains("manager");
                    var allowAdminAndManager = roles.Contains("admin") || roles.Contains("manager");
                    var allowAdminManagerSupport = roles.Contains("support") || roles.Contains("admin") || roles.Contains("manager");
                    var allowAdmin = roles.Contains("admin");


                    if (allowAdminAndManager)
                    {

                        var actionsTable = db.Actions.ToList();
                        foreach (var actLine in actionsTable)
                        {
                            db.FillText(actLine);
                        }
                        //return Vaiew("ActionsAdmin", model);
                        return Json(actionsTable, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        var deviceList = scanDevices.DevicesData.Devices.Where(p => p.Owners == user.UserName).ToList<WebServer.Models.Device.Device>();
                        var actionsTable = db.Actions.Where(a => deviceList.Select(b => b.Owners).Contains(a.UserId));
                        foreach (var actLine in actionsTable)
                        {
                            db.FillText(actLine);
                        }
                        var actionsUserTable = actionsTable.Select(a => new { a.ActionTime, a.ActionStationId, a.ActionPowerBankId, a.ActionText }).ToList();

                        return Json(actionsTable, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }

                }
                catch (Exception ex)
                {
                    
                    Logger.LogError(ex, $"{nameof(DevicesController)} -> {nameof(Refresh)} throw Exception");
                    return null;
                }

                
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(DevicesController)} -> {nameof(Refresh)} throw Exception");
                return null;
            }
        }


    }
}