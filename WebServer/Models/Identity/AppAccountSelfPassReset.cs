using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.Identity
{
    public class AppAccountSelfPassReset
    {
        public string Id { get; set; }

        public string UserName { get; set; }

        public string PhoneNumber { get; set; }

        [DataType(DataType.Password)]   
        public string NewPassword { get; set; }

        public string Message { get; set; }

        public bool CodeReq { get; set; }

        public string SMSCode { get; set; }
    }

}