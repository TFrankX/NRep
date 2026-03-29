using System.Text.Json;
using System.Text;
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
using ProtoBuf.Meta;
using WebServer.Data;
using WebServer.Models.User;
using System.Data;
using System;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Security.Claims;
using WebServer.Utils.Requests;
using System.Net.Http;
using System.Threading.Tasks;
using WebServer.Models.Stripe;
using Stripe.Checkout;
using Stripe;
using System.Drawing;
using WebServer.Models.Stripe;
using WebServer.Services.Stripe;
using WebServer.Services.Pricing;



namespace WebServer.Controllers.User
{

    public class DeviceToGetPB
    {
        public string DeviceId { get; set; }
    }

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
        private readonly IConfiguration _configuration;
        private readonly IStripeRoutines _stripeRoutines;
        private readonly IPricingService _pricingService;


        private readonly HttpClient _httpClient;


        public UserController(UserManager<AppUser> _userManager, ScanDevices scanDevices, ILogger<UserController> logger, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IStripeRoutines stripeRoutines, IPricingService pricingService)
        {
            userManager = _userManager;
            Logger = logger;
            this.scanDevices = scanDevices;
            scanDevices.EvReturnThePowerBank -= ShowInfo;
            scanDevices.EvReturnThePowerBank += ShowInfo;
            this._httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _stripeRoutines = stripeRoutines;
            _pricingService = pricingService;
        }

        //public IActionResult InitiatePayment(StripeCheckout paymentRequest)
        //{
        //    // Настраиваем параметры платежа
        //    paymentRequest.ProductName = "A-Charger";
        //    paymentRequest.ProductDescription = "Powerbank rent pay";
        //    paymentRequest.Amount = 12;
        //    paymentRequest.Currency = "eur";

        //    try
        //    {
        //        var session = _stripeRoutines.MakePayment(paymentRequest, Request);

        //        var payCard = new UserPayCard
        //        {
        //            SessionId = session.Id
        //        };

        //        return View("DoPayCard", payCard);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest($"Payment initiation failed: {ex.Message}");
        //    }
        //}


        //public async Task<IActionResult> InitiatePayment(StripeCheckout paymentRequest)
        //{

        //    paymentRequest.ProductName = "A-Charger";
        //    paymentRequest.ProductDescription = "Powerbank rent pay";
        //    paymentRequest.Amount = 12;
        //    paymentRequest.Currency = "eur";

        //    var _httpClient = new HttpClient();
        //    // Отправка POST запроса к методу HoldPayment
        //    var jsonContent = JsonConvert.SerializeObject(paymentRequest);
        //    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        //    //          var response = await _httpClient.PostAsync($"{Request.Scheme}://{Request.Host}/checkout/hold-payment", content);
        //    var response = await _httpClient.PostAsync($"{Request.Scheme}://{Request.Host}/checkout/make-payment", content);

        //    if (response.IsSuccessStatusCode)
        //    {
        //        // Получаем sessionId из ответа
        //        var result = await response.Content.ReadAsStringAsync();
        //        var sessionId = JsonConvert.DeserializeObject<dynamic>(result).sessionId;
        //        var payCard= new UserPayCard();
        //        payCard.SessionId = sessionId;

        //        return View("DoPayCard", payCard);
        //        //return Ok(new { sessionId });
        //    }

        //    return BadRequest("Payment initiation failed");
        //}


        //[HttpGet]
        //[AllowAnonymous]
        //[Authorize(Roles = "admin, manager, viewer, support")]
        //public IActionResult User()
        //{

        //    //// read cookie from IHttpContextAccessor
        //    //string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["KeyCharge911"];
        //    ////read cookie from Request object  
        //    //string cookieValueFromReq = Request.Cookies["KeyCharge911"];


        //    //read cookie from Request object  
        //    string cookieValueFromReq = Request.Cookies["KeyCharge911"];
        //    //bool taken = false;

        //    PayInfo payInfo = new PayInfo();
        //    foreach (PowerBank pb in scanDevices.DevicesData.PowerBanks)
        //    {
        //        if (((int)pb.ChargeLevel > 3) && pb.Plugged && pb.IsOk)
        //        {
        //            payInfo.Available++;
        //        }
        //    }
        //    if (!string.IsNullOrEmpty(cookieValueFromReq))
        //    {

