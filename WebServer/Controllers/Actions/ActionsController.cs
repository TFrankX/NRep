using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebServer.Models.Device;
using WebServer.Models.Identity;
using WebServer.Models.Action;
using WebServer.Models.Finance;
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
        private readonly DeviceContext deviceDb;

        public ActionsController(UserManager<AppUser> _userManager, ScanDevices scanDevices, ILogger<DevicesController> logger,
            WebServer.Models.Action.ActionContext context, DeviceContext deviceContext)
        {
            userManager = _userManager;
            Logger = logger;
            db = context;
            deviceDb = deviceContext;
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
                    return Json(new List<FinanceStationRow>(), new JsonSerializerOptions { PropertyNamingPolicy = null });
                }

                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var startOfYear = new DateTime(now.Year, 1, 1);

                // Get all transactions from FinancialTransactions table
                var transactions = await deviceDb.FinancialTransactions
                    .Where(t => t.Type == TransactionType.Capture || t.Type == TransactionType.Refund)
                    .ToListAsync();

                Logger.LogInformation("RefreshFinance: Found {Count} financial transactions", transactions.Count);

                // Group by station
                var stationIds = transactions.Select(t => t.StationId).Distinct().ToList();

                var result = new List<FinanceStationRow>();

                foreach (var stationId in stationIds)
                {
                    var stationTransactions = transactions.Where(t => t.StationId == stationId).ToList();

                    // Get station name (prefer stored name, fallback to device lookup)
                    var stationName = stationTransactions.FirstOrDefault()?.StationName;
                    if (string.IsNullOrEmpty(stationName))
                    {
                        var device = scanDevices.DevicesData.Devices.FirstOrDefault(d => d.Id == stationId);
                        stationName = device?.DeviceName ?? stationId.ToString();
                    }

                    // Calculate earnings (captures - refunds)
                    decimal monthEarnings = stationTransactions
                        .Where(t => t.TransactionTime >= startOfMonth)
                        .Sum(t => t.SignedAmount);

                    decimal yearEarnings = stationTransactions
                        .Where(t => t.TransactionTime >= startOfYear)
                        .Sum(t => t.SignedAmount);

                    decimal totalEarnings = stationTransactions.Sum(t => t.SignedAmount);

                    // Count capture transactions only
                    int monthTx = stationTransactions.Count(t => t.TransactionTime >= startOfMonth && t.Type == TransactionType.Capture);
                    int yearTx = stationTransactions.Count(t => t.TransactionTime >= startOfYear && t.Type == TransactionType.Capture);
                    int totalTx = stationTransactions.Count(t => t.Type == TransactionType.Capture);

                    result.Add(new FinanceStationRow
                    {
                        StationId = stationId.ToString(),
                        StationName = stationName,
                        MonthEarnings = (float)monthEarnings,
                        YearEarnings = (float)yearEarnings,
                        TotalEarnings = (float)totalEarnings,
                        MonthTransactions = monthTx,
                        YearTransactions = yearTx,
                        TotalTransactions = totalTx
                    });
                }

                // Add totals row
                if (result.Count > 0)
                {
                    result.Add(new FinanceStationRow
                    {
                        StationId = "",
                        StationName = "TOTAL",
                        MonthEarnings = result.Sum(r => r.MonthEarnings),
                        YearEarnings = result.Sum(r => r.YearEarnings),
                        TotalEarnings = result.Sum(r => r.TotalEarnings),
                        MonthTransactions = result.Sum(r => r.MonthTransactions),
                        YearTransactions = result.Sum(r => r.YearTransactions),
                        TotalTransactions = result.Sum(r => r.TotalTransactions)
                    });
                }

                return Json(result, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(ActionsController)} -> {nameof(RefreshFinance)} throw Exception");
                return Json(new List<FinanceStationRow>(), new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
        }

        public class FinanceStationRow
        {
            public string StationId { get; set; } = "";
            public string StationName { get; set; } = "";
            public float MonthEarnings { get; set; }
            public float YearEarnings { get; set; }
            public float TotalEarnings { get; set; }
            public int MonthTransactions { get; set; }
            public int YearTransactions { get; set; }
            public int TotalTransactions { get; set; }
        }

        /// <summary>
        /// Add a financial transaction record
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> AddTransaction([FromBody] AddTransactionRequest request)
        {
            try
            {
                var transaction = new FinancialTransaction
                {
                    TransactionTime = DateTime.Now,
                    Type = request.Type,
                    Amount = request.Amount,
                    StationId = request.StationId,
                    StationName = request.StationName ?? "",
                    PowerBankId = request.PowerBankId,
                    UserId = request.UserId ?? "",
                    CustomerName = request.CustomerName ?? "",
                    PaymentReference = request.PaymentReference ?? "",
                    SessionId = request.SessionId ?? "",
                    Description = request.Description ?? ""
                };

                deviceDb.FinancialTransactions.Add(transaction);
                await deviceDb.SaveChangesAsync();

                Logger.LogInformation("Added financial transaction: Type={Type}, Amount={Amount}, Station={Station}",
                    transaction.Type, transaction.Amount, transaction.StationName);

                return Json(new { success = true, id = transaction.Id });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to add financial transaction");
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class AddTransactionRequest
        {
            public TransactionType Type { get; set; }
            public decimal Amount { get; set; }
            public ulong StationId { get; set; }
            public string? StationName { get; set; }
            public ulong PowerBankId { get; set; }
            public string? UserId { get; set; }
            public string? CustomerName { get; set; }
            public string? PaymentReference { get; set; }
            public string? SessionId { get; set; }
            public string? Description { get; set; }
        }

        /// <summary>
        /// Delete a financial transaction record
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteTransaction([FromBody] DeleteTransactionRequest request)
        {
            try
            {
                var transaction = await deviceDb.FinancialTransactions.FindAsync(request.Id);
                if (transaction == null)
                {
                    return Json(new { success = false, message = "Transaction not found" });
                }

                deviceDb.FinancialTransactions.Remove(transaction);
                await deviceDb.SaveChangesAsync();

                var userName = User.Identity?.Name ?? "unknown";
                Logger.LogInformation("Deleted financial transaction {Id} by {User}: Type={Type}, Amount={Amount}, Station={Station}",
                    request.Id, userName, transaction.Type, transaction.Amount, transaction.StationName);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to delete financial transaction {Id}", request.Id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class DeleteTransactionRequest
        {
            public long Id { get; set; }
        }

        /// <summary>
        /// Get all financial transactions (for detailed view)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "admin,manager")]
        public async Task<ActionResult> GetTransactions(int? limit = 100)
        {
            try
            {
                var transactions = await deviceDb.FinancialTransactions
                    .OrderByDescending(t => t.TransactionTime)
                    .Take(limit ?? 100)
                    .Select(t => new
                    {
                        t.Id,
                        TransactionTime = t.TransactionTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        Type = t.Type.ToString(),
                        t.Amount,
                        t.StationId,
                        t.StationName,
                        t.PowerBankId,
                        t.UserId,
                        t.CustomerName,
                        t.PaymentReference,
                        t.SessionId,
                        t.Description
                    })
                    .ToListAsync();

                return Json(transactions, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get financial transactions");
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Get financial transactions for a specific station (for row details)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "admin,manager")]
        public async Task<ActionResult> GetTransactionsByStation(ulong stationId)
        {
            try
            {
                var transactions = await deviceDb.FinancialTransactions
                    .Where(t => t.StationId == stationId)
                    .OrderByDescending(t => t.TransactionTime)
                    .Take(100)
                    .Select(t => new
                    {
                        t.Id,
                        TransactionTime = t.TransactionTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        Type = t.Type.ToString(),
                        t.Amount,
                        SignedAmount = t.Type == TransactionType.Refund ? -t.Amount : t.Amount,
                        t.PowerBankId,
                        t.CustomerName,
                        t.CardInfo,
                        t.CardExpiry,
                        t.CardCountry,
                        t.UserId,
                        t.SessionId
                    })
                    .ToListAsync();

                return Json(transactions, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get transactions for station {StationId}", stationId);
                return Json(new List<object>());
            }
        }
    }
}