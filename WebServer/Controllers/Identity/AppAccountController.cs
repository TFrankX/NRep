using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.Identity;
using WebServer.Utils.Requests;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using WebServer.Workers;
using System.Security.Claims;
using System;

namespace WebServer.Controllers.Identity
{
    public class AppAccountController : Controller
    {
        private readonly UserManager<AppUser> userManag;
        private readonly SignInManager<AppUser> signInManag;
        private ScanDevices scanDevices;

        public AppAccountController(UserManager<AppUser> userManager, ScanDevices scanDevices, SignInManager<AppUser> signInManager)
        {
            userManag = userManager;
            signInManag = signInManager;
            this.scanDevices = scanDevices;
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

            SMS chSMS = new SMS();
            model.PhoneNumber = chSMS.TunePhoneNumber(model.PhoneNumber);

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
                    SMS sms = new SMS();
                    string cd = sms.Gen4Code();
                    //HttpContext.Session.SetString("cd", cd);
                    TempData["cd"] = cd;
                    var phone = sms.TunePhoneNumber(model.PhoneNumber);
                    if (phone.Length != 11)
                    {
                        ModelState.AddModelError("", $"Wrong phone number format");
                        return View("AppAccountLoginSMS", model);
                    }

                    sms.Send(phone, "takecharger", cd);
                    return View("AppLogCheckPhoneNumber", model);
                }
                catch
                {
                    ModelState.AddModelError("", $"Problem with sms-gate");
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




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppAccountLoginSMS(AppAccountsLoginSMS model)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            if (ModelState.IsValid)
            {


                WebServer.Models.Identity.AppUser user;

                SMS chSMS = new SMS();
                model.PhoneNumber = chSMS.TunePhoneNumber(model.PhoneNumber);

                try
                {
                    user = userManag.Users.Where(x => x.PhoneNumber == model.PhoneNumber).First();
                }
                catch
                {
                    user = null;
                }

                //var user = await userManag.FindByNameAsync(model.PhoneNumber);
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