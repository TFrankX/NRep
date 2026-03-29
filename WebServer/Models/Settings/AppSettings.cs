using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.Settings
{
    /// <summary>
    /// Application settings stored in database
    /// Key-value storage with category grouping for easy extension
    /// </summary>
    public class AppSetting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Category of the setting (e.g., "Pricing", "Notifications", "System")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Unique key within category (e.g., "PayByCard.HoldAmount")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Value stored as string (JSON for complex types)
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Data type hint for UI rendering (string, int, float, bool, json)
        /// </summary>
        [MaxLength(20)]
        public string ValueType { get; set; } = "string";

        /// <summary>
        /// Human-readable description for UI
        /// </summary>
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Display order within category
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Last modification time
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who last modified the setting
        /// </summary>
        [MaxLength(100)]
        public string ModifiedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// View model for pricing plan settings
    /// </summary>
    public class PricingPlanSettings
    {
        public string PlanName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public float HoldAmount { get; set; } = 25.0f;
        public float BaseFee { get; set; } = 2.0f;
        public float HourlyRate { get; set; } = 2.0f;
        public float DailyRate { get; set; } = 12.0f;
        public int MaxDaysBeforeCapture { get; set; } = 3;
        public string Currency { get; set; } = "eur";
    }

    /// <summary>
    /// View model for settings page
    /// </summary>
    public class SettingsViewModel
    {
        public List<PricingPlanSettings> PricingPlans { get; set; } = new();
        public string ActiveSection { get; set; } = "pricing";
    }
}
