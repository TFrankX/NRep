using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.Identity
{
    public class AppAccountDataEdit
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Please enter user name")]
        public string UserName { get; set; }
    }

}