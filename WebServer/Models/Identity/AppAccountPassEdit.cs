using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.Identity
{
    public class AppAccountPassEdit
    {
        public string Id { get; set; }

        public string UserName { get; set; }

        [DataType(DataType.Password)]   
        public string NewPassword { get; set; }

    }

}