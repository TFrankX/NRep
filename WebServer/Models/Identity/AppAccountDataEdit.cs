using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.Identity
{
    public class AppAccountDataEdit
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Please enter user name")]
        public string UserName { get; set; }

        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Phone Number")]
        //[RegularExpression(@"^((357|\+357)[\- ]?)?(\(?\d{3}\)?[\- ]?)?\d{3}\)?[\- ]?\d{2}$", ErrorMessage = "Not a valid phone number")]
        //TODO: Make it unique
        public string PhoneNumber { get; set; }
    }

}