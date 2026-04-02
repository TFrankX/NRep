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
    }
}