        //        foreach (PowerBank pb in scanDevices.DevicesData.PowerBanks)
        //        {


        //            if (pb.UserId == cookieValueFromReq)
        //            {
        //                //float cost = ((DateTime.Now - pb.LastGetTime).Minutes/60) * pb.Price;
        //                payInfo.Taken = pb.Taken ? 1 : 0;
        //                payInfo.UserId = cookieValueFromReq;
        //                payInfo.Time = pb.Taken ? $"{(DateTime.Now - pb.LastGetTime).Hours.ToString()} hr {((DateTime.Now - pb.LastGetTime).Minutes - (DateTime.Now - pb.LastGetTime).Hours * 60).ToString()} min" : "-";
        //                payInfo.Cost = (float)Math.Round(pb.Taken ? ((DateTime.Now - pb.LastGetTime).Minutes * pb.Price / 60F) : pb.Cost, 2);
        //            }
        //        }
        //    }
        //    //return View(Json(payInfo, new JsonSerializerOptions { PropertyNamingPolicy = null }));
        //    return View(payInfo);
        //    //return View();
        //    //set the key value in Cookie  
        //    //Set("KeyCharge911", "Hello from cookie1", 1500);
        //    //Delete the cookie object  
        //    //Remove("Key");
        //    //return View();
        //}



        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> AlreadyPaid( string session_Id,  string stationId, string powerBankId)
        {
            PayInfo payInfo = new PayInfo();
            ulong stationIdn=0;
            ulong powerBankIdn = 0;
            if (!ulong.TryParse(stationId, out stationIdn))
            {
                Logger.LogError($"Getting invalid Station Id: {stationId}\n");
                return BadRequest("Invalid stationId");
            }
            if (!ulong.TryParse(powerBankId, out powerBankIdn))
            {
                Logger.LogError($"Getting invalid PowerBank Id: {stationId}\n");
                return BadRequest("Invalid powerbankId"); ;
            }

            if (string.IsNullOrEmpty(session_Id))
            {
                return BadRequest("Missing session_id.");
            }

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

            // 1. Получаем сессию
            var sessionService = new SessionService();
            Session session;
            try
            {
                session = sessionService.Get(session_Id);
            }
            catch (StripeException ex)
            {
                return BadRequest("Invalid session_id: " + ex.Message);
            }

            PaymentIntent paymentIntent = null;
            var paymentIntentId = session.PaymentIntentId;

            if (!string.IsNullOrEmpty(paymentIntentId))
            {
                var paymentIntentService = new PaymentIntentService();
                paymentIntent = paymentIntentService.Get(paymentIntentId);
            }

            var chargeService = new ChargeService();
            var chargeList = chargeService.List(new ChargeListOptions
            {
                PaymentIntent = paymentIntentId,
                Limit = 1
            });

            var charge = chargeList?.FirstOrDefault();

            DateTime? paymentDateTime = null;

            if (charge != null)
            {
                paymentDateTime = charge.Created;
            }

            Customer customer = null;
            if (!string.IsNullOrEmpty(session.CustomerId))
            {
                var customerService = new CustomerService();
                customer = customerService.Get(session.CustomerId);
            }



            var pbPush = scanDevices.DevicesData.PowerBanks[scanDevices.DevicesData.PowerBanks.FindIndex(item => item.Id == powerBankIdn)];
            var pbDevice = scanDevices.DevicesData.Devices.FirstOrDefault(d => d.Id == stationIdn);
            var typeOfUse = pbDevice?.TypeOfUse ?? TypeOfUse.PayByCard;


            if (pbPush.Reserved && !pbPush.Taken && pbPush.Plugged)
            {

                var userId = userManager.GetUserId(base.User);
                List<string> roles = new List<string>();

                if (string.IsNullOrEmpty(userId))
                {
                    Guid guid = Guid.NewGuid();
                    userId = guid.ToString();
                }
                else
                {
                    var user = await userManager.FindByIdAsync(userId);
                    var rolesTask = await userManager.GetRolesAsync(user);

                    roles = rolesTask.ToList();
                }



                pbPush.Reserved = false;
                pbPush.SessionId = session_Id;
                pbPush.PaymentInfo = $"Name: {session.CustomerDetails?.Name ?? "Unknown"},amount:{(session.AmountTotal / 100m).ToString()},time: {paymentDateTime?.ToString("g") ?? "Unknown".ToString()}, email:{session.CustomerDetails?.Email}, card country:{charge.PaymentMethodDetails?.Card.Country}, type: {charge.PaymentMethodDetails?.Card.Brand}, card last 4 digs: {charge.PaymentMethodDetails?.Card.Last4.ToString()}, card expired: {charge.PaymentMethodDetails?.Card.ExpMonth.ToString("D2")}/{charge.PaymentMethodDetails?.Card.ExpYear.ToString()}";
                pbPush.Stored = false;

                var pbId = scanDevices.PushPowerBank(pbPush.HostDeviceName, pbPush.HostSlot, userId, roles);

  
                if ((pbPush.Id > 1000) && (pbPush != null))
                {

                    //set the key value in Cookie 
                    Set("KeyCharge911", userId, 1500);

                    payInfo.Taken = pbPush.Taken ? 1 : 0;
                    payInfo.UserId = $"{session.CustomerDetails?.Name ?? "Unknown"}";
                    payInfo.Time = pbPush.Taken ? $"{(DateTime.Now - pbPush.LastGetTime).Hours.ToString()} hr {((DateTime.Now - pbPush.LastGetTime).Minutes - (DateTime.Now - pbPush.LastGetTime).Hours * 60).ToString()} min" : "-";
                    payInfo.Cost = _pricingService.CalculateCost(typeOfUse, pbPush.LastGetTime, pbPush.Taken);

                    return View("Do",payInfo);

                };
                Thread.Sleep(100);


            }

            payInfo.Taken = pbPush.Taken ? 1 : 0;
            payInfo.UserId = $"{ session.CustomerDetails?.Name ?? "Unknown"}";
            //payInfo.Time = pb.Taken ? $"{(DateTime.Now - pb.LastGetTime).Hours.ToString()} hr {((DateTime.Now - pb.LastGetTime).Minutes - (DateTime.Now - pb.LastGetTime).Hours * 60).ToString()} min" : "-";
            payInfo.Time = pbPush.Taken ? $"{(DateTime.Now - pbPush.LastGetTime).Hours.ToString()} hr {((DateTime.Now - pbPush.LastGetTime).Minutes).ToString()} min" : "-";
            payInfo.Cost = _pricingService.CalculateCost(typeOfUse, pbPush.LastGetTime, pbPush.Taken);

            return View("Do", payInfo);


        }




