using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.Settings;
using WebServer.Services.Settings;
using WebServer.Workers;
using System.Security.Claims;
using System.IO;

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
                Support = await _settingsService.GetSupportSettingsAsync(),
                Servers = await _settingsService.GetServerConfigsAsync()
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveServerConfigAjax([FromBody] ServerConfigSettings server)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid data" });
            }

            try
            {
                var userName = User.Identity?.Name ?? "unknown";

                // Check max 5 servers
                if (server.Index < 0 || server.Index > 4)
                {
                    return BadRequest(new { success = false, message = "Maximum 5 servers allowed" });
                }

                // Save to settings database
                await _settingsService.SaveServerConfigAsync(server, userName);

                // Apply changes immediately - add/update the server in runtime
                await _scanDevices.AddOrUpdateServerAsync(server);

                return Ok(new { success = true, message = $"Server '{server.Address}:{server.Port}' saved and activated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save server config");
                return StatusCode(500, new { success = false, message = "Failed to save settings" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteServerConfigAjax([FromBody] DeleteServerRequest request)
        {
            try
            {
                var userName = User.Identity?.Name ?? "unknown";

                // Get server info before deletion to know what to remove
                var servers = await _settingsService.GetServerConfigsAsync();
                var serverToDelete = servers.FirstOrDefault(s => s.Index == request.Index);

                // Delete from settings
                await _settingsService.DeleteServerConfigAsync(request.Index, userName);

                // Remove from runtime if it was active
                if (serverToDelete != null)
                {
                    await _scanDevices.RemoveServerAsync(serverToDelete.Address, serverToDelete.Port);
                }

                return Ok(new { success = true, message = "Server deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete server config");
                return StatusCode(500, new { success = false, message = "Failed to delete server" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetServerConfigs()
        {
            var servers = await _settingsService.GetServerConfigsAsync();
            return Json(servers);
        }

        public class DeleteServerRequest
        {
            public int Index { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestServerConnection([FromBody] ServerConfigSettings server)
        {
            try
            {
                var userName = User.Identity?.Name ?? "unknown";
                _logger.LogInformation("Testing server connection {Address}:{Port} by {User}",
                    server.Address, server.Port, userName);

                var (success, message) = await _scanDevices.TestServerConnectionAsync(server);

                return Ok(new {
                    success,
                    message,
                    status = success ? "connected" : "failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test server connection");
                return Ok(new {
                    success = false,
                    message = ex.Message,
                    status = "error"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCertificate(int serverIndex, string certType, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file uploaded" });
                }

                // Validate file extension
                var allowedExtensions = new[] { ".crt", ".pem", ".key", ".p12", ".pfx", ".cer" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { success = false, message = "Invalid file type. Allowed: .crt, .pem, .key, .p12, .pfx, .cer" });
                }

                // Create directory for server certificates
                var certsDir = Path.Combine(Directory.GetCurrentDirectory(), "certificates", $"server{serverIndex}");
                if (!Directory.Exists(certsDir))
                {
                    Directory.CreateDirectory(certsDir);
                }

                // Determine filename based on cert type
                string fileName;
                switch (certType.ToLower())
                {
                    case "ca":
                        fileName = $"ca{extension}";
                        break;
                    case "client":
                        fileName = $"client{extension}";
                        break;
                    default:
                        return BadRequest(new { success = false, message = "Invalid certificate type" });
                }

                var filePath = Path.Combine(certsDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var userName = User.Identity?.Name ?? "unknown";
                _logger.LogInformation("Certificate {CertType} uploaded for server {Index} by {User}: {Path}",
                    certType, serverIndex, userName, filePath);

                // Return relative path for saving in settings
                var relativePath = Path.Combine("certificates", $"server{serverIndex}", fileName);

                return Ok(new { success = true, path = relativePath, message = $"Certificate uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload certificate");
                return StatusCode(500, new { success = false, message = "Failed to upload certificate" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCertificate(int serverIndex, string certType)
        {
            try
            {
                var certsDir = Path.Combine(Directory.GetCurrentDirectory(), "certificates", $"server{serverIndex}");

                if (!Directory.Exists(certsDir))
                {
                    return Ok(new { success = true, message = "No certificate to delete" });
                }

                // Find and delete the certificate file
                var pattern = certType.ToLower() == "ca" ? "ca.*" : "client.*";
                var files = Directory.GetFiles(certsDir, pattern);

                foreach (var file in files)
                {
                    System.IO.File.Delete(file);
                    _logger.LogInformation("Certificate deleted: {Path}", file);
                }

                return Ok(new { success = true, message = "Certificate deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete certificate");
                return StatusCode(500, new { success = false, message = "Failed to delete certificate" });
            }
        }
    }
}
