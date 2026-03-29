using Microsoft.Extensions.Configuration;
using WebServer.Models.Device;

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
    }

    public class PricingService : IPricingService
    {
        private readonly PricingConfig _config;

        public PricingService(IConfiguration configuration)
        {
            _config = new PricingConfig();
            configuration.GetSection("Pricing").Bind(_config);
        }

        public PricingPlan GetPlan(TypeOfUse typeOfUse)
        {
            return typeOfUse switch
            {
                TypeOfUse.PayByCard => _config.PayByCard,
                TypeOfUse.PayByCard2 => _config.PayByCard2,
                TypeOfUse.PayByCard3 => _config.PayByCard3,
                TypeOfUse.PayByCard4 => _config.PayByCard4,
                _ => _config.PayByCard // Default fallback
            };
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
            var plan = GetPlan(typeOfUse);
            var duration = DateTime.Now - rentalStartTime;

            // If returned within 1 minute, no charge
            if (duration.TotalMinutes < 1)
            {
                return 0;
            }

            float cost;

            if (duration.Days == 0)
            {
                // First day: base fee + hourly rate
                cost = plan.BaseFee + (float)Math.Ceiling(duration.TotalHours) * plan.HourlyRate;
            }
            else
            {
                // Subsequent days: first day max + daily rate for additional days
                float firstDayCost = plan.BaseFee + 24 * plan.HourlyRate;
                float additionalDaysCost = duration.Days * plan.DailyRate;
                cost = firstDayCost + additionalDaysCost;
            }

            // Cap at hold amount (max charge)
            cost = Math.Min(cost, plan.HoldAmount);

            // If powerbank not taken (still held), return full daily rate as estimate
            if (!powerBankTaken)
            {
                return plan.DailyRate;
            }

            return (float)Math.Round(cost, 2);
        }
    }
}
