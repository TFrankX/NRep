using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.Identity;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using WebServer.Workers;
using System.Security.Claims;
using System;
using WebServer.Services.Sms;

namespace WebServer.Controllers.Identity
{
    public class AppAccountController : Controller
    {
        private readonly ILogger<AppAccountController> _logger;
        private readonly UserManager<AppUser> userManag;
        private readonly SignInManager<AppUser> signInManag;
        private readonly ScanDevices scanDevices;
        private readonly ISmsService _smsService;

        public AppAccountController(UserManager<AppUser> userManager, ScanDevices scanDevices, SignInManager<AppUser> signInManager, ISmsService smsService, ILogger<AppAccountController> logger)
        {
            _logger = logger;
            userManag = userManager;
            signInManag = signInManager;
            this.scanDevices = scanDevices;
            _smsService = smsService;
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
        public IActionResult AppAccountLogin(string? returnUrl = null, ulong newStationId = 0)
        {

            var currentUserID = "";
            ClaimsPrincipal currentUser = this.User;
            if (currentUser.Identity.Name != null)
            {
                currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
            else
            {
                currentUserID = "";
            }



            if (newStationId > 0)
            {
                Models.Device.Device matches;
                try
                {
                    matches = scanDevices.DevicesData.Devices.Where(p => p.Id == newStationId).FirstOrDefault();
                }
                catch
                {
                    matches = null;
                }
                //find = scanDevices.DevicesData.Devices.Contains(find);
                if (matches != null)
                {

                    if (matches.Owners == "")
                    {
                        if (currentUserID != "")
                        {
                            matches.Owners = currentUserID;
                            return RedirectToAction("Devices", "Devices");
                        }
                        else
                        {
                            //AppAccountsLogin Model = new AppAccountsLogin { ReturnUrl=null, NewStationId = Id };
                            //return RedirectToAction("AppAccountLogin", "AppAccount", new { newStationId = Id });
                            return View(new AppAccountsLogin { ReturnUrl = returnUrl, NewStationId = newStationId });
                        }
                    }

                    if (matches.Owners == currentUserID)
                    {
                        return RedirectToAction("Devices", "Devices");
                    }

                    

                }
                else
                {
                    return View(new AppAccountsLogin { ReturnUrl = returnUrl, NewStationId = 0 });
                }




               // return RedirectToAction("AppAccountLogin", "AppAccount");
            }

            return View(new AppAccountsLogin { ReturnUrl = returnUrl, NewStationId= newStationId });
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult AppAccountLoginSMS(string? returnUrl = null, ulong newStationId = 0)
        {

            var currentUserID = "";
            ClaimsPrincipal currentUser = this.User;
            if (currentUser.Identity.Name != null)
            {
                currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
            else
            {
                currentUserID = "";
            }

            if (newStationId > 0)
            {
                Models.Device.Device matches;
                try
                {
                    matches = scanDevices.DevicesData.Devices.Where(p => p.Id == newStationId).FirstOrDefault();
                }
                catch
                {
                    matches = null;
                }
                //find = scanDevices.DevicesData.Devices.Contains(find);
                if (matches != null)
                {

                    if (matches.Owners == "")
                    {
                        if (currentUserID != "")
                        {
                            matches.Owners = currentUserID;
                            return RedirectToAction("Devices", "Devices");
                        }
                        else
                        {
                            //AppAccountsLogin Model = new AppAccountsLogin { ReturnUrl=null, NewStationId = Id };
                            //return RedirectToAction("AppAccountLogin", "AppAccount", new { newStationId = Id });
                            return View(new AppAccountsLoginSMS { ReturnUrl = returnUrl, NewStationId = newStationId });
                        }
                    }

                    if (matches.Owners == currentUserID)
                    {
                        return RedirectToAction("Devices", "Devices");
                    }



                }
                else
                {
                    return View(new AppAccountsLoginSMS { ReturnUrl = returnUrl, NewStationId = 0 });
                }

            }
            return View(new AppAccountsLoginSMS { ReturnUrl = returnUrl, NewStationId = newStationId });
        }


        [HttpPost("{phoneNumber}")]
        [Route("SendCodeLogin")]
        [AllowAnonymous]
        public async Task<ActionResult> SendCodeLogin(AppAccountsLoginSMS model)
        {
            //     System.Web.HttpContext.Current.Session["Id"] = 1;

            //var user = userManag.Users.Where(x => x.PhoneNumber == model.PhoneNumber).First();
            //var user = await userManag.FindByNameAsync(model.PhoneNumber);

            WebServer.Models.Identity.AppUser user;

            if (!_smsService.IsValidPhoneNumber(model.PhoneNumber))
            {
                ModelState.AddModelError("", "Wrong phone number format");
                return View("AppAccountLoginSMS", model);
            }

            try
            {
                user = userManag.Users.Where(x => x.PhoneNumber == model.PhoneNumber).First();
            }
            catch
            {
                user = null;
            }

            if (user != null)
            {
                try
                {
                    string cd = _smsService.GenerateCode();
                    TempData["cd"] = cd;

                    var sent = await _smsService.SendCodeAsync(model.PhoneNumber, "takecharger", cd);
                    if (!sent)
                    {
                        ModelState.AddModelError("", "Problem with sms-gate");
                        return View("AppAccountLoginSMS", model);
                    }
                    return View("AppLogCheckPhoneNumber", model);
                }
                catch
                {
                    ModelState.AddModelError("", "Problem with sms-gate");
                    return View("AppAccountLoginSMS", model);
                }


                
               // return View("AppLogCheckPhoneNumber", model);
            }
            else
            {
                ModelState.AddModelError("", $"User with phone {model.PhoneNumber} not found");
                return View("AppAccountLoginSMS", model);
            }
        }     

        //[HttpPost("{phoneNumber}")]
        //[Route("SendCode")]
        //public async Task<ActionResult> SendCode(string phoneNumber)
        //{
        //    var res = new
        //    {
        //        result = "ok",
        //        phone = phoneNumber
        //    };
        //    //return infoParser.BankruptInfo(taxId);
        //    //var json= Json(res, new JsonSerializerOptions { PropertyNamingPolicy = null });
        //    return Json(res, new JsonSerializerOptions { PropertyNamingPolicy = null });
        //}






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
                    _logger.LogInformation("User {UserName} logged in successfully", model.UserName);

                    var currentUserID = "";
                    ClaimsPrincipal currentUser = this.User;
                    if (currentUser.Identity.Name != null)
                    {
                        currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
                    }
                    else
                    {
                        currentUserID = "";
                    }


                    Models.Device.Device matches;
                    try
                    {
                        matches = scanDevices.DevicesData.Devices.Where(p => p.Id == model.NewStationId).FirstOrDefault();
                    }
                    catch
                    {
                        matches = null;
                    }
                    if (matches != null)
                    {
                        if (matches.Owners != "")
                        {
                            matches.Owners = model.UserName;
                        }

                    }
                    

                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Devices", "Devices");
                    }
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for user {UserName}", model.UserName);
                    ModelState.AddModelError("", "Incorrect login and/or password");
                }
            }
            return View(model);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppAccountLoginSMS(AppAccountsLoginSMS model)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            if (ModelState.IsValid)
            {


                WebServer.Models.Identity.AppUser user;

                try
                {
                    user = userManag.Users.Where(x => x.PhoneNumber == model.PhoneNumber).First();
                }
                catch
                {
                    user = null;
                }

                if ((user != null) && (model.SMSCode == TempData["cd"].ToString().Trim()))
                {

                    await signInManag.SignInAsync(user, true);



                    var currentUserID = "";
                    ClaimsPrincipal currentUser = this.User;
                    if (currentUser.Identity.Name != null)
                    {
                        currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
                    }
                    else
                    {
                        currentUserID = "";
                    }


                    Models.Device.Device matches;
                    try
                    {
                        matches = scanDevices.DevicesData.Devices.Where(p => p.Id == model.NewStationId).FirstOrDefault();
                    }
                    catch
                    {
                        matches = null;
                    }
                    if (matches != null)
                    {
                        if (matches.Owners != "")
                        {
                            matches.Owners = currentUserID;
                        }

                    }



                    return RedirectToAction("Devices", "Devices");
                }
                else
                {
                    ModelState.AddModelError("", "Incorrect SMS code");
                    return View("AppLogCheckPhoneNumber", model);
                }
                 
                // return RedirectToAction("Servers", "Servers");

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> CheckLoginSMSCodeAjax(AppAccountsLoginSMS model)
        {
            try
            {
                // Check if code exists in TempData
                var savedCode = TempData.Peek("cd")?.ToString()?.Trim();
                if (string.IsNullOrEmpty(savedCode))
                {
                    return Json(new { success = false, expired = true, message = "Code expired. Please request a new one." });
                }

                // Find user by phone
                var user = userManag.Users.FirstOrDefault(x => x.PhoneNumber == model.PhoneNumber);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Verify code
                if (model.SMSCode != savedCode)
                {
                    return Json(new { success = false, message = "Incorrect SMS code" });
                }

                // Code is correct - sign in user
                await signInManag.SignInAsync(user, true);

                // Handle new station assignment if provided
                if (model.NewStationId > 0)
                {
                    var device = scanDevices.DevicesData.Devices.FirstOrDefault(d => d.Id == model.NewStationId);
                    if (device != null && string.IsNullOrEmpty(device.Owners))
                    {
                        device.Owners = user.Id;
                    }
                }

                // Clear the code from TempData
                TempData.Remove("cd");

                var redirectUrl = !string.IsNullOrEmpty(model.ReturnUrl) ? model.ReturnUrl : Url.Action("Devices", "Devices");
                return Json(new { success = true, redirectUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckLoginSMSCodeAjax error");
                return Json(new { success = false, message = "An error occurred" });
            }
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
            var userName = User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserName} logged out", userName);
            await signInManag.SignOutAsync().ConfigureAwait(false);
            return RedirectToAction("AppAccountLogin", "AppAccount");
        }

        //private string tunePhoneNumber(string phoneNumber)
        //{
        //    string num = phoneNumber.Trim();
        //    num=Regex.Replace(num, "(?i)[ ()+_-]", "");
        //    if (num.Length == 8)
        //    {
        //        num = $"357{num}";
        //    }
        //    return num;
        //}
        //private string Gen4Code()
        //{
        //        int _min = 10000;
        //        int _max = 99999;
        //        Random _rdm = new Random();
        //        return _rdm.Next(_min, _max).ToString();
            
        //}
    }
}