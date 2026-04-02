using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.Settings;
using WebServer.Services.Settings;
using WebServer.Workers;
using System.Security.Claims;

namespace WebServer.Controllers.Settings
{
    [Authorize(Roles = "admin")]
    public class SettingsController : Controller
    {
        private readonly IAppSettingsService _settingsService;
        private readonly ILogger<SettingsController> _logger;
        private readonly ScanDevices _scanDevices;

        public SettingsController(IAppSettingsService settingsService, ILogger<SettingsController> logger, ScanDevices scanDevices)
        {
            _settingsService = settingsService;
            _logger = logger;
            _scanDevices = scanDevices;
        }

        [HttpGet]
        public async Task<IActionResult> Settings(string section = "pricing")
        {
            var model = new SettingsViewModel
            {
                ActiveSection = section,
                PricingPlans = await _settingsService.GetPricingPlansAsync(),
                Support = await _settingsService.GetSupportSettingsAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePricingPlan(PricingPlanSettings plan)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userName = User.Identity?.Name ?? "unknown";

            await _settingsService.SavePricingPlanAsync(plan, userName);

            TempData["SuccessMessage"] = $"Pricing plan '{plan.DisplayName}' saved successfully.";

            return RedirectToAction(nameof(Settings), new { section = "pricing" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePricingPlanAjax([FromBody] PricingPlanSettings plan)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid data" });
            }

            try
            {
                var userName = User.Identity?.Name ?? "unknown";
                await _settingsService.SavePricingPlanAsync(plan, userName);

                return Ok(new { success = true, message = $"Plan '{plan.DisplayName}' saved" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save pricing plan {Plan}", plan.PlanName);
                return StatusCode(500, new { success = false, message = "Failed to save settings" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSupportSettingsAjax([FromBody] SupportSettings support)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid data" });
            }

            try
            {
                var userName = User.Identity?.Name ?? "unknown";
                await _settingsService.SaveSupportSettingsAsync(support, userName);

                return Ok(new { success = true, message = "Support settings saved" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save support settings");
                return StatusCode(500, new { success = false, message = "Failed to save settings" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReloadDatabase()
        {
            try
            {
                var userName = User.Identity?.Name ?? "unknown";
                _logger.LogInformation("Database reload requested by {User}", userName);

                var (devices, powerBanks) = await _scanDevices.ReloadFromDatabaseAsync();

                return Ok(new {
                    success = true,
                    message = $"Database reloaded successfully. Devices: {devices}, PowerBanks: {powerBanks}",
                    devices,
                    powerBanks
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload database");
                return StatusCode(500, new { success = false, message = "Failed to reload database: " + ex.Message });
            }
        }
    }
}
