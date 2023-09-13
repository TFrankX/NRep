using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.Identity;
using System.Collections.Generic;

namespace WebServer.Controllers.Identity
{
    [Authorize(Roles = "admin")]
    public class AppAccountManagementController : Controller
    {
        UserManager<AppUser> userManag;
        RoleManager<IdentityRole> roleManag;

        public AppAccountManagementController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            userManag = userManager;
            roleManag = roleManager;

        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AppAccounts()
        {
            var roles = roleManag.Roles.ToList();
//            var model = userManag.Users.ToList();
            var model = new AppUsers();

            foreach (AppUser user in userManag.Users)
            {
                user.AppRoles = new List<AppRole>();
                model.Add(user);
                foreach (IdentityRole role in roles)
                {
                    var list = await userManag.IsInRoleAsync(user, role.Name);
                    model[model.Count-1].AppRoles.Add(new AppRole { RoleName = role.Name, RoleId = role.Id, UserId = user.Id, UserName = user.UserName, IsInRole = list });
                }

            }
            return View(model);
        }
            

        [Authorize(Roles = "admin")]
        public IActionResult AppCreateAccount() => View();

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AppCreateAccount(AppAccountsRegister model)
        {
            if (ModelState.IsValid)
            {
                var user = new AppUser {UserName = model.UserName};

                var result = await userManag.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    return RedirectToAction("AppAccounts");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AppEditAccountData(string id)
        {
            var user = await userManag.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var model = new AppAccountDataEdit {Id = user.Id, UserName = user.UserName};

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AppEditAccountData(AppAccountDataEdit model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManag.FindByIdAsync(model.Id);

                if (user != null)
                {
                    user.UserName = model.UserName;

                    var result = await userManag.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        return RedirectToAction("AppAccounts");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Delete(string id)
        {
            var user = await userManag.FindByIdAsync(id);

            if (user != null)
            {
                await userManag.DeleteAsync(user);
            }

            return RedirectToAction("AppAccounts");
        }
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AppEditAccountPass(string id)
        {
            var user = await userManag.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var model = new AppAccountPassEdit {Id = user.Id, UserName = user.UserName};

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AppEditAccountPass(AppAccountPassEdit model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManag.FindByIdAsync(model.Id);

                if (user != null)
                {
                    var result =
                        await userManag.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("AppAccounts");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Пользователь не найден");
                }
            }

            return View(model);
        }
    }
}
