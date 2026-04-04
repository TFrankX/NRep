using Microsoft.EntityFrameworkCore;
using WebServer.Models.Device;
using WebServer.Models.Settings;
using WebServer.Services.Pricing;

namespace WebServer.Services.Settings
{
    public interface IAppSettingsService
    {
        Task<List<AppSetting>> GetByCategoryAsync(string category);
        Task<AppSetting?> GetAsync(string category, string key);
        Task<string> GetValueAsync(string category, string key, string defaultValue = "");
        Task<T> GetValueAsync<T>(string category, string key, T defaultValue);
        Task SetAsync(string category, string key, string value, string modifiedBy);
        Task<List<PricingPlanSettings>> GetPricingPlansAsync();
        Task SavePricingPlanAsync(PricingPlanSettings plan, string modifiedBy);
        Task<SupportSettings> GetSupportSettingsAsync();
        Task SaveSupportSettingsAsync(SupportSettings support, string modifiedBy);
        Task<List<ServerConfigSettings>> GetServerConfigsAsync();
        Task SaveServerConfigAsync(ServerConfigSettings server, string modifiedBy);
        Task DeleteServerConfigAsync(int index, string modifiedBy);
        Task<ScanSettings> GetScanSettingsAsync();
        Task SaveScanSettingsAsync(ScanSettings settings, string modifiedBy);
        Task<List<ZoneSettings>> GetZonesAsync();
        Task SaveZoneAsync(ZoneSettings zone, string modifiedBy);
        Task DeleteZoneAsync(int zoneId, string modifiedBy);
        Task<int> GetNextZoneIdAsync();
    }

    public class AppSettingsService : IAppSettingsService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppSettingsService> _logger;
        private readonly IPricingService _pricingService;

