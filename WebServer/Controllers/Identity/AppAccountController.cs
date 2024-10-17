using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.Identity;

namespace WebServer.Controllers.Identity
{
    public class AppAccountController : Controller
    {
        private readonly UserManager<AppUser> userManag;
        private readonly SignInManager<AppUser> signInManag;

        public AppAccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            userManag = userManager;
            signInManag = signInManager;
        }
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult AppAccountRegister()
        {
            return View();
        }
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AppAccountRegister(AppAccountsRegister model)
        {
            if (ModelState.IsValid)
            {
                AppUser user = new AppUser { UserName = model.UserName};

                var result = await userManag.CreateAsync(user, model.Password).ConfigureAwait(false);
                if (result.Succeeded)
                {
                    await userManag.AddToRoleAsync(user, "guest").ConfigureAwait(false);
                    await signInManag.SignInAsync(user, false).ConfigureAwait(false);
                    return RedirectToAction("Administration", "Administration");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            return View(model);
        }
       
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AppAccountLogin(string? returnUrl = null)
        {
            return View(new AppAccountsLogin { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppAccountLogin(AppAccountsLogin model)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            if (ModelState.IsValid)
            {
                var result =
                    await signInManag.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, false).ConfigureAwait(false);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else
                    {
                        //return this.RedirectToAction<ServersController>(m => m.Servers());
                        return RedirectToAction("Servers", "Servers");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Incorrect login and/or password");
                }
            }
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            ViewBag.Referer = Request.Headers["Referer"].ToString();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await signInManag.SignOutAsync().ConfigureAwait(false);
            return RedirectToAction("AppAccountLogin", "AppAccount");
        }
    }
}