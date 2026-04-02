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

        [HttpGet]
        [Authorize]
        public IActionResult StationActions(string stationId, string stationName)
        {
            ViewBag.StationId = stationId;
            ViewBag.StationName = stationName;
            return View();
        }




        [Authorize]
        public async Task<ActionResult> Refresh(DateTime? fromDate = null, DateTime? toDate = null, string period = "day")
        {
            try
            {
                // Определяем диапазон дат
                DateTime dateFrom, dateTo;
                dateTo = DateTime.Now;

                if (fromDate.HasValue && toDate.HasValue)
                {
                    // Пользовательский диапазон
                    dateFrom = fromDate.Value;
                    dateTo = toDate.Value;
                }
                else
                {
                    // Предустановленные периоды
                    switch (period?.ToLower())
                    {
                        case "hour":
                            dateFrom = DateTime.Now.AddHours(-1);
                            break;
                        case "month":
                            dateFrom = DateTime.Now.AddMonths(-1);
                            break;
                        case "all":
                            dateFrom = DateTime.MinValue;
                            break;
                        case "day":
                        default:
                            dateFrom = DateTime.Now.AddDays(-1);
                            break;
                    }
                }
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


                    // Получаем словарь устройств для поиска DeviceName по Id
                    var devicesDict = scanDevices.DevicesData.Devices.ToDictionary(d => d.Id, d => d.DeviceName);

                    if (allowAdminAndManager)
                    {
                        var query = db.Actions.AsQueryable();
                        if (dateFrom != DateTime.MinValue)
                        {
                            query = query.Where(a => a.ActionTime >= dateFrom && a.ActionTime <= dateTo);
                        }
                        var actionsTable = query.OrderByDescending(a => a.ActionTime).ToList();
                        foreach (var actLine in actionsTable)
                        {
                            // Сначала заполняем DeviceName, потом вызываем FillText
                            actLine.DeviceName = devicesDict.TryGetValue(actLine.ActionStationId, out var name) && !string.IsNullOrEmpty(name)
                                ? name
                                : actLine.ActionStationId.ToString();
                            db.FillText(actLine);
                        }
                        return Json(actionsTable, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        var deviceList = scanDevices.DevicesData.Devices.Where(p => p.Owners == user.UserName).ToList<WebServer.Models.Device.Device>();
                        var query = db.Actions.Where(a => deviceList.Select(b => b.Owners).Contains(a.UserId));
                        if (dateFrom != DateTime.MinValue)
                        {
                            query = query.Where(a => a.ActionTime >= dateFrom && a.ActionTime <= dateTo);
                        }
                        var actionsTable = query.OrderByDescending(a => a.ActionTime).ToList();
                        foreach (var actLine in actionsTable)
                        {
                            // Сначала заполняем DeviceName, потом вызываем FillText
                            actLine.DeviceName = devicesDict.TryGetValue(actLine.ActionStationId, out var name2) && !string.IsNullOrEmpty(name2)
                                ? name2
                                : actLine.ActionStationId.ToString();
                            db.FillText(actLine);
                        }

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



        [HttpGet]
        [Authorize]
        public async Task<ActionResult> RefreshStation(string stationId)
        {
            try
            {
                Logger.LogInformation($"RefreshStation called with stationId: {stationId}");

                if (string.IsNullOrEmpty(stationId) || !ulong.TryParse(stationId, out ulong stationIdNum))
                {
                    Logger.LogWarning($"Invalid stationId: {stationId}");
                    return Json(new List<object>(), new JsonSerializerOptions { PropertyNamingPolicy = null });
                }

                Logger.LogInformation($"Parsed stationIdNum: {stationIdNum}");

                var userId = userManager.GetUserId(User);
                var user = await userManager.FindByIdAsync(userId);
                var roles = await userManager.GetRolesAsync(user);
                var allowAdminAndManager = roles.Contains("admin") || roles.Contains("manager");

                // Get actions for last month
                var oneMonthAgo = DateTime.Now.AddMonths(-1);

                // First check total count in DB
                var totalCount = db.Actions.Count();
                var stationCount = db.Actions.Count(a => a.ActionStationId == stationIdNum);
                Logger.LogInformation($"Total actions in DB: {totalCount}, Actions for station {stationIdNum}: {stationCount}");

                var actionsQuery = db.Actions
                    .Where(a => a.ActionStationId == stationIdNum && a.ActionTime >= oneMonthAgo)
                    .OrderByDescending(a => a.ActionTime);

                // Check permissions
                if (!allowAdminAndManager)
                {
                    var device = scanDevices.DevicesData.Devices.FirstOrDefault(d => d.Id == stationIdNum);
                    if (device == null || device.Owners != user.UserName)
                    {
                        return Json(new List<object>(), new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                }

                var actionsTable = actionsQuery.ToList();

                // Получаем словарь устройств для поиска DeviceName по Id
                var devicesDict = scanDevices.DevicesData.Devices.ToDictionary(d => d.Id, d => d.DeviceName);

                foreach (var actLine in actionsTable)
                {
                    // Сначала заполняем DeviceName, потом вызываем FillText
                    actLine.DeviceName = devicesDict.TryGetValue(actLine.ActionStationId, out var name) && !string.IsNullOrEmpty(name)
                        ? name
                        : actLine.ActionStationId.ToString();
                    db.FillText(actLine);
                }

                return Json(actionsTable, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(ActionsController)} -> {nameof(RefreshStation)} throw Exception");
                return Json(new List<object>(), new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
        }

        [HttpGet]
        [Authorize(Roles = "admin,manager")]
        public IActionResult Finance()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin,manager")]
        public async Task<ActionResult> RefreshFinance()
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

                // Payment action codes
                const int PaymentCapture = 0x4020;  // 16416 - реально списанные деньги
                const int PaymentRefund = 0x4040;   // 16448 - возвраты

                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var startOfYear = new DateTime(now.Year, 1, 1);

                // Get all capture and refund actions
                var paymentActions = db.Actions
                    .Where(a => a.ActionCode == PaymentCapture || a.ActionCode == PaymentRefund)
                    .ToList();

                // Group by station
                var stationIds = paymentActions.Select(a => a.ActionStationId).Distinct().ToList();

                var result = new List<object>();

                foreach (var stationId in stationIds)
                {
                    var stationActions = paymentActions.Where(a => a.ActionStationId == stationId).ToList();

                    // Get station name from devices
                    var device = scanDevices.DevicesData.Devices.FirstOrDefault(d => d.Id == stationId);
                    var stationName = device?.DeviceName ?? stationId.ToString();

                    // Calculate earnings (captures - refunds)
                    float monthEarnings = stationActions
                        .Where(a => a.ActionTime >= startOfMonth)
                        .Sum(a => a.ActionCode == PaymentCapture ? (a.PaymentAmount ?? 0) : -(a.PaymentAmount ?? 0));

                    float yearEarnings = stationActions
                        .Where(a => a.ActionTime >= startOfYear)
                        .Sum(a => a.ActionCode == PaymentCapture ? (a.PaymentAmount ?? 0) : -(a.PaymentAmount ?? 0));

                    float totalEarnings = stationActions
                        .Sum(a => a.ActionCode == PaymentCapture ? (a.PaymentAmount ?? 0) : -(a.PaymentAmount ?? 0));

                    // Count transactions
                    int monthTransactions = stationActions.Count(a => a.ActionTime >= startOfMonth && a.ActionCode == PaymentCapture);
                    int yearTransactions = stationActions.Count(a => a.ActionTime >= startOfYear && a.ActionCode == PaymentCapture);
                    int totalTransactions = stationActions.Count(a => a.ActionCode == PaymentCapture);

                    result.Add(new
                    {
                        StationId = stationId.ToString(),
                        StationName = stationName,
                        MonthEarnings = monthEarnings,
                        YearEarnings = yearEarnings,
                        TotalEarnings = totalEarnings,
                        MonthTransactions = monthTransactions,
                        YearTransactions = yearTransactions,
                        TotalTransactions = totalTransactions
                    });
                }

                // Add totals row
                result.Add(new
                {
                    StationId = "",
                    StationName = "TOTAL",
                    MonthEarnings = result.Sum(r => ((dynamic)r).MonthEarnings),
                    YearEarnings = result.Sum(r => ((dynamic)r).YearEarnings),
                    TotalEarnings = result.Sum(r => ((dynamic)r).TotalEarnings),
                    MonthTransactions = result.Sum(r => ((dynamic)r).MonthTransactions),
                    YearTransactions = result.Sum(r => ((dynamic)r).YearTransactions),
                    TotalTransactions = result.Sum(r => ((dynamic)r).TotalTransactions)
                });

                return Json(result, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(ActionsController)} -> {nameof(RefreshFinance)} throw Exception");
                return Json(new List<object>(), new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
        }
    }
}