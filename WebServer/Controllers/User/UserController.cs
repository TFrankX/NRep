using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebServer.Models.Device;
using WebServer.Models.Identity;
using WebServer.Workers;
using WebServer.Data;
using WebServer.Controllers.Device;
using SimnetLib;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Net;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography.Xml;

namespace WebServer.Controllers.User
{

    public class PayInfo
    {
        public int Taken;
        public string UserId;
        public string Time;
        public float Cost;
        public int Available;

        public PayInfo()
        {
            Taken = 0;
            UserId = "";
            Time = "0h";
            Cost = 0;
            Available = 0;
        }

        public PayInfo(string userId, float cost,string time, int taken)
        {
            Taken = taken;
            UserId = userId;
            Time = time;
            Cost = cost;
            Available = 0;
        }
    } 

    public class UserController : Controller
    {
        private readonly ILogger<UserController> Logger;
        private readonly UserManager<AppUser> userManager;
        private readonly ScanDevices scanDevices;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserController(UserManager<AppUser> _userManager, ScanDevices scanDevices, ILogger<UserController> logger, IHttpContextAccessor httpContextAccessor)
        {
            userManager = _userManager;
            Logger = logger;
            this.scanDevices = scanDevices;
            scanDevices.EvReturnThePowerBank -= ShowInfo;
            scanDevices.EvReturnThePowerBank += ShowInfo;
            this._httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        [AllowAnonymous]
        [Authorize(Roles = "admin, manager, viewer, support")]
        public IActionResult User()
        {

            //// read cookie from IHttpContextAccessor
            //string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["KeyCharge911"];
            ////read cookie from Request object  
            //string cookieValueFromReq = Request.Cookies["KeyCharge911"];


            //read cookie from Request object  
            string cookieValueFromReq = Request.Cookies["KeyCharge911"];
            //bool taken = false;

            PayInfo payInfo = new PayInfo();
            foreach (PowerBank pb in scanDevices.DevicesData.PowerBanks)
            {
                if (((int)pb.ChargeLevel > 3) && pb.Plugged && pb.IsOk)
                {
                    payInfo.Available++;
                }
            }
            if (!string.IsNullOrEmpty(cookieValueFromReq))
            {

                foreach (PowerBank pb in scanDevices.DevicesData.PowerBanks)
                {
                    

                    if ( pb.UserId == cookieValueFromReq)
                    {
                        //float cost = ((DateTime.Now - pb.LastGetTime).Minutes/60) * pb.Price;
                        payInfo.Taken = pb.Taken ? 1:0;
                        payInfo.UserId = cookieValueFromReq;
                        payInfo.Time = pb.Taken ? $"{(DateTime.Now - pb.LastGetTime).Hours.ToString()} hr {((DateTime.Now - pb.LastGetTime).Minutes - (DateTime.Now - pb.LastGetTime).Hours*60).ToString()} min":"-";
                        payInfo.Cost = (float)Math.Round(pb.Taken ? ((DateTime.Now - pb.LastGetTime).Minutes * pb.Price / 60F) : pb.Cost, 2);
                    }
                }
            }
            //return View(Json(payInfo, new JsonSerializerOptions { PropertyNamingPolicy = null }));
            return View(payInfo);
            //return View();
            //set the key value in Cookie  
            //Set("KeyCharge911", "Hello from cookie1", 1500);
            //Delete the cookie object  
            //Remove("Key");
            //return View();
        }

        //public async Task<ActionResult> Refresh()
        //{
        //    try
        //    {


        //        DevicesData serversTable = new DevicesData(scanDevices.DevicesData.Servers, scanDevices.DevicesData.Devices, scanDevices.DevicesData.PowerBanks);

        //        serversTable.Sort();
        //        return Json(serversTable.Devices, new JsonSerializerOptions { PropertyNamingPolicy = null });
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogError(ex, $"{nameof(UserController)} -> {nameof(Refresh)} throw Exception");
        //        return null;
        //    }
        //}


        [Microsoft.AspNetCore.Mvc.HttpPost]
        [AllowAnonymous]
        [HttpPost]
        public IActionResult PushPB([FromBody] PowerBankToPush powerBankToPush)
        {


            //read cookie from Request object  
            string cookieValueFromReq = Request.Cookies["KeyCharge911"];
            bool taken = false;
            foreach (PowerBank pb in scanDevices.DevicesData.PowerBanks)
            {
                if (pb.Taken && pb.UserId == cookieValueFromReq)
                {
                    taken = true;
                }
            }

            if (!taken)
            {
                Guid guid = Guid.NewGuid();
                string UserId = guid.ToString();

                if (powerBankToPush == null || string.IsNullOrEmpty(powerBankToPush.DeviceName) || string.IsNullOrEmpty(powerBankToPush.PowerBankNum))
                    RedirectToAction("User", "User");

                if (scanDevices.PushPowerBank(powerBankToPush?.DeviceName, Convert.ToUInt32(powerBankToPush.PowerBankNum), UserId) == 200)
                {
                    //set the key value in Cookie 
                    Set("KeyCharge911", UserId, 1500);
                };
                Thread.Sleep(100);
            }


            //return RedirectToAction("ServerDetails", "ServerDetails");
            return RedirectToAction("User", "User");
        }



        /// <summary>  
        /// Get the cookie  
        /// </summary>  
        /// <param name="key">Key </param>  
        /// <returns>string value</returns>  
        public string Get(string key)
        {
            return Request.Cookies["Key"];
        }
        /// <summary>  
        /// set the cookie  
        /// </summary>  
        /// <param name="key">key (unique indentifier)</param>  
        /// <param name="value">value to store in cookie object</param>  
        /// <param name="expireTime">expiration time</param>  
        public void Set(string key, string value, int? expireTime)
        {
            CookieOptions option = new CookieOptions();
            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMilliseconds(10);
            Response.Cookies.Append(key, value, option);
        }
        /// <summary>  
        /// Delete the key  
        /// </summary>  
        /// <param name="key">Key</param>  
        public void Remove(string key)
        {
            Response.Cookies.Delete(key);
        }
        private void ShowInfo(string deviceName, ulong pbId, uint slot, float price)
        {

             Thread.Sleep(100);
        }

    }
}