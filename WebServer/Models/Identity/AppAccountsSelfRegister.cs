using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.Identity
{
    public class AppAccountsSelfRegister
    {
        //[Required(ErrorMessage = "Please enter login")]
        //public string UserName { get; set; }

        //[Required(ErrorMessage = "Please enter password")]
        //[DataType(DataType.Password)]
        //public string Password { get; set; }

        //[Required(ErrorMessage = "Please enter confirm password")]
        //[Compare("Password", ErrorMessage = "Passwords don't match")]
        //[DataType(DataType.Password)]
        //[NotMapped]
        //public string PasswordConfirm { get; set; }


        //public string UserName { get; set; }
        //[Required(ErrorMessage = "Phone Number is a required field")]

        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^((357|\+357)[\- ]?)?(\(?\d{3}\)?[\- ]?)?\d{3}\)?[\- ]?\d{2}$", ErrorMessage = "Not a valid phone number")]        
        //TODO: Make it unique
        public string PhoneNumber { get; set; }

        public string Message { get; set; }

        public bool CodeReq { get; set; }

        public string SMSCode { get; set; }

        public ulong NewStationId { get; set; }
        public string? ReturnUrl { get; set; }

        //[NotMapped]
        //public string SMSCodeConfirm { get; set; }                   
    }
}
