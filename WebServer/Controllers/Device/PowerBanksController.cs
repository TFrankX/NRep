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
        private readonly ActionProcess _actionProcess;

        public PowerBanksController(UserManager<AppUser> _userManager, ScanDevices scanDevices, ILogger<PowerBanksController> logger, IPricingService pricingService, DeviceContext deviceContext, ActionProcess actionProcess)
        {
            userManager = _userManager;
            Logger = logger;
            this.scanDevices = scanDevices;
            _pricingService = pricingService;
            _deviceContext = deviceContext;
            _actionProcess = actionProcess;
        }

        [HttpGet]
        [Authorize]
        public IActionResult PowerBanks(string? filter = null)
        {
            ViewBag.Filter = filter;
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
        public IActionResult Statistics(string? filter = null)
        {
            ViewBag.Filter = filter;
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
                        var duration = pb.Taken ? DateTime.UtcNow - pb.LastGetTime : pb.LastPutTime - pb.LastGetTime;

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

                        // Определяем статус с учетом состояния станции
                        string status;
                        bool hasRentalHistory = pb.LastGetTime > new DateTime(2000, 1, 1);
                        var isStationOnline = device?.Online ?? false;

                        // If station is offline, show offline status with last known state
                        if (!isStationOnline)
                        {
                            if (pb.Taken)
                                status = "Offline";
                            else if (pb.Plugged && hasRentalHistory)
                                status = "Returned (Offline)";
                            else if (pb.Plugged)
                                status = "In station (Offline)";
                            else
                                status = "Offline";
                        }
                        else if (pb.Taken)
                        {
                            status = "On hands";
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
                            PowerBankId = pb.Name,
                            PowerBankIdNum = pb.Id_str,  // Keep numeric ID for delete operation
                            StationName = pb.HostDeviceName,
                            StationId = device?.Id_str ?? "",
                            Slot = pb.HostSlot,
                            UserId = pb.UserId ?? "",
                            Status = status,
                            TakeTime = hasRentalHistory ? pb.LastGetTime.ToString("yyyy-MM-ddTHH:mm:ssZ") : "",
                            ReturnTime = hasRentalHistory && !pb.Taken ? pb.LastPutTime.ToString("yyyy-MM-ddTHH:mm:ssZ") : "",
                            Duration = hasRentalHistory && duration.TotalMinutes > 0 ? $"{(int)duration.TotalHours}h {(int)(duration.TotalMinutes % 60)}m" : "-",
                            Cost = currentCost,
                            TotalEarnings = pb.TotalEarnings,
                            PaymentInfo = pb.PaymentInfo ?? "",
                            SessionId = !string.IsNullOrEmpty(pb.SessionId) && pb.SessionId != "\"\"" ? "Active" : "",
                            ChargeLevel = pb.ChargeLevel,
                            // Technical fields
                            Plugged = pb.Plugged,
                            Locked = pb.Locked,
                            Charging = pb.Charging,
                            IsOk = pb.IsOk,
                            LastGetTime = pb.LastGetTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            LastPutTime = pb.LastPutTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            LastUpdate = pb.LastUpdate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            CanDelete = status.Contains("Offline") || status == "On hands"
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

        [HttpPost]
        [Authorize(Roles = "admin,manager")]
        public async Task<ActionResult> DeletePowerBank([FromBody] DeletePowerBankRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.PowerBankId) || !ulong.TryParse(request.PowerBankId, out ulong pbId))
                {
                    return Json(new { success = false, message = "Invalid PowerBank ID" });
                }

                // Получаем информацию о текущем пользователе
                var currentUser = await userManager.GetUserAsync(User);
                var userName = currentUser?.UserName ?? "unknown";

                // Проверяем что повербанк можно удалить (только Offline или On hands)
                var memPowerBank = scanDevices.DevicesData.PowerBanks.FirstOrDefault(p => p.Id == pbId);
                string deviceName = "";
                ulong deviceId = 0;
                uint slot = 0;

                if (memPowerBank != null)
                {
                    if (memPowerBank.Plugged && !memPowerBank.Taken)
                    {
                        return Json(new { success = false, message = "Cannot delete a powerbank that is currently in station. Only offline or taken powerbanks can be deleted." });
                    }
                    deviceName = memPowerBank.HostDeviceName;
                    deviceId = memPowerBank.HostDeviceId;
                    slot = memPowerBank.HostSlot;
                }

                // Удаляем из БД
                var dbPowerBank = await _deviceContext.PowerBank.FirstOrDefaultAsync(p => p.Id == pbId);
                if (dbPowerBank != null)
                {
                    if (string.IsNullOrEmpty(deviceName))
                    {
                        deviceName = dbPowerBank.HostDeviceName;
                        deviceId = dbPowerBank.HostDeviceId;
                        slot = dbPowerBank.HostSlot;
                    }
                    _deviceContext.PowerBank.Remove(dbPowerBank);
                    await _deviceContext.SaveChangesAsync();
                    Logger.LogInformation($"Deleted PowerBank {pbId} from database");
                }

                // Удаляем из памяти
                if (memPowerBank != null)
                {
                    scanDevices.DevicesData.PowerBanks.Remove(memPowerBank);
                    Logger.LogInformation($"Removed PowerBank {pbId} from memory");
                }

                // Логируем в Actions
                _actionProcess.ActionSave(
                    (int)ActionsDescription.PowerBankDelete,
                    userName,
                    0,  // serverId
                    deviceId,
                    pbId,
                    slot,
                    $"PowerBank {pbId} in device {deviceName} slot {slot} - deleted from system by user {userName}"
                );

                return Json(new { success = true, message = "PowerBank deleted successfully" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{nameof(PowerBanksController)} -> {nameof(DeletePowerBank)} throw Exception");
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

    public class DeletePowerBankRequest
    {
        public string PowerBankId { get; set; }
    }
}