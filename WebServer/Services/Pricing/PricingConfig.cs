namespace WebServer.Services.Pricing
{
    /// <summary>
    /// Configuration for a single payment plan (PayByCard, PayByCard2, etc.)
    /// </summary>
    public class PricingPlan
    {
        /// <summary>
        /// Amount to hold/block when user takes a powerbank (e.g., 25 EUR)
        /// </summary>
        public float HoldAmount { get; set; } = 25.0f;

        /// <summary>
        /// Base/flat fee charged for any rental (e.g., 2 EUR)
        /// </summary>
        public float BaseFee { get; set; } = 2.0f;

        /// <summary>
        /// Hourly rate for the first day (e.g., 2 EUR per hour)
        /// </summary>
        public float HourlyRate { get; set; } = 2.0f;

        /// <summary>
        /// Daily rate starting from the second day (e.g., 12 EUR per day)
        /// </summary>
        public float DailyRate { get; set; } = 12.0f;

        /// <summary>
        /// Maximum days before the full hold amount is captured (e.g., 3 days)
        /// </summary>
        public int MaxDaysBeforeCapture { get; set; } = 3;

        /// <summary>
        /// Currency code (e.g., "eur", "usd")
        /// </summary>
        public string Currency { get; set; } = "eur";
    }

    /// <summary>
    /// Root configuration containing all pricing plans
    /// </summary>
    public class PricingConfig
    {
        public PricingPlan PayByCard { get; set; } = new PricingPlan();
        public PricingPlan PayByCard2 { get; set; } = new PricingPlan();
        public PricingPlan PayByCard3 { get; set; } = new PricingPlan();
        public PricingPlan PayByCard4 { get; set; } = new PricingPlan();
    }
}
