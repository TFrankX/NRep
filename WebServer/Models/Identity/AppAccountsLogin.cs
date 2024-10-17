using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.Identity
{
    public class AppAccountsLogin
    {
        [Required(ErrorMessage = "Please enter login")]
        public string UserName { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember?")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
        public ulong NewStationId { get; set; }
    }
}