        //[HttpGet("{deviceId}")]
        [HttpGet]
        [AllowAnonymous]

        //[Authorize(Roles = "admin, manager, viewer, support")]
        public async Task<IActionResult> Do(string deviceId)
        {

            if (deviceId == null || string.IsNullOrEmpty(deviceId))
            {
                return StatusCode(403);
            }

            //// read cookie from IHttpContextAccessor
            //string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["KeyCharge911"];
            ////read cookie from Request object  
            //string cookieValueFromReq = Request.Cookies["KeyCharge911"];


            //read cookie from Request object



            Models.Device.Device device;
            try
            {
                device = scanDevices.DevicesData.Devices[scanDevices.DevicesData.Devices.FindIndex(item => item.Id_str == deviceId)];
            }
            catch
            {
                device = null;
                Logger.LogInformation($"Trying to get invalid device with Id: {deviceId}\n");
                return StatusCode(404);
            }



            string cookieValueFromReq = Request.Cookies["KeyCharge911"];
            PayInfo payInfo = new PayInfo();
            //foreach (PowerBank pb in scanDevices.DevicesData.PowerBanks)
            //{
            //    if (((int)pb.ChargeLevel > 3) && pb.Plugged && pb.IsOk)
            //    {
            //        payInfo.Available++;
            //    }
            //}

            var devicePbs = scanDevices.DevicesData.PowerBanks.Where(p => p.HostDeviceId == device.Id).ToList<WebServer.Models.Device.PowerBank>();

            var userPbs = scanDevices.DevicesData.PowerBanks.Where(p => p.UserId == cookieValueFromReq).ToList<WebServer.Models.Device.PowerBank>();







            if (!string.IsNullOrEmpty(cookieValueFromReq) && (device.TypeOfUse != TypeOfUse.FreeMultiTake) )
            {
                foreach (PowerBank pb in userPbs)
                {
                    if ((pb.UserId == cookieValueFromReq) && (pb.Taken))
                    {
                        payInfo.Taken = pb.Taken ? 1 : 0;
                        payInfo.UserId = cookieValueFromReq;
                        //payInfo.Time = pb.Taken ? $"{(DateTime.Now - pb.LastGetTime).Hours.ToString()} hr {((DateTime.Now - pb.LastGetTime).Minutes - (DateTime.Now - pb.LastGetTime).Hours * 60).ToString()} min" : "-";
                        payInfo.Time = pb.Taken ? $"{(DateTime.Now - pb.LastGetTime).Hours.ToString()} hr {((DateTime.Now - pb.LastGetTime).Minutes).ToString()} min" : "-";
                        payInfo.Cost = _pricingService.CalculateCost(device.TypeOfUse, pb.LastGetTime, pb.Taken);

                        return View(payInfo);
                    }
                }
            }






            // if ((device.TypeOfUse != TypeOfUse.FreeTake && device.TypeOfUse != TypeOfUse.FreeMultiTake && device.TypeOfUse != TypeOfUse.SMSTake && device.TypeOfUse != TypeOfUse.PayByCard) ||(!device.Activated))

            if ((device.TypeOfUse==0) || (!device.Activated))
            {
                payInfo.Taken = 0;
                payInfo.UserId = "Not registred/enabled device";
                //payInfo.Time = pb.Taken ? $"{(DateTime.Now - pb.LastGetTime).Hours.ToString()} hr {((DateTime.Now - pb.LastGetTime).Minutes - (DateTime.Now - pb.LastGetTime).Hours * 60).ToString()} min" : "-";
                payInfo.Time =  "-";
                payInfo.Cost = 0;
                return View(payInfo);         
            }   





            bool taken = false;
            foreach (PowerBank pb in devicePbs)
            {
                if (pb.Taken && pb.UserId == cookieValueFromReq)
                {
                    taken = true;
                }
            }


            if ((!taken)||(device.TypeOfUse == TypeOfUse.FreeMultiTake))
            {
                var maxCharge = 0;
                PowerBank? pbTake = null;
                uint maxChargedSlot = 0;
                foreach (PowerBank pb in devicePbs)
                {
                    TimeSpan? resTimeDiff = (DateTime.Now - pb.ReserveTime);
                    double resTimeDiffSec = 0;
                    if (resTimeDiff.HasValue)
                    {
                         resTimeDiffSec = resTimeDiff.Value.TotalSeconds;
                       
                    }
                    else
                    {
                        resTimeDiffSec = 0.0;
                    }
                    if (!pb.Taken && pb.Plugged && (!pb.Reserved || (resTimeDiffSec>90.0)))
                    {
                        if ((int)pb.ChargeLevel > maxCharge)
                        {
                            maxCharge = (int)pb.ChargeLevel;
                            maxChargedSlot = pb.HostSlot;
                            pbTake = pb;
                        }
                       
                    }
                    }





                var userId = userManager.GetUserId(base.User);
                List<string> roles = new List<string>();

                if (string.IsNullOrEmpty(userId))
                {
                    Guid guid = Guid.NewGuid();
                    userId = guid.ToString();
                }
                else
                {
                    var user = await userManager.FindByIdAsync(userId);
                    var rolesTask = await userManager.GetRolesAsync(user);

                    roles = rolesTask.ToList();
                }




                if (maxChargedSlot == 0)
                    RedirectToAction("User", "User");


                if (device.TypeOfUse == TypeOfUse.SMSTake)
                {
                    return View("DoSMS",new UserSMS { StationId=device.Id});
                }


                if (_pricingService.IsPaidOption(device.TypeOfUse))
                {
                   if (pbTake != null)
                    {
                        var pricingPlan = _pricingService.GetPlan(device.TypeOfUse);

                        pbTake.Reserved = true;
                        pbTake.ReserveTime = DateTime.Now;
                        pbTake.Stored = false;


                        var paymentRequest = new StripeCheckout
                        {
                            StationId = device.Id,
                            PowerBankId = pbTake.Id,
                            Amount = pricingPlan.HoldAmount,
                            ProductName = "A-Charger",
                            ProductDescription = "Powerbank rent pay",
                            Currency = pricingPlan.Currency
                        };



                        try
                        {
                            //Для мгновенного списывания
                            //var session = _stripeRoutines.MakePayment(paymentRequest, Request);
                            //Для захвата суммы
                            var session = _stripeRoutines.HoldPayment(paymentRequest, Request);
                            var payCard = new UserPayCard
                            {
                               SessionId = session.Id
                            };

                            return View("DoPayCard", payCard);
                        }
                        catch (Exception ex)
                        {
                             return BadRequest($"Payment initiation failed: {ex.Message}");
                        }




                        //_stripeRoutines.MakePayment(paymentRequest, Request);

                        //return RedirectToAction("InitiatePayment", new StripeCheckout { StationId = device.Id, PowerBankId = pbTake.Id, Amount=pbTake.Price});
                    }
                    payInfo.Cost = 0;
                    return View(payInfo);
                    //return View("DoPayCard", new UserSMS { StationId = device.Id });
                }


                //if (string.IsNullOrEmpty(userId))
                //{
                //    userId = "unknown";

                //}
                //else
                //{
                //    var user = await userManager.FindByIdAsync(userId);
                //    var rolesTask = await userManager.GetRolesAsync(user);
                //    roles = rolesTask.ToList();
                //}





                var pbId = scanDevices.PushPowerBank(device.DeviceName, maxChargedSlot, userId, roles);
                PowerBank pbPush=null;
                try
                {
                    pbPush=scanDevices.DevicesData.PowerBanks[scanDevices.DevicesData.PowerBanks.FindIndex(item => item.Id == pbId)];
                }
                catch
                {

                }
                if ((pbId > 1000) && (pbPush!=null))
                {
                    
                    //set the key value in Cookie 
                    Set("KeyCharge911", userId, 1500);

                    payInfo.Taken = pbPush.Taken ? 1 : 0;
                    payInfo.UserId = userId;
                    payInfo.Time = pbPush.Taken ? $"{(DateTime.Now - pbPush.LastGetTime).Hours.ToString()} hr {((DateTime.Now - pbPush.LastGetTime).Minutes - (DateTime.Now - pbPush.LastGetTime).Hours * 60).ToString()} min" : "-";
                    payInfo.Cost = _pricingService.CalculateCost(device.TypeOfUse, pbPush.LastGetTime, pbPush.Taken);
                    return View(payInfo);

                };
                Thread.Sleep(100);
            }

            payInfo.Cost = 0;
            return View(payInfo);



        }
        [HttpPost("{phoneNumber}")]
        [Route("SendSMSCode")]
        [AllowAnonymous]
        public async Task<ActionResult> SendSMSCode(UserSMS model)
        {
            //!!!!!!!!!!
            WebServer.Models.Identity.AppUser user;


                model.Message = "Piska";
                model.CodeReq = true;

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
                        return View("DoSMS", model);
                }

                    sms.Send(phone, "takecharger", cd);
                    return View("DoCheckSMSCode", model);
                }
                catch
                {
                    ModelState.AddModelError("", $"Problem with sms-gate");
                    return View("DoSMS", model);
                }


            


        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckSMSCode(UserSMS model)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            if (ModelState.IsValid)
            {


                WebServer.Models.Identity.AppUser user;

                SMS chSMS = new SMS();
                model.PhoneNumber = chSMS.TunePhoneNumber(model.PhoneNumber);

                //try
                //{
                //    user = userManag.Users.Where(x => x.PhoneNumber == model.PhoneNumber).First();
                //}
                //catch
                //{
                //    user = null;
                //}

                //var user = await userManag.FindByNameAsync(model.PhoneNumber);
                if (model.SMSCode == TempData["cd"].ToString().Trim())
                {


                    PayInfo payInfo = new PayInfo();
                    //foreach (PowerBank pb in scanDevices.DevicesData.PowerBanks)
                    //{
                    //    if (((int)pb.ChargeLevel > 3) && pb.Plugged && pb.IsOk)
                    //    {
                    //        payInfo.Available++;
                    //    }
                    //}

                    var devicePbs = scanDevices.DevicesData.PowerBanks.Where(p => p.HostDeviceId == model.StationId).ToList<WebServer.Models.Device.PowerBank>();

                    //var userPbs = scanDevices.DevicesData.PowerBanks.Where(p => p.UserId == cookieValueFromReq).ToList<WebServer.Models.Device.PowerBank>();



                    Models.Device.Device device;
                    try
                    {
                        device = scanDevices.DevicesData.Devices[scanDevices.DevicesData.Devices.FindIndex(item => item.Id == model.StationId)];
                    }
                    catch
                    {
                        device = null;
                        Logger.LogInformation($"Trying to get invalid device with Id: {model.StationId}\n");
                        return StatusCode(404);
                    }


                    Models.Device.Device matches;
                    try
                    {
                        matches = scanDevices.DevicesData.Devices.Where(p => p.Id == model.StationId).FirstOrDefault();
                    }
                    catch
                    {
                        matches = null;
                    }
                    if (matches != null)
                    {

                        var maxCharge = 0;
                        uint maxChargedSlot = 0;
                        foreach (PowerBank pb in devicePbs)
                        {
                            if (!pb.Taken && pb.Plugged)
                            {
                                if ((int)pb.ChargeLevel > maxCharge)
                                {
                                    maxCharge = (int)pb.ChargeLevel;
                                    maxChargedSlot = pb.HostSlot;
                                }

                            }
                        }

                        if (maxChargedSlot == 0)
                            RedirectToAction("User", "User");

                        var pbId = scanDevices.PushPowerBank(device.DeviceName, maxChargedSlot, model.PhoneNumber, new List<string>{"Guest"});
                        PowerBank pbPush = null;
                        try
                        {
                            pbPush = scanDevices.DevicesData.PowerBanks[scanDevices.DevicesData.PowerBanks.FindIndex(item => item.Id == pbId)];
                        }
                        catch
                        {

                        }
                        if ((pbId > 1000) && (pbPush != null))
                        {

                            //set the key value in Cookie 
                            Set("KeyCharge911", model.PhoneNumber, 1500);

                            payInfo.Taken = pbPush.Taken ? 1 : 0;
                            payInfo.UserId = model.PhoneNumber;
                            payInfo.Time = pbPush.Taken ? $"{(DateTime.Now - pbPush.LastGetTime).Hours.ToString()} hr {((DateTime.Now - pbPush.LastGetTime).Minutes - (DateTime.Now - pbPush.LastGetTime).Hours * 60).ToString()} min" : "-";
                            payInfo.Cost = _pricingService.CalculateCost(device.TypeOfUse, pbPush.LastGetTime, pbPush.Taken);
                            return View("User",payInfo);

                        };
                        Thread.Sleep(100);

                    }


                }
                else
                {
                    ModelState.AddModelError("", "Incorrect SMS code");
                    return View("DoSMS", model);
                }

                // return RedirectToAction("Servers", "Servers");

            }
            return View("DoSMS", model);
        }


        [Microsoft.AspNetCore.Mvc.HttpPost]
        [Authorize]
        [HttpPost]
        public async Task <IActionResult> PushPB([FromBody] PowerBankToPush powerBankToPush)
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

                var userId = userManager.GetUserId(base.User);
                var userName = userManager.GetUserName(base.User);
                List<string> roles = new List<string>();
                if (string.IsNullOrEmpty(userId))
                {
                    userId = "unknown";

                }
                else
                {
                    var user = await userManager.FindByIdAsync(userId);
                    var rolesTask = await userManager.GetRolesAsync(user);
                    roles = rolesTask.ToList();
                }

                if (scanDevices.PushPowerBank(powerBankToPush?.DeviceName, Convert.ToUInt32(powerBankToPush.PowerBankNum), userName,roles) == 200)
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