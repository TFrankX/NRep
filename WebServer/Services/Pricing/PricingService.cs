using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebServer.Models.Device;
using WebServer.Models.Settings;

namespace WebServer.Services.Pricing
{
    public interface IPricingService
    {
        /// <summary>
        /// Get pricing plan for a specific TypeOfUse
        /// </summary>
        PricingPlan GetPlan(TypeOfUse typeOfUse);

        /// <summary>
        /// Calculate rental cost based on duration and pricing plan
        /// </summary>
        float CalculateCost(TypeOfUse typeOfUse, DateTime rentalStartTime, bool powerBankTaken);

        /// <summary>
        /// Check if a TypeOfUse is a paid option (PayByCard variants)
        /// </summary>
        bool IsPaidOption(TypeOfUse typeOfUse);

        /// <summary>
        /// Reload pricing plans from database
        /// </summary>
        void ReloadPlans();
    }

    public class PricingService : IPricingService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PricingService> _logger;
        private Dictionary<string, PricingPlan> _plans = new();
        private DateTime _lastReload = DateTime.MinValue;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);

        public PricingService(IServiceScopeFactory scopeFactory, ILogger<PricingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            ReloadPlans();
        }

        public void ReloadPlans()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();

                var settings = db.AppSettings
                    .Where(s => s.Category == "Pricing")
                    .ToList();

                var planNames = new[] { "PayByCard", "PayByCard2", "PayByCard3", "PayByCard4" };
                var newPlans = new Dictionary<string, PricingPlan>();

                foreach (var planName in planNames)
                {
                    var plan = new PricingPlan
                    {
                        HoldAmount = GetFloatValue(settings, $"{planName}.HoldAmount", 25.0f),
                        BaseFee = GetFloatValue(settings, $"{planName}.BaseFee", 2.0f),
                        HourlyRate = GetFloatValue(settings, $"{planName}.HourlyRate", 2.0f),
                        DailyRate = GetFloatValue(settings, $"{planName}.DailyRate", 12.0f),
                        MaxDaysBeforeCapture = GetIntValue(settings, $"{planName}.MaxDaysBeforeCapture", 3),
                        Currency = GetStringValue(settings, $"{planName}.Currency", "eur")
                    };
                    newPlans[planName] = plan;
                }

                _plans = newPlans;
                _lastReload = DateTime.UtcNow;
                _logger.LogInformation("Pricing plans reloaded from database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload pricing plans from database, using defaults");
                // Initialize with defaults if DB read fails
                if (_plans.Count == 0)
                {
                    _plans["PayByCard"] = new PricingPlan();
                    _plans["PayByCard2"] = new PricingPlan();
                    _plans["PayByCard3"] = new PricingPlan();
                    _plans["PayByCard4"] = new PricingPlan();
                }
            }
        }

        private void EnsurePlansLoaded()
        {
            if (DateTime.UtcNow - _lastReload > _cacheTimeout)
            {
                ReloadPlans();
            }
        }

        public PricingPlan GetPlan(TypeOfUse typeOfUse)
        {
            EnsurePlansLoaded();

            var planName = typeOfUse switch
            {
                TypeOfUse.PayByCard => "PayByCard",
                TypeOfUse.PayByCard2 => "PayByCard2",
                TypeOfUse.PayByCard3 => "PayByCard3",
                TypeOfUse.PayByCard4 => "PayByCard4",
                _ => "PayByCard"
            };

            return _plans.TryGetValue(planName, out var plan) ? plan : new PricingPlan();
        }

        public bool IsPaidOption(TypeOfUse typeOfUse)
        {
            return typeOfUse == TypeOfUse.PayByCard ||
                   typeOfUse == TypeOfUse.PayByCard2 ||
                   typeOfUse == TypeOfUse.PayByCard3 ||
                   typeOfUse == TypeOfUse.PayByCard4;
        }

        public float CalculateCost(TypeOfUse typeOfUse, DateTime rentalStartTime, bool powerBankTaken)
        {
            // Free modes don't charge anything
            if (!IsPaidOption(typeOfUse))
            {
                return 0;
            }

            var plan = GetPlan(typeOfUse);
            var duration = DateTime.UtcNow - rentalStartTime;

            // If returned within 1 minute, no charge (grace period)
            if (duration.TotalMinutes < 1)
            {
                return 0;
            }

            // If powerbank not taken (still held), return full daily rate as estimate
            if (!powerBankTaken)
            {
                return plan.DailyRate;
            }

            // Алгоритм расчёта:
            // 1. До 1 часа: BaseFee (фиксированная плата за первый час)
            // 2. Больше 1 часа: количество часов × HourlyRate (BaseFee не учитывается)
            // 3. Лимит за первые сутки: DailyRate
            // 4. После суток: количество дней × DailyRate
            // 5. Общий лимит: HoldAmount

            float cost;

            if (duration.TotalHours <= 1)
            {
                // До 1 часа - только BaseFee
                cost = plan.BaseFee;
            }
            else if (duration.TotalHours < 24)
            {
                // Больше часа - только почасовая ставка (BaseFee не добавляется)
                int hours = (int)Math.Ceiling(duration.TotalHours);
                float hourlyCost = hours * plan.HourlyRate;
                cost = Math.Min(hourlyCost, plan.DailyRate);
            }
            else
            {
                // После суток - количество дней × DailyRate
                int days = (int)Math.Ceiling(duration.TotalDays);
                cost = days * plan.DailyRate;
            }

            // Общий лимит - сумма блокировки (HoldAmount)
            cost = Math.Min(cost, plan.HoldAmount);

            return (float)Math.Round(cost, 2);
        }

        private static float GetFloatValue(List<AppSetting> settings, string key, float defaultValue)
        {
            var setting = settings.FirstOrDefault(s => s.Key == key);
            if (setting == null || string.IsNullOrEmpty(setting.Value))
                return defaultValue;

            // Handle both dot and comma as decimal separator
            var value = setting.Value.Replace(',', '.');
            return float.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
        }

        private static int GetIntValue(List<AppSetting> settings, string key, int defaultValue)
        {
            var setting = settings.FirstOrDefault(s => s.Key == key);
            if (setting == null || string.IsNullOrEmpty(setting.Value))
                return defaultValue;

            return int.TryParse(setting.Value, out var result) ? result : defaultValue;
        }

        private static string GetStringValue(List<AppSetting> settings, string key, string defaultValue)
        {
            var setting = settings.FirstOrDefault(s => s.Key == key);
            return string.IsNullOrEmpty(setting?.Value) ? defaultValue : setting.Value;
        }
    }
}
