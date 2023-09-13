using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.Identity;


namespace WebServer.Controllers.Identity
{
    public class AppRoleManagementController : Controller
    {
        private readonly UserManager<AppUser> userManag;
        private readonly RoleManager<IdentityRole> roleManag;

        public AppRoleManagementController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            userManag = userManager;
            roleManag = roleManager;
        }

        [Authorize(Roles = "admin")]
        public IActionResult AppRole() => View(roleManag.Roles.ToList());

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ManageUsersRole([FromBody] AppRole roleInfo)
        {
            var role = await roleManag.FindByIdAsync(roleInfo.RoleId);
            var user = await userManag.FindByIdAsync(roleInfo.UserId);
            if (roleInfo.IsInRole)
            {
                await userManag.AddToRoleAsync(user, role.Name);
            }
            else
            {
                await userManag.RemoveFromRoleAsync(user, role.Name);
            }
            //return RedirectToAction("EditRole", "AppRoleManagement", new { roleInfo.RoleId });
            //return View();
            return RedirectToAction("AppAccounts", "AppAccountManagement");
        }
        [Authorize(Roles = "admin")]
        
        
        
        
        /*
        public async Task<IActionResult> DelUsersFromRole(string idUser, string idRole)
        {
            var role = await roleManag.FindByIdAsync(idRole);
            var user = await userManag.FindByIdAsync(idUser);
            await userManag.RemoveFromRoleAsync(user, role.Name);

            return RedirectToAction("EditRole", "AppRoleManagement", new { idRole });
        }
        */
        
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EditRole(string idRole)
        {
            var role = await roleManag.FindByIdAsync(idRole);
            ViewBag.IdRole = role.Id;
            ViewBag.NameRole = role.Name;
            //var model = new List<AppRoleEdit>();
            var model = new AppRoles();

            foreach (AppUser user in userManag.Users)
            {
                var list = await userManag.IsInRoleAsync(user, role.Name);

                model.Roles.Add(new AppRole { UserId = user.Id, UserName = user.UserName, IsInRole = list });
            }

            return View(model);
        }

    }
}