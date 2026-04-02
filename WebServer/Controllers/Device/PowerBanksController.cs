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
using WebServer.Workers;
using WebServer.Data;
using System.Data;
using WebServer.Services.Pricing;

namespace WebServer.Controllers.Device
{

    public class PowerBanksController : Controller
    {
        private readonly ILogger<PowerBanksController> Logger;
        private readonly UserManager<AppUser> userManager;
        private readonly ScanDevices scanDevices;
        private readonly IPricingService _pricingService;
        private readonly DeviceContext _deviceContext;

        public PowerBanksController(UserManager<AppUser> _userManager, ScanDevices scanDevices, ILogger<PowerBanksController> logger, IPricingService pricingService, DeviceContext deviceContext)
        {
            userManager = _userManager;
            Logger = logger;
            this.scanDevices = scanDevices;
            _pricingService = pricingService;
            _deviceContext = deviceContext;
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

        [HttpGet]
        [Authorize]
        public IActionResult Statistics()
        {
            return View();
        }

        [Authorize]
        public async Task<ActionResult> RefreshStatistics()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                var user = await userManager.FindByIdAsync(userId);
                var roles = await userManager.GetRolesAsync(user);

                var allowAdminAndManager = roles.Contains("admin") || roles.Contains("manager");

                // Получаем все павербанки из БД (не только от онлайн устройств)
                List<PowerBank> powerBanks;
                try
                {
                    powerBanks = _deviceContext.PowerBank.AsNoTracking().ToList();
                    Logger.LogInformation($"RefreshStatistics: Loaded {powerBanks.Count} powerbanks from DB");
                }
                catch (Exception dbEx)
                {
                    Logger.LogError(dbEx, "RefreshStatistics: Failed to load from DB, falling back to in-memory");
                    powerBanks = scanDevices.DevicesData.PowerBanks.ToList();
                }

                var devices = scanDevices.DevicesData.Devices.ToList();

                // Обновляем данные из in-memory коллекции для онлайн устройств
                var onlinePowerBanks = scanDevices.DevicesData.PowerBanks.ToDictionary(p => p.Id);
                foreach (var pb in powerBanks)
                {
                    if (onlinePowerBanks.TryGetValue(pb.Id, out var onlinePb))
                    {
                        // Обновляем актуальные данные из памяти
                        pb.Taken = onlinePb.Taken;
                        pb.Plugged = onlinePb.Plugged;
                        pb.UserId = onlinePb.UserId;
                        pb.LastGetTime = onlinePb.LastGetTime;
                        pb.LastPutTime = onlinePb.LastPutTime;
                        pb.Cost = onlinePb.Cost;
                        pb.SessionId = onlinePb.SessionId;
                        pb.PaymentInfo = onlinePb.PaymentInfo;
                        pb.ChargeLevel = onlinePb.ChargeLevel;
                        pb.TotalEarnings = onlinePb.TotalEarnings;
                    }
                }

                if (!allowAdminAndManager)
                {
                    var userDeviceNames = devices
                        .Where(d => d.Owners == user.UserName)
                        .Select(d => d.DeviceName)
                        .ToList();
                    powerBanks = powerBanks.Where(p => userDeviceNames.Contains(p.HostDeviceName)).ToList();
                }

                // Показываем все павербанки
                var statistics = powerBanks
                    .Select(pb => {
                        var device = devices.FirstOrDefault(d => d.DeviceName == pb.HostDeviceName);
                        var typeOfUse = device?.TypeOfUse ?? TypeOfUse.PayByCard;
                        var duration = pb.Taken ? DateTime.Now - pb.LastGetTime : pb.LastPutTime - pb.LastGetTime;

                        // Рассчитываем/показываем стоимость аренды
                        float currentCost = 0;
                        var hasStripeSession = !string.IsNullOrEmpty(pb.SessionId) && pb.SessionId != "\"\"";

                        if (pb.Taken && _pricingService.IsPaidOption(typeOfUse) && hasStripeSession)
                        {
                            // Повербанк на руках - считаем стоимость на текущий момент
                            currentCost = _pricingService.CalculateCost(typeOfUse, pb.LastGetTime, true);
                        }
                        else if (pb.Cost > 0)
                        {
                            // Повербанк вернули - показываем сохранённую стоимость последней аренды
                            currentCost = pb.Cost;
                        }

                        // Определяем статус:
                        // - In station: если в станции и никогда не брали
                        // - Returned: если вернули (Taken=false и Plugged=true и была аренда)
                        // - On hands: если взяли, станция онлайн и не вернули
                        // - Offline: если взяли, но станция офлайн
                        string status;
                        bool hasRentalHistory = pb.LastGetTime > new DateTime(2000, 1, 1);

                        if (pb.Taken)
                        {
                            var isStationOnline = device?.Online ?? false;
                            status = isStationOnline ? "On hands" : "Offline";
                        }
                        else if (pb.Plugged && hasRentalHistory)
                        {
                            status = "Returned";
                        }
                        else if (pb.Plugged)
                        {
                            status = "In station";
                        }
                        else
                        {
                            status = "Unknown";
                        }

                        return new {
                            PowerBankId = pb.Id_str,
                            StationName = pb.HostDeviceName,
                            StationId = device?.Id_str ?? "",
                            Slot = pb.HostSlot,
                            UserId = pb.UserId ?? "",
                            Status = status,
                            TakeTime = hasRentalHistory ? pb.LastGetTime : (DateTime?)null,
                            ReturnTime = hasRentalHistory && !pb.Taken ? pb.LastPutTime : (DateTime?)null,
                            Duration = hasRentalHistory && duration.TotalMinutes > 0 ? $"{(int)duration.TotalHours}h {(int)(duration.TotalMinutes % 60)}m" : "-",
                            Cost = currentCost,
                            TotalEarnings = pb.TotalEarnings,
                            PaymentInfo = pb.PaymentInfo ?? "",
                            SessionId = !string.IsNullOrEmpty(pb.SessionId) && pb.SessionId != "\"\"" ? "Active" : "",
                            ChargeLevel = pb.ChargeLevel
                        };
                    })
                    .OrderByDescending(x => x.TakeTime)
                    .ToList();

                // Общая статистика по доходам
                var currentRevenue = statistics.Sum(s => s.Cost);  // Текущий сбор (активные + последние завершённые аренды)
                var totalRevenue = powerBanks.Sum(pb => pb.TotalEarnings);  // Накопленный доход со всех PowerBank

                return Json(new {
                    items = statistics,
                    currentRevenue = currentRevenue,
                    totalRevenue = totalRevenue
                }, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(PowerBanksController)} -> {nameof(RefreshStatistics)} throw Exception");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin,manager")]
        public async Task<ActionResult> ResetPowerBankEarnings([FromBody] ResetEarningsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.PowerBankId) || !ulong.TryParse(request.PowerBankId, out ulong pbId))
                {
                    return Json(new { success = false, message = "Invalid PowerBank ID" });
                }

                // Обновляем в БД
                var dbPowerBank = await _deviceContext.PowerBank.FirstOrDefaultAsync(p => p.Id == pbId);
                if (dbPowerBank != null)
                {
                    if (request.ResetCost)
                        dbPowerBank.Cost = 0;
                    if (request.ResetTotal)
                        dbPowerBank.TotalEarnings = 0;
                    await _deviceContext.SaveChangesAsync();
                }

                // Обновляем в памяти
                var memPowerBank = scanDevices.DevicesData.PowerBanks.FirstOrDefault(p => p.Id == pbId);
                if (memPowerBank != null)
                {
                    if (request.ResetCost)
                        memPowerBank.Cost = 0;
                    if (request.ResetTotal)
                        memPowerBank.TotalEarnings = 0;
                }

                var what = request.ResetCost && request.ResetTotal ? "Cost and TotalEarnings" :
                           request.ResetCost ? "Cost" : "TotalEarnings";
                Logger.LogInformation($"Reset {what} for PowerBank {pbId}");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(PowerBanksController)} -> {nameof(ResetPowerBankEarnings)} throw Exception");
                return Json(new { success = false, message = ex.Message });
            }
        }

    }

    public class ResetEarningsRequest
    {
        public string PowerBankId { get; set; }
        public bool ResetCost { get; set; }
        public bool ResetTotal { get; set; }
    }
}