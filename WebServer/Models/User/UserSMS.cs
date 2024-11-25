using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.Identity
{
    public class UserSMS
    {

        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^((357|\+357)[\- ]?)?(\(?\d{3}\)?[\- ]?)?\d{3}\)?[\- ]?\d{2}$", ErrorMessage = "Not a valid phone number")]
        //TODO: Make it unique
        public string PhoneNumber { get; set; }

        public string Message { get; set; }

        public bool CodeReq { get; set; }

        public string SMSCode { get; set; }

        public ulong StationId { get; set; }
        public string? ReturnUrl { get; set; }
    }

}