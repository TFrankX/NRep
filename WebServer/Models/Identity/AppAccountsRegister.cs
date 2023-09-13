using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.Identity
{
    public class AppAccountsRegister
    {
        [Required(ErrorMessage = "Please enter login")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Please enter password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please enter confirm password")]
        [Compare("Password", ErrorMessage = "Passwords don't match")]
        [DataType(DataType.Password)]
        [NotMapped]
        public string PasswordConfirm { get; set; }
    }
}
