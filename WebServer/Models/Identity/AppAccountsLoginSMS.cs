using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.Identity
{
    public class AppAccountsLoginSMS
    {
        [Required(ErrorMessage = "Please enter phone")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Remember?")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }

        public string SMSCode { get; set; }
        public ulong NewStationId { get; set; }
    }
}
