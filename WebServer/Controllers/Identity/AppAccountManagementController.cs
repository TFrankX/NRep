using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.Identity;
using System.Collections.Generic;
using System.Text.Json;
using System.Security.Claims;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebServer.Workers;
using WebServer.Services.Sms;

namespace WebServer.Controllers.Identity
{
    [Authorize]
    public class AppAccountManagementController : Controller
    {
        private readonly ILogger<AppAccountManagementController> _logger;
        private readonly UserManager<AppUser> userManag;
        private readonly RoleManager<IdentityRole> roleManag;
        private readonly SignInManager<AppUser> signInManag;
        private readonly ScanDevices scanDevices;
        private readonly ISmsService _smsService;

        public AppAccountManagementController(UserManager<AppUser> userManager, ScanDevices scanDevices, RoleManager<IdentityRole> roleManager, SignInManager<AppUser> signInManager, ISmsService smsService, ILogger<AppAccountManagementController> logger)
        {
            _logger = logger;
            userManag = userManager;
            roleManag = roleManager;
            signInManag = signInManager;
            this.scanDevices = scanDevices;
            _smsService = smsService;
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AppAccounts()
        {
            var roles = roleManag.Roles.ToList();
            var users = userManag.Users.ToList();
            var model = new AppUsers();

            // Batch load user roles to avoid N+1 queries
            var userRolesMap = new Dictionary<string, IList<string>>();
            foreach (var user in users)
            {
                userRolesMap[user.Id] = await userManag.GetRolesAsync(user);
            }

            foreach (AppUser user in users)
            {
                user.AppRoles = new List<AppRole>();
                model.Add(user);
                var userRoleNames = userRolesMap[user.Id];
                foreach (IdentityRole role in roles)
                {
                    var isInRole = userRoleNames.Contains(role.Name ?? "");
                    model[model.Count-1].AppRoles.Add(new AppRole { RoleName = role.Name, RoleId = role.Id, UserId = user.Id, UserName = user.UserName, IsInRole = isInRole });
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
                    var resultR = await userManag.AddToRoleAsync(user, "operator");

                    return RedirectToAction("AppAccounts");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        public IActionResult AppCreateSelfAccount(string? returnUrl = null, ulong newStationId = 0)
        {



            //var currentUserID = "";
            //ClaimsPrincipal currentUser = this.User;
            //if (currentUser.Identity.Name != null)
            //{
            //    currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
            //}
            //else
            //{
            //    currentUserID = "";
            //}


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
                        
                        return View(new AppAccountsSelfRegister { NewStationId = newStationId });

                    }
                    
                }
               

                // return RedirectToAction("AppAccountLogin", "AppAccount");
            }

    




            return View(new AppAccountsSelfRegister { NewStationId = 0 });
        } 

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AppCreateSelfAccount(AppAccountsSelfRegister model)
        {

            if (model.SMSCode == TempData["cd"].ToString().Trim())
            {

                if (ModelState.IsValid)
                {

                    var user = new AppUser { UserName = model.PhoneNumber, PhoneNumber = model.PhoneNumber };
                   // model.Password = "Reset777!";
                    var result = await userManag.CreateAsync(user, "Reset777!");

                    if (result.Succeeded)
                    {


                        if (model.NewStationId > 0)
                        {
                            Models.Device.Device matches;
                            try
                            {
                                matches = scanDevices.DevicesData.Devices.Where(p => p.Id == model.NewStationId).FirstOrDefault();
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
                                    if ((user != null) && (user.PhoneNumber != null) && (user.PhoneNumber != ""))
                                    {
                                        matches.Owners = user.UserName;

                                    }
                                    await signInManag.SignInAsync(user, true);
                                    return RedirectToAction("Devices", "Devices");

                                }

                            }
                            else
                            {
                                await signInManag.SignInAsync(user, true);
                                return RedirectToAction("Devices", "Devices");
                            }
                        }





                                //AppAccountsLoginSMS logmod=new AppAccountsLoginSMS();
                                //logmod.PhoneNumber = model.PhoneNumber;
                                //logmod.SMSCode = model.SMSCode;                 
                                //return RedirectToAction("AppAccountLoginSMS","AppAccounts", model);
                        await signInManag.SignInAsync(user, true);
                        return RedirectToAction("Devices", "Devices");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            else
            {
                TempData.Keep("cd");
                ModelState.AddModelError("", "Incorrect SMS code. Please try again.");
                return View("AppCheckPhoneNumber", model);
            }
            return View(model);
        }


        [HttpPost("{phoneNumber}")]
        [Route("SendCodeRegister")]
        [AllowAnonymous]
        public async Task<ActionResult> SendCodeRegister(AppAccountsSelfRegister model)
        {

            if (!_smsService.IsValidPhoneNumber(model.PhoneNumber))
            {
                ModelState.AddModelError("", "Wrong phone number format");
                return View("AppCreateSelfAccount", model);
            }

            WebServer.Models.Identity.AppUser user;

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
                ModelState.AddModelError("", $"User with phone {model.PhoneNumber} exist");
                return View("AppCreateSelfAccount", model);
            }

            try
            {
                string cd = _smsService.GenerateCode();
                TempData["cd"] = cd;

                var sent = await _smsService.SendCodeAsync(model.PhoneNumber, "takecharger", cd);
                if (!sent)
                {
                    ModelState.AddModelError("", "Problem with sms-gate");
                    return View("AppCreateSelfAccount", model);
                }
                ViewBag.ResendCooldown = 180; // 3 minutes cooldown
                return View("AppCheckPhoneNumber", model);
            }
            catch
            {
                ModelState.AddModelError("", "Problem with sms-gate");
                return View("AppCreateSelfAccount", model);
            }


           

   

            //newAccount.PhoneNumber = model.PhoneNumber;

            //return infoParser.BankruptInfo(taxId);
            //var json= Json(res, new JsonSerializerOptions { PropertyNamingPolicy = null });
            //return Json(res, new JsonSerializerOptions { PropertyNamingPolicy = null });
            //return View(model);
            //return View("AppCreateAccount",model);
           // return View("AppCheckPhoneNumber", model);
        }

        //[HttpPost("{phoneNumber}")]
        //[Route("CheckCodeRegister")]
        //[AllowAnonymous]
        //public async Task<ActionResult> CheckCodeRegister(AppAccountsSelfRegister model)
        //{
        //    var code1=TempData["cd"].ToString();
        //    //TempData["cd"] = code1;
        //    //var code1 = HttpContext.Session.GetString("cd");
        //    //var code2 = HttpContext.Session.GetString("cd");
        //    ModelState.AddModelError("", code1);
        //   // ModelState.AddModelError("", code2);
        //    if (model.SMSCode.Trim() == TempData["cd"].ToString().Trim())
        //    {
        //        var res = new
        //        {
        //            result = "ok",
        //            phone = model.PhoneNumber
        //        };
        //        // model.Message = "hhh";
        //        //return infoParser.BankruptInfo(taxId);
        //        //var json= Json(res, new JsonSerializerOptions { PropertyNamingPolicy = null });
        //        //return Json(res, new JsonSerializerOptions { PropertyNamingPolicy = null });
        //        //return View(model);
        //        //return View("AppCreateAccount",model);
        //        model.UserName = model.PhoneNumber;
        //        model.Password = "Reset777";
        //        model.PasswordConfirm = "Reset777";
        //        //newAccount.SMSCode = model.SMSCode;
        //        // return RedirectToAction( "AppCreateAccount", "AppAccountManagement",model);
        //        return RedirectToActionPreserveMethod("AppCreateSelfAccount", "AppAccountManagement", model);
        //    }
        //    else
        //    {
        //        TempData.Keep("cd");
        //        ModelState.AddModelError("", "Incorrect SMS code");
        //        return View("AppCheckPhoneNumber", model);
        //    }
        //}


        [HttpPost("{phoneNumber}")]
        [Route("SendCodePassReset")]
        [Authorize]
        public async Task<ActionResult> SendCodePassReset(AppAccountSelfPassReset model)
        {

            if (!_smsService.IsValidPhoneNumber(model.PhoneNumber))
            {
                ModelState.AddModelError("", "Wrong phone number format");
                return View("AppResetSelfAccountPass", model);
            }

            try
            {
                string cd = _smsService.GenerateCode();
                TempData["cd"] = cd;

                var sent = await _smsService.SendCodeAsync(model.PhoneNumber, "takecharger", cd);
                if (!sent)
                {
                    ModelState.AddModelError("", "Problem with sms-gate");
                    return View("AppResetSelfAccountPass", model);
                }
                return View("AppResetPassCheckPhoneNumber", model);
            }
            catch
            {
                ModelState.AddModelError("", "Problem with sms-gate");
                return View("AppResetSelfAccountPass", model);
            }
        }

        [HttpPost("{phoneNumber}")]
        [Route("CheckCodePassReset")]
        [Authorize]
        public async Task<ActionResult> CheckCodePassReset(AppAccountSelfPassReset model)
        {



            if ((ModelState.IsValid) && (model.SMSCode == TempData["cd"].ToString().Trim()))
       
            {
                var user = await userManag.FindByIdAsync(model.Id);

                if (user != null)
                {
                    var token = await userManag.GeneratePasswordResetTokenAsync(user);
                    //var result =
                    //    await userManag.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    var result = await userManag.ResetPasswordAsync(user, token, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("AppEditSelfAccountData");
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
            else
            {
                ModelState.AddModelError("", "Incorrect SMS code");
                return View("AppResetPassCheckPhoneNumber", model);
            }

            return RedirectToActionPreserveMethod("AppEditSelfAccountData", "AppAccountManagement", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult VerifyResetCodeAjax(AppAccountSelfPassReset model)
        {
            try
            {
                var savedCode = TempData.Peek("cd")?.ToString()?.Trim();
                if (string.IsNullOrEmpty(savedCode))
                {
                    return Json(new { success = false, expired = true, message = "Code expired. Please request a new one." });
                }

                if (model.SMSCode != savedCode)
                {
                    return Json(new { success = false, message = "Incorrect SMS code" });
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ResetPasswordAjax(AppAccountSelfPassReset model)
        {
            try
            {
                var savedCode = TempData.Peek("cd")?.ToString()?.Trim();
                if (string.IsNullOrEmpty(savedCode))
                {
                    return Json(new { success = false, message = "Code expired. Please request a new one." });
                }

                if (model.SMSCode != savedCode)
                {
                    return Json(new { success = false, message = "Incorrect SMS code" });
                }

                var user = await userManag.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var token = await userManag.GeneratePasswordResetTokenAsync(user);
                var result = await userManag.ResetPasswordAsync(user, token, model.NewPassword);

                if (result.Succeeded)
                {
                    TempData.Remove("cd");
                    return Json(new { success = true, redirectUrl = Url.Action("AppEditSelfAccountData", "AppAccountManagement") });
                }

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Json(new { success = false, message = errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred" });
            }
        }


        [Authorize]
        public async Task<IActionResult> AppEditAccountData(string id)
        {
            var user = await userManag.FindByIdAsync(id);
           
            if (user == null)
            {
                return NotFound();
            }

            var model = new AppAccountDataEdit {Id = user.Id, UserName = user.UserName, PhoneNumber = user.PhoneNumber};

            return View(model);
        }

        [Authorize]
        //[AllowAnonymous]
        public async Task<IActionResult> AppEditSelfAccountData()
        {
            ClaimsPrincipal currentUser = this.User;
            var currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;

            var user = await userManag.FindByIdAsync(currentUserID);

            if (user == null)
            {
                return NotFound();
            }

            var model = new AppAccountDataEdit { Id = user.Id, UserName = user.UserName, PhoneNumber=user.PhoneNumber };

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
                    user.PhoneNumber = model.PhoneNumber;
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
        [Authorize]
        public async Task<IActionResult> AppEditSelfAccountData(AppAccountDataEdit model)
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
                        return RedirectToAction("Devices","Devices");
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

        [Authorize]
        public async Task<IActionResult> AppEditSelfAccountPass(string id)
        {
            ClaimsPrincipal currentUser = this.User;
            var currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;

            var user = await userManag.FindByIdAsync(currentUserID);

            if (user == null)
            {
                return NotFound();
            }

            var model = new AppAccountPassEdit { Id = user.Id, UserName = user.UserName };

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
                    var token = await userManag.GeneratePasswordResetTokenAsync(user);
                    //var result =
                    //    await userManag.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    var result = await userManag.ResetPasswordAsync (user,token, model.NewPassword);
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

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AppEditSelfAccountPass(AppAccountSelfPassEdit model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManag.FindByIdAsync(model.Id);

                if (user != null)
                {
                    var result = await userManag.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("AppEditSelfAccountData");
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

        [Authorize]
        public async Task<IActionResult> AppResetSelfAccountPass()
        {
            ClaimsPrincipal currentUser = this.User;
            var currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;

            var user = await userManag.FindByIdAsync(currentUserID);

            if (user == null)
            {
                return NotFound();
            }

            var model = new AppAccountSelfPassReset { Id = user.Id, UserName = user.UserName, PhoneNumber = user.PhoneNumber };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccountAjax(AppAccountsSelfRegister model)
        {
            try
            {
                // Check if code exists
                if (TempData["cd"] == null)
                {
                    return Json(new { success = false, expired = true, message = "SMS code expired. Please request a new code." });
                }

                var savedCode = TempData["cd"].ToString().Trim();

                if (model.SMSCode?.Trim() != savedCode)
                {
                    TempData.Keep("cd");
                    return Json(new { success = false, message = "Incorrect SMS code" });
                }

                // Create user
                var user = new AppUser { UserName = model.PhoneNumber, PhoneNumber = model.PhoneNumber };
                var result = await userManag.CreateAsync(user, "Reset777!");

                if (result.Succeeded)
                {
                    // Handle station registration if needed
                    if (model.NewStationId > 0)
                    {
                        var matches = scanDevices.DevicesData.Devices.FirstOrDefault(p => p.Id == model.NewStationId);
                        if (matches != null && string.IsNullOrEmpty(matches.Owners))
                        {
                            matches.Owners = user.UserName;
                        }
                    }

                    await signInManag.SignInAsync(user, true);
                    return Json(new { success = true, redirectUrl = "/Devices/Devices" });
                }

                var errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                return Json(new { success = false, message = errorMessage });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Registration failed: " + ex.Message });
            }
        }

        //private string tunePhoneNumber(string phoneNumber)
        //{
        //    string num = phoneNumber.Trim();
        //    num = Regex.Replace(num, "(?i)[ ()+_-]", "");
        //    if (num.Length == 8)
        //    {
        //        num = $"357{num}";
        //    }
        //    return num;
        //}
        //private string Gen4Code()
        //{
        //    int _min = 10000;
        //    int _max = 99999;
        //    Random _rdm = new Random();
        //    return _rdm.Next(_min, _max).ToString();

        //}
    }
}
