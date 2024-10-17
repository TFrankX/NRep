using System;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.Identity;
using WebServer.Workers;
using System.Security.Claims;
using Microsoft.CodeAnalysis.Text;
using WebServer.Models.Device;

namespace WebServer.Controllers.Device
{
    public class RedirectController : Controller
    {
        private readonly ILogger<DevicesController> Logger;
        private readonly UserManager<AppUser> userManager;
        private readonly ScanDevices scanDevices;
        private WebServer.Models.Action.ActionContext db;
        

        public RedirectController(UserManager<AppUser> _userManager, ScanDevices scanDevices, ILogger<DevicesController> logger, WebServer.Models.Action.ActionContext context)
        {
            userManager = _userManager;
            Logger = logger;
            db = context;
            this.scanDevices = scanDevices;
        }

        [HttpGet("{StationId}")]
        [Route("Redirect/Link")]
        [AllowAnonymous]
        [Authorize(Roles = "admin, manager, viewer, support")]
        public IActionResult Get(string StationId)
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
            
            ulong Id = 0;
            try
            {
                Id = Convert.ToUInt64(StationId);

            }
            catch
            {
                Id = 0;
            }
            if (Id > 0)
            {
                Models.Device.Device matches;
                try
                {
                    matches = scanDevices.DevicesData.Devices.Where(p => p.Id == Id).FirstOrDefault();
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
                        //AppAccountsLogin Model = new AppAccountsLogin { ReturnUrl=null, NewStationId = Id };
                        return RedirectToAction("AppAccountLogin", "AppAccount", new { newStationId = Id });

                    }

                    if (matches.Owners == currentUserID)
                    {
                        return RedirectToAction("User", "User");
                    }
                    

  

                }

                


                return RedirectToAction("AppAccountLogin", "AppAccount");
            }

             return RedirectToAction("User", "User");
        }
    }



}

