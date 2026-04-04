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
    /// View model for support contact settings
    /// </summary>
    public class SupportSettings
    {
        public string Phone { get; set; } = "+357 99 123 456";
        public string Email { get; set; } = "support@a-charger.com";
        public string WorkingHours { get; set; } = "24/7";
    }

    /// <summary>
    /// View model for MQTT server configuration
    /// </summary>
    public class ServerConfigSettings
    {
        public int Index { get; set; } = 0;  // 0-4
        public string Address { get; set; } = string.Empty;
        public int Port { get; set; } = 8884;
        public string User { get; set; } = string.Empty;
        public string Pass { get; set; } = string.Empty;
        public int ReconnectTime { get; set; } = 30;
        public string CertCA { get; set; } = string.Empty;
        public string CertCli { get; set; } = string.Empty;
        public string CertPass { get; set; } = string.Empty;
        public bool Enabled { get; set; } = false;
    }

    /// <summary>
    /// View model for scan/polling settings
    /// </summary>
    public class ScanSettings
    {
        /// <summary>
        /// Interval between inventory requests to each station (seconds)
        /// </summary>
        public int InventoryPeriodSeconds { get; set; } = 300;

        /// <summary>
        /// Number of retry attempts before marking station offline
        /// </summary>
        public int OfflineRetryCount { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts (seconds)
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 5;

        /// <summary>
        /// Timeout waiting for station response (seconds)
        /// </summary>
        public int ResponseTimeoutSeconds { get; set; } = 10;
    }

    /// <summary>
    /// View model for zone settings
    /// </summary>
    public class ZoneSettings
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#7C3AED";
        public string Language { get; set; } = "en";
    }

    /// <summary>
    /// Supported languages for zones
    /// </summary>
    public static class SupportedLanguages
    {
        public static readonly Dictionary<string, string> All = new()
        {
            { "en", "English" },
            { "ru", "Русский" },
            { "es", "Español" },
            { "de", "Deutsch" },
            { "fr", "Français" },
            { "el", "Ελληνικά" }
        };
    }

    /// <summary>
    /// User page translations
    /// </summary>
    public static class Translations
    {
        public static string Get(string lang, string key)
        {
            if (Strings.TryGetValue(lang, out var langDict) && langDict.TryGetValue(key, out var value))
                return value;
            // Fallback to English
            if (Strings["en"].TryGetValue(key, out var fallback))
                return fallback;
            return key;
        }

        private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
        {
            { "en", new Dictionary<string, string>
                {
                    { "PowerbankActive", "Powerbank Active" },
                    { "EnjoyYourCharge", "Enjoy your charge!" },
                    { "PowerbankReturned", "Powerbank Returned!" },
                    { "ThankYou", "Thank you for using A-Charger" },
                    { "AllReturned", "All Powerbanks Returned!" },
                    { "NoPowerbanks", "No Powerbanks Available" },
                    { "TryAgainLater", "Please try again later or visit another station." },
                    { "NeedHelp", "Need help?" },
                    { "PBank", "P-BANK" },
                    { "Taken", "TAKEN" },
                    { "Time", "TIME" },
                    { "Cost", "COST" },
                    { "Total", "Total:" },
                    { "TakeAnother", "Take Another Powerbank" },
                    { "Dispensing", "Dispensing..." }
                }
            },
            { "ru", new Dictionary<string, string>
                {
                    { "PowerbankActive", "Повербанк активен" },
                    { "EnjoyYourCharge", "Приятного пользования!" },
                    { "PowerbankReturned", "Повербанк возвращён!" },
                    { "ThankYou", "Спасибо за использование A-Charger" },
                    { "AllReturned", "Все повербанки возвращены!" },
                    { "NoPowerbanks", "Нет доступных повербанков" },
                    { "TryAgainLater", "Пожалуйста, попробуйте позже или посетите другую станцию." },
                    { "NeedHelp", "Нужна помощь?" },
                    { "PBank", "П-БАНК" },
                    { "Taken", "ВЗЯТ" },
                    { "Time", "ВРЕМЯ" },
                    { "Cost", "ЦЕНА" },
                    { "Total", "Итого:" },
                    { "TakeAnother", "Взять ещё повербанк" },
                    { "Dispensing", "Выдаём..." }
                }
            },
            { "es", new Dictionary<string, string>
                {
                    { "PowerbankActive", "Powerbank Activo" },
                    { "EnjoyYourCharge", "¡Disfruta de tu carga!" },
                    { "PowerbankReturned", "¡Powerbank Devuelto!" },
                    { "ThankYou", "Gracias por usar A-Charger" },
                    { "AllReturned", "¡Todos los Powerbanks Devueltos!" },
                    { "NoPowerbanks", "No Hay Powerbanks Disponibles" },
                    { "TryAgainLater", "Por favor, inténtalo más tarde o visita otra estación." },
                    { "NeedHelp", "¿Necesitas ayuda?" },
                    { "PBank", "P-BANK" },
                    { "Taken", "TOMADO" },
                    { "Time", "TIEMPO" },
                    { "Cost", "COSTO" },
                    { "Total", "Total:" },
                    { "TakeAnother", "Tomar Otro Powerbank" },
                    { "Dispensing", "Dispensando..." }
                }
            },
            { "de", new Dictionary<string, string>
                {
                    { "PowerbankActive", "Powerbank Aktiv" },
                    { "EnjoyYourCharge", "Viel Spaß beim Laden!" },
                    { "PowerbankReturned", "Powerbank Zurückgegeben!" },
                    { "ThankYou", "Danke für die Nutzung von A-Charger" },
                    { "AllReturned", "Alle Powerbanks Zurückgegeben!" },
                    { "NoPowerbanks", "Keine Powerbanks Verfügbar" },
                    { "TryAgainLater", "Bitte versuchen Sie es später erneut oder besuchen Sie eine andere Station." },
                    { "NeedHelp", "Brauchen Sie Hilfe?" },
                    { "PBank", "P-BANK" },
                    { "Taken", "GENOMMEN" },
                    { "Time", "ZEIT" },
                    { "Cost", "KOSTEN" },
                    { "Total", "Gesamt:" },
                    { "TakeAnother", "Noch eine Powerbank nehmen" },
                    { "Dispensing", "Ausgabe..." }
                }
            },
            { "fr", new Dictionary<string, string>
                {
                    { "PowerbankActive", "Powerbank Actif" },
                    { "EnjoyYourCharge", "Bonne recharge !" },
                    { "PowerbankReturned", "Powerbank Retourné !" },
                    { "ThankYou", "Merci d'utiliser A-Charger" },
                    { "AllReturned", "Tous les Powerbanks Retournés !" },
                    { "NoPowerbanks", "Pas de Powerbanks Disponibles" },
                    { "TryAgainLater", "Veuillez réessayer plus tard ou visiter une autre station." },
                    { "NeedHelp", "Besoin d'aide ?" },
                    { "PBank", "P-BANK" },
                    { "Taken", "PRIS" },
                    { "Time", "TEMPS" },
                    { "Cost", "COÛT" },
                    { "Total", "Total :" },
                    { "TakeAnother", "Prendre un Autre Powerbank" },
                    { "Dispensing", "Distribution..." }
                }
            },
            { "el", new Dictionary<string, string>
                {
                    { "PowerbankActive", "Powerbank Ενεργό" },
                    { "EnjoyYourCharge", "Καλή φόρτιση!" },
                    { "PowerbankReturned", "Powerbank Επιστράφηκε!" },
                    { "ThankYou", "Ευχαριστούμε που χρησιμοποιείτε το A-Charger" },
                    { "AllReturned", "Όλα τα Powerbanks Επιστράφηκαν!" },
                    { "NoPowerbanks", "Δεν Υπάρχουν Διαθέσιμα Powerbanks" },
                    { "TryAgainLater", "Παρακαλώ δοκιμάστε αργότερα ή επισκεφθείτε άλλο σταθμό." },
                    { "NeedHelp", "Χρειάζεστε βοήθεια;" },
                    { "PBank", "P-BANK" },
                    { "Taken", "ΛΗΦΘΗΚΕ" },
                    { "Time", "ΧΡΟΝΟΣ" },
                    { "Cost", "ΚΟΣΤΟΣ" },
                    { "Total", "Σύνολο:" },
                    { "TakeAnother", "Πάρτε Άλλο Powerbank" },
                    { "Dispensing", "Διανομή..." }
                }
            }
        };
    }

    /// <summary>
    /// View model for settings page
    /// </summary>
    public class SettingsViewModel
    {
        public List<PricingPlanSettings> PricingPlans { get; set; } = new();
        public SupportSettings Support { get; set; } = new();
        public List<ServerConfigSettings> Servers { get; set; } = new();
        public ScanSettings Scan { get; set; } = new();
        public List<ZoneSettings> Zones { get; set; } = new();
        public string ActiveSection { get; set; } = "pricing";
    }
}
