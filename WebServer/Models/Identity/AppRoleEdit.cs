using System.Collections.Generic;

namespace WebServer.Models.Identity
{
    
    public class AppRoles
    {
        public List<AppRole> Roles { get; set; }

        public AppRoles()
        {
            Roles = new List<AppRole>();
        }
    }



    public class AppRole
    {
        public string RoleName { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string RoleId { get; set; }
        public bool IsInRole { get; set; }

        
    }


    public class AppRoleEdit : List<AppRoleEdit>
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public bool IsInRole { get; set; }
    }

}