        public AppSettingsService(IServiceScopeFactory scopeFactory, ILogger<AppSettingsService> logger, IPricingService pricingService)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _pricingService = pricingService;
        }

        public async Task<List<AppSetting>> GetByCategoryAsync(string category)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();

            return await db.AppSettings
                .Where(s => s.Category == category)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();
        }

        public async Task<AppSetting?> GetAsync(string category, string key)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();

            return await db.AppSettings
                .FirstOrDefaultAsync(s => s.Category == category && s.Key == key);
        }

        public async Task<string> GetValueAsync(string category, string key, string defaultValue = "")
        {
            var setting = await GetAsync(category, key);
            return setting?.Value ?? defaultValue;
        }

        public async Task<T> GetValueAsync<T>(string category, string key, T defaultValue)
        {
            var stringValue = await GetValueAsync(category, key, "");
            if (string.IsNullOrEmpty(stringValue))
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(stringValue, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task SetAsync(string category, string key, string value, string modifiedBy)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();

            var setting = await db.AppSettings
                .FirstOrDefaultAsync(s => s.Category == category && s.Key == key);

            if (setting != null)
            {
                setting.Value = value;
                setting.LastModified = DateTime.UtcNow;
                setting.ModifiedBy = modifiedBy;
            }
            else
            {
                db.AppSettings.Add(new AppSetting
                {
                    Category = category,
                    Key = key,
                    Value = value,
                    LastModified = DateTime.UtcNow,
                    ModifiedBy = modifiedBy
                });
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Setting updated: {Category}.{Key} = {Value} by {User}", category, key, value, modifiedBy);
        }

        public async Task<List<PricingPlanSettings>> GetPricingPlansAsync()
        {
            var settings = await GetByCategoryAsync("Pricing");
            var plans = new List<PricingPlanSettings>();

            var planNames = new[] { "PayByCard", "PayByCard2", "PayByCard3", "PayByCard4" };

            foreach (var planName in planNames)
            {
                var plan = new PricingPlanSettings
                {
                    PlanName = planName,
                    DisplayName = GetSettingValue(settings, $"{planName}.DisplayName", planName),
                    HoldAmount = GetSettingValue(settings, $"{planName}.HoldAmount", 25.0f),
                    BaseFee = GetSettingValue(settings, $"{planName}.BaseFee", 2.0f),
                    HourlyRate = GetSettingValue(settings, $"{planName}.HourlyRate", 2.0f),
                    DailyRate = GetSettingValue(settings, $"{planName}.DailyRate", 12.0f),
                    MaxDaysBeforeCapture = GetSettingValue(settings, $"{planName}.MaxDaysBeforeCapture", 3),
                    Currency = GetSettingValue(settings, $"{planName}.Currency", "eur")
                };
                plans.Add(plan);
            }

            return plans;
        }

        public async Task SavePricingPlanAsync(PricingPlanSettings plan, string modifiedBy)
        {
            await SetAsync("Pricing", $"{plan.PlanName}.DisplayName", plan.DisplayName, modifiedBy);
            await SetAsync("Pricing", $"{plan.PlanName}.HoldAmount", plan.HoldAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), modifiedBy);
            await SetAsync("Pricing", $"{plan.PlanName}.BaseFee", plan.BaseFee.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), modifiedBy);
            await SetAsync("Pricing", $"{plan.PlanName}.HourlyRate", plan.HourlyRate.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), modifiedBy);
            await SetAsync("Pricing", $"{plan.PlanName}.DailyRate", plan.DailyRate.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), modifiedBy);
            await SetAsync("Pricing", $"{plan.PlanName}.MaxDaysBeforeCapture", plan.MaxDaysBeforeCapture.ToString(), modifiedBy);
            await SetAsync("Pricing", $"{plan.PlanName}.Currency", plan.Currency, modifiedBy);

            // Reload pricing plans in memory so changes take effect immediately
            _pricingService.ReloadPlans();

            _logger.LogInformation("Pricing plan {Plan} saved and reloaded by {User}", plan.PlanName, modifiedBy);
        }

        public async Task<SupportSettings> GetSupportSettingsAsync()
        {
            var settings = await GetByCategoryAsync("Support");

            var result = new SupportSettings
            {
                Phone = GetSettingValue(settings, "Phone", "+357 99 123 456"),
                Email = GetSettingValue(settings, "Email", "support@a-charger.com"),
                WorkingHours = GetSettingValue(settings, "WorkingHours", "24/7")
            };

            _logger.LogDebug("GetSupportSettingsAsync: Found {Count} settings in Support category. Phone={Phone}, Email={Email}",
                settings.Count, result.Phone, result.Email);

            return result;
        }

        public async Task SaveSupportSettingsAsync(SupportSettings support, string modifiedBy)
        {
            _logger.LogInformation("SaveSupportSettingsAsync: Saving Phone={Phone}, Email={Email}, WorkingHours={WorkingHours} by {User}",
                support.Phone, support.Email, support.WorkingHours, modifiedBy);

            await SetAsync("Support", "Phone", support.Phone, modifiedBy);
            await SetAsync("Support", "Email", support.Email, modifiedBy);
            await SetAsync("Support", "WorkingHours", support.WorkingHours, modifiedBy);

            _logger.LogInformation("Support settings saved successfully by {User}", modifiedBy);
        }

        private static T GetSettingValue<T>(List<AppSetting> settings, string key, T defaultValue)
        {
            var setting = settings.FirstOrDefault(s => s.Key == key);
            if (setting == null || string.IsNullOrEmpty(setting.Value))
                return defaultValue;

            try
            {
                if (typeof(T) == typeof(float))
                    return (T)(object)float.Parse(setting.Value, System.Globalization.CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(int))
                    return (T)(object)int.Parse(setting.Value);
                if (typeof(T) == typeof(bool))
                    return (T)(object)bool.Parse(setting.Value);
                return (T)(object)setting.Value;
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task<List<ServerConfigSettings>> GetServerConfigsAsync()
        {
            var settings = await GetByCategoryAsync("Servers");
            var servers = new List<ServerConfigSettings>();

            // Support up to 5 servers (indexes 0-4)
            for (int i = 0; i < 5; i++)
            {
                var address = GetSettingValue(settings, $"Server{i}.Address", "");
                if (string.IsNullOrEmpty(address))
                    continue;

                servers.Add(new ServerConfigSettings
                {
                    Index = i,
                    Address = address,
                    Port = GetSettingValue(settings, $"Server{i}.Port", 8884),
                    User = GetSettingValue(settings, $"Server{i}.User", ""),
                    Pass = GetSettingValue(settings, $"Server{i}.Pass", ""),
                    ReconnectTime = GetSettingValue(settings, $"Server{i}.ReconnectTime", 30),
                    CertCA = GetSettingValue(settings, $"Server{i}.CertCA", ""),
                    CertCli = GetSettingValue(settings, $"Server{i}.CertCli", ""),
                    CertPass = GetSettingValue(settings, $"Server{i}.CertPass", ""),
                    Enabled = GetSettingValue(settings, $"Server{i}.Enabled", false)
                });
            }

            return servers;
        }

        public async Task SaveServerConfigAsync(ServerConfigSettings server, string modifiedBy)
        {
            if (server.Index < 0 || server.Index > 4)
                throw new ArgumentException("Server index must be 0-4");

            var prefix = $"Server{server.Index}";
            await SetAsync("Servers", $"{prefix}.Address", server.Address, modifiedBy);
            await SetAsync("Servers", $"{prefix}.Port", server.Port.ToString(), modifiedBy);
            await SetAsync("Servers", $"{prefix}.User", server.User, modifiedBy);
            await SetAsync("Servers", $"{prefix}.Pass", server.Pass, modifiedBy);
            await SetAsync("Servers", $"{prefix}.ReconnectTime", server.ReconnectTime.ToString(), modifiedBy);
            await SetAsync("Servers", $"{prefix}.CertCA", server.CertCA, modifiedBy);
            await SetAsync("Servers", $"{prefix}.CertCli", server.CertCli, modifiedBy);
            await SetAsync("Servers", $"{prefix}.CertPass", server.CertPass, modifiedBy);
            await SetAsync("Servers", $"{prefix}.Enabled", server.Enabled.ToString(), modifiedBy);

            _logger.LogInformation("Server config {Index} ({Address}:{Port}) saved by {User}",
                server.Index, server.Address, server.Port, modifiedBy);
        }

        public async Task DeleteServerConfigAsync(int index, string modifiedBy)
        {
            if (index < 0 || index > 4)
                throw new ArgumentException("Server index must be 0-4");

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();

            var prefix = $"Server{index}";
            var keysToDelete = new[] { "Address", "Port", "User", "Pass", "ReconnectTime", "CertCA", "CertCli", "CertPass", "Enabled" };

            foreach (var key in keysToDelete)
            {
                var setting = await db.AppSettings
                    .FirstOrDefaultAsync(s => s.Category == "Servers" && s.Key == $"{prefix}.{key}");
                if (setting != null)
                    db.AppSettings.Remove(setting);
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Server config {Index} deleted by {User}", index, modifiedBy);
        }

        public async Task<ScanSettings> GetScanSettingsAsync()
        {
            var settings = await GetByCategoryAsync("Scan");

            return new ScanSettings
            {
                InventoryPeriodSeconds = GetSettingValue(settings, "InventoryPeriodSeconds", 300),
                OfflineRetryCount = GetSettingValue(settings, "OfflineRetryCount", 3),
                RetryDelaySeconds = GetSettingValue(settings, "RetryDelaySeconds", 5),
                ResponseTimeoutSeconds = GetSettingValue(settings, "ResponseTimeoutSeconds", 10)
            };
        }

        public async Task SaveScanSettingsAsync(ScanSettings scanSettings, string modifiedBy)
        {
            await SetAsync("Scan", "InventoryPeriodSeconds", scanSettings.InventoryPeriodSeconds.ToString(), modifiedBy);
            await SetAsync("Scan", "OfflineRetryCount", scanSettings.OfflineRetryCount.ToString(), modifiedBy);
            await SetAsync("Scan", "RetryDelaySeconds", scanSettings.RetryDelaySeconds.ToString(), modifiedBy);
            await SetAsync("Scan", "ResponseTimeoutSeconds", scanSettings.ResponseTimeoutSeconds.ToString(), modifiedBy);

            _logger.LogInformation("Scan settings saved: InventoryPeriod={Period}s, Retries={Retries}, RetryDelay={Delay}s, ResponseTimeout={Timeout}s by {User}",
                scanSettings.InventoryPeriodSeconds, scanSettings.OfflineRetryCount,
                scanSettings.RetryDelaySeconds, scanSettings.ResponseTimeoutSeconds, modifiedBy);
        }

        public async Task<List<ZoneSettings>> GetZonesAsync()
        {
            var settings = await GetByCategoryAsync("Zones");
            var zones = new List<ZoneSettings>();

            // Get all unique zone IDs from settings
            var zoneIds = new HashSet<int>();
            foreach (var setting in settings)
            {
                if (setting.Key.Contains(".") && int.TryParse(setting.Key.Split('.')[0], out var id))
                    zoneIds.Add(id);
            }

            foreach (var zoneId in zoneIds.OrderBy(z => z))
            {
                zones.Add(new ZoneSettings
                {
                    Id = zoneId,
                    Name = GetSettingValue(settings, $"{zoneId}.Name", ""),
                    Color = GetSettingValue(settings, $"{zoneId}.Color", "#7C3AED"),
                    Language = GetSettingValue(settings, $"{zoneId}.Language", "en")
                });
            }

            return zones;
        }

        public async Task SaveZoneAsync(ZoneSettings zone, string modifiedBy)
        {
            await SetAsync("Zones", $"{zone.Id}.Name", zone.Name, modifiedBy);
            await SetAsync("Zones", $"{zone.Id}.Color", zone.Color, modifiedBy);
            await SetAsync("Zones", $"{zone.Id}.Language", zone.Language, modifiedBy);

            _logger.LogInformation("Zone {ZoneId} ({Name}) saved by {User}", zone.Id, zone.Name, modifiedBy);
        }

        public async Task DeleteZoneAsync(int zoneId, string modifiedBy)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();

            var keysToDelete = new[] { "Name", "Color", "Language" };

            foreach (var key in keysToDelete)
            {
                var setting = await db.AppSettings
                    .FirstOrDefaultAsync(s => s.Category == "Zones" && s.Key == $"{zoneId}.{key}");
                if (setting != null)
                    db.AppSettings.Remove(setting);
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Zone {ZoneId} deleted by {User}", zoneId, modifiedBy);
        }

        public async Task<int> GetNextZoneIdAsync()
        {
            var zones = await GetZonesAsync();
            if (zones.Count == 0)
                return 1;
            return zones.Max(z => z.Id) + 1;
        }
    }
}
