using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using WebServer.Models.Identity;

namespace WebServer.Controllers.Identity
{
    public class RoleInitializer
    {
        public static async Task InitializeAsync(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, string defAdminPass)
        {
            if (string.IsNullOrEmpty(defAdminPass))
                throw new ArgumentNullException("Default Admin Password is null or empty");

            var adminName = "Admin";

            if (await roleManager.FindByNameAsync("admin") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("admin"));
            }

            if (await roleManager.FindByNameAsync("manager") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("manager"));
            }

            if (await roleManager.FindByNameAsync("viewer") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("viewer"));
            }

            if (await roleManager.FindByNameAsync("guest") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("guest"));
            }

            if (await roleManager.FindByNameAsync("support") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("support"));
            }

            if (await userManager.FindByNameAsync(adminName) == null)
            {
                var admin = new AppUser {UserName = adminName };
                var result = await userManager.CreateAsync(admin, defAdminPass);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "admin");
                }
            }
        }
    }
}