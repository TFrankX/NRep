using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Identity;

namespace WebServer.Models.Identity
{

    public class AppUsers: List<AppUser>
    {
        public List<AppUser> Users { get; set; }


        public AppUsers()
        {
            Users = new List<AppUser>();
        }

    }


    public class AppUser : IdentityUser
    {
        public List<AppRole> AppRoles;
    }

}
