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
using WebServer.Services.Sms;
using System.Net.Http;
using System.Threading.Tasks;
using WebServer.Models.Stripe;
using Stripe.Checkout;
using Stripe;
using System.Drawing;
using WebServer.Models.Stripe;
using WebServer.Services.Stripe;
using WebServer.Services.Pricing;
using WebServer.Services.Settings;



namespace WebServer.Controllers.User
{

    public class DeviceToGetPB
    {
        public string DeviceId { get; set; }
    }

    public class PowerBankInfo
    {
        public string PowerBankId { get; set; } = "";
        public string PowerBankName { get; set; } = "";
        public string Time { get; set; } = "-";
        public string StartTime { get; set; } = "-";
        public float Cost { get; set; }
        public ulong StationId { get; set; }
        public bool Returned { get; set; } = false;
    }

    public class PayInfo
    {
        public int Taken;
        public string UserId;
        public string Time;
        public float Cost;
        public int Available;
        public List<PowerBankInfo> PowerBanks { get; set; } = new List<PowerBankInfo>();
        public bool CanTakeMore { get; set; }
        public ulong StationId { get; set; }
        public string TypeOfUse { get; set; } = "";
        public string SupportPhone { get; set; } = "+357 99 123 456";
        public string SupportEmail { get; set; } = "support@a-charger.com";
        public string Language { get; set; } = "en";
        public string ZoneColor { get; set; } = "#7C3AED";

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
        private readonly ISmsService _smsService;
        private readonly IAppSettingsService _appSettingsService;

        private readonly HttpClient _httpClient;


        public UserController(UserManager<AppUser> _userManager, ScanDevices scanDevices, ILogger<UserController> logger, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IStripeRoutines stripeRoutines, IPricingService pricingService, ISmsService smsService, IAppSettingsService appSettingsService)
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
            _smsService = smsService;
            _appSettingsService = appSettingsService;
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

            // Загружаем настройки поддержки
            var supportSettings = await _appSettingsService.GetSupportSettingsAsync();
            payInfo.SupportPhone = supportSettings.Phone;
            payInfo.SupportEmail = supportSettings.Email;

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



            var pbPush = scanDevices.DevicesData.PowerBanks.GetById(powerBankIdn);
            var pbDevice = scanDevices.DevicesData.Devices.GetById(stationIdn);
            var typeOfUse = pbDevice?.TypeOfUse ?? TypeOfUse.PayByCard;

            if (pbPush == null)
            {
                return BadRequest("PowerBank not found");
            }

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

                // Для PayByCard используем имя плательщика из Stripe, а не login ID
                var customerName = session.CustomerDetails?.Name ?? "Unknown";

                Logger.LogInformation($"AlreadyPaid: Pushing powerbank {pbPush.Id} from slot {pbPush.HostSlot} device {pbPush.HostDeviceName}, Plugged={pbPush.Plugged}, Customer={customerName}");

                var pbId = scanDevices.PushPowerBank(pbPush.HostDeviceName, pbPush.HostSlot, customerName, roles);

                Logger.LogInformation($"AlreadyPaid: PushPowerBank returned {pbId}, pbPush.Taken={pbPush.Taken}");

                if ((pbPush.Id > 1000) && (pbPush != null))
                {

                    //set the key value in Cookie - use customerName to match powerbank UserId
                    Set("KeyCharge911", customerName, 1500);

                    payInfo.Taken = pbPush.Taken ? 1 : 0;
                    payInfo.UserId = customerName;

                    // Fill PowerBanks list for new UI
                    if (pbPush.Taken)
                    {
                        var duration = DateTime.Now - pbPush.LastGetTime;
                        payInfo.PowerBanks = new List<PowerBankInfo>
                        {
                            new PowerBankInfo
                            {
                                PowerBankId = pbPush.Id_str,
                                PowerBankName = pbPush.Name ?? pbPush.Id_str,
                                StartTime = pbPush.LastGetTime.ToString("dd.MM HH:mm"),
                                Time = $"{(int)duration.TotalHours}h {duration.Minutes}m",
                                Cost = _pricingService.CalculateCost(typeOfUse, pbPush.LastGetTime, pbPush.Taken),
                                StationId = pbPush.HostDeviceId
                            }
                        };
                        payInfo.Time = payInfo.PowerBanks[0].Time;
                        payInfo.Cost = payInfo.PowerBanks[0].Cost;
                        payInfo.StationId = pbPush.HostDeviceId;
                    }

                    return View("Do",payInfo);

                };
                await Task.Delay(100);


            }

            payInfo.Taken = pbPush.Taken ? 1 : 0;
            payInfo.UserId = $"{ session.CustomerDetails?.Name ?? "Unknown"}";

            // Fill PowerBanks list for new UI
            if (pbPush.Taken)
            {
                var duration = DateTime.Now - pbPush.LastGetTime;
                payInfo.PowerBanks = new List<PowerBankInfo>
                {
                    new PowerBankInfo
                    {
                        PowerBankId = pbPush.Id_str,
                        PowerBankName = pbPush.Name ?? pbPush.Id_str,
                        StartTime = pbPush.LastGetTime.ToString("dd.MM HH:mm"),
                        Time = $"{(int)duration.TotalHours}h {duration.Minutes}m",
                        Cost = _pricingService.CalculateCost(typeOfUse, pbPush.LastGetTime, pbPush.Taken),
                        StationId = pbPush.HostDeviceId
                    }
                };
                payInfo.Time = payInfo.PowerBanks[0].Time;
                payInfo.Cost = payInfo.PowerBanks[0].Cost;
                payInfo.StationId = pbPush.HostDeviceId;
            }

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

            var device = scanDevices.DevicesData.Devices.FirstOrDefault(item => item.Id_str == deviceId);
            if (device == null)
            {
                Logger.LogInformation($"Trying to get invalid device with Id: {deviceId}\n");
                return StatusCode(404);
            }

            string cookieValueFromReq = Request.Cookies["KeyCharge911"];
            PayInfo payInfo = new PayInfo();

            // Загружаем настройки поддержки для всех путей
            var supportSettings = await _appSettingsService.GetSupportSettingsAsync();
            payInfo.SupportPhone = supportSettings.Phone;
            payInfo.SupportEmail = supportSettings.Email;
            //foreach (PowerBank pb in scanDevices.DevicesData.PowerBanks)
            //{
            //    if (((int)pb.ChargeLevel > 3) && pb.Plugged && pb.IsOk)
            //    {
            //        payInfo.Available++;
            //    }
            //}

            var devicePbs = scanDevices.DevicesData.PowerBanks.Where(p => p.HostDeviceId == device.Id).ToList<WebServer.Models.Device.PowerBank>();

            var userPbs = scanDevices.DevicesData.PowerBanks.Where(p => p.UserId == cookieValueFromReq).ToList<WebServer.Models.Device.PowerBank>();







            if (!string.IsNullOrEmpty(cookieValueFromReq))
            {
                // Для FreeMultiTake: проверяем защиту от дубль-запроса (если последний повербанк взят < 3 сек назад)
                if (device.TypeOfUse == TypeOfUse.FreeMultiTake)
                {
                    var takenPbs = userPbs.Where(p => p.Taken && p.UserId == cookieValueFromReq).ToList();
                    if (takenPbs.Any())
                    {
                        // Проверяем время последнего взятия - защита от двойного запроса браузера
                        var lastTaken = takenPbs.Max(p => p.LastGetTime);
                        var timeSinceLastTake = (DateTime.Now - lastTaken).TotalSeconds;

                        if (timeSinceLastTake < 3)
                        {
                            // Слишком быстрый повторный запрос - показать текущее состояние без нового push
                            Logger.LogInformation($"FreeMultiTake: Duplicate request detected ({timeSinceLastTake:F1}s), showing current state");
                            var multiPayInfo = await BuildPayInfoAsync(cookieValueFromReq, device.Id);
                            return View(multiPayInfo);
                        }
                        // Иначе продолжаем - выдадим ещё один повербанк ниже
                    }
                }
                else
                {
                    // Для остальных режимов: стандартная логика - один повербанк
                    foreach (PowerBank pb in userPbs)
                    {
                        if ((pb.UserId == cookieValueFromReq) && (pb.Taken))
                        {
                            var duration = DateTime.Now - pb.LastGetTime;
                            payInfo.Taken = 1;
                            payInfo.UserId = cookieValueFromReq;
                            payInfo.StationId = device.Id;
                            payInfo.PowerBanks = new List<PowerBankInfo>
                            {
                                new PowerBankInfo
                                {
                                    PowerBankId = pb.Id_str,
                                    PowerBankName = pb.Name ?? pb.Id_str,
                                    StartTime = pb.LastGetTime.ToString("dd.MM HH:mm"),
                                    Time = $"{(int)duration.TotalHours}h {duration.Minutes}m",
                                    Cost = _pricingService.CalculateCost(device.TypeOfUse, pb.LastGetTime, pb.Taken),
                                    StationId = pb.HostDeviceId
                                }
                            };
                            payInfo.Time = payInfo.PowerBanks[0].Time;
                            payInfo.Cost = payInfo.PowerBanks[0].Cost;

                            return View(payInfo);
                        }
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


            // Для FreeMultiTake проверяем лимит 4 повербанка
            if (device.TypeOfUse == TypeOfUse.FreeMultiTake)
            {
                var userTakenCount = userPbs.Count(p => p.Taken && p.UserId == cookieValueFromReq);
                if (userTakenCount >= 4)
                {
                    // Достигнут лимит - показать текущее состояние
                    var multiPayInfo = await BuildPayInfoAsync(cookieValueFromReq, device.Id);
                    multiPayInfo.CanTakeMore = false;
                    return View(multiPayInfo);
                }
            }

            if ((!taken) || (device.TypeOfUse == TypeOfUse.FreeMultiTake))
            {
                var maxCharge = 0;
                PowerBank? pbTake = null;
                uint maxChargedSlot = 0;

                Logger.LogInformation($"User/Do: Checking {devicePbs.Count} powerbanks for device {device.DeviceName}");

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

                    Logger.LogInformation($"  PB slot {pb.HostSlot}: Taken={pb.Taken}, Plugged={pb.Plugged}, Reserved={pb.Reserved}, Charge={pb.ChargeLevel}");

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
                {
                    Logger.LogWarning($"User/Do: No available powerbanks found for device {device.DeviceName}");
                    RedirectToAction("User", "User");
                }


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
                var pbPush = scanDevices.DevicesData.PowerBanks.GetById(pbId);

                if ((pbId > 1000) && (pbPush != null))
                {

                    //set the key value in Cookie
                    Set("KeyCharge911", userId, 1500);

                    if (pbPush.Taken)
                    {
                        // Для FreeMultiTake: используем BuildPayInfoAsync для получения информации обо ВСЕХ повербанках
                        if (device.TypeOfUse == TypeOfUse.FreeMultiTake)
                        {
                            var multiPayInfo = await BuildPayInfoAsync(userId, device.Id);
                            return View(multiPayInfo);
                        }

                        // Для остальных режимов: только один повербанк
                        var duration = DateTime.Now - pbPush.LastGetTime;
                        payInfo.Taken = 1;
                        payInfo.UserId = userId;
                        payInfo.StationId = device.Id;
                        payInfo.PowerBanks = new List<PowerBankInfo>
                        {
                            new PowerBankInfo
                            {
                                PowerBankId = pbPush.Id_str,
                                PowerBankName = pbPush.Name ?? pbPush.Id_str,
                                StartTime = pbPush.LastGetTime.ToString("dd.MM HH:mm"),
                                Time = $"{(int)duration.TotalHours}h {duration.Minutes}m",
                                Cost = _pricingService.CalculateCost(device.TypeOfUse, pbPush.LastGetTime, pbPush.Taken),
                                StationId = pbPush.HostDeviceId
                            }
                        };
                        payInfo.Time = payInfo.PowerBanks[0].Time;
                        payInfo.Cost = payInfo.PowerBanks[0].Cost;
                    }
                    await LoadZoneSettingsAsync(payInfo, device.UserLocation);
                    return View(payInfo);

                };
                await Task.Delay(100);
            }

            payInfo.Cost = 0;
            await LoadZoneSettingsAsync(payInfo, device?.UserLocation ?? "");
            return View(payInfo);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult DoSMS(ulong stationId)
        {
            var device = scanDevices.DevicesData.Devices.GetById(stationId);
            if (device == null)
            {
                Logger.LogWarning($"DoSMS: Device not found: {stationId}");
                return NotFound("Station not found");
            }

            return View("DoSMS", new UserSMS { StationId = stationId });
        }

        [HttpPost("{phoneNumber}")]
        [Route("SendSMSCode")]
        [AllowAnonymous]
        public async Task<ActionResult> SendSMSCode(UserSMS model)
        {
            if (!_smsService.IsValidPhoneNumber(model.PhoneNumber))
            {
                ModelState.AddModelError("", "Wrong phone number format");
                return View("DoSMS", model);
            }

            // Проверяем cooldown - не чаще чем раз в 3 минуты
            var lastSentStr = HttpContext.Session.GetString("sms_last_sent");
            if (!string.IsNullOrEmpty(lastSentStr) && DateTime.TryParse(lastSentStr, out var lastSent))
            {
                var elapsed = DateTime.Now - lastSent;
                if (elapsed.TotalSeconds < 180) // 3 минуты
                {
                    var remaining = 180 - (int)elapsed.TotalSeconds;
                    ViewBag.ResendCooldown = remaining;
                    return View("DoCheckSMSCode", model);
                }
            }

            try
            {
                string cd = _smsService.GenerateCode();
                // Сохраняем код и время отправки в Session
                HttpContext.Session.SetString("sms_code", cd);
                HttpContext.Session.SetString("sms_phone", model.PhoneNumber);
                HttpContext.Session.SetString("sms_last_sent", DateTime.Now.ToString("O"));

                var sent = await _smsService.SendCodeAsync(model.PhoneNumber, "takecharger", cd);
                if (!sent)
                {
                    ModelState.AddModelError("", "Problem with sms-gate");
                    return View("DoSMS", model);
                }
                ViewBag.ResendCooldown = 180; // 3 минуты cooldown после отправки
                return View("DoCheckSMSCode", model);
            }
            catch
            {
                ModelState.AddModelError("", "Problem with sms-gate");
                return View("DoSMS", model);
            }


            


        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckSMSCode(UserSMS model)
        {
            // Получаем сохранённый код из Session
            var savedCode = HttpContext.Session.GetString("sms_code");
            var savedPhone = HttpContext.Session.GetString("sms_phone");

            Logger.LogInformation($"CheckSMSCode: SMSCode='{model.SMSCode}', savedCode='{savedCode}', savedPhone='{savedPhone}'");

            if (string.IsNullOrEmpty(savedCode))
            {
                ModelState.AddModelError("", "SMS code expired. Please request a new code.");
                return View("DoCheckSMSCode", model);
            }

            if (string.IsNullOrEmpty(model.SMSCode))
            {
                ModelState.AddModelError("SMSCode", "Please enter the SMS code");
                return View("DoCheckSMSCode", model);
            }

            if (model.SMSCode?.Trim() == savedCode.Trim())
            {
                Logger.LogInformation($"CheckSMSCode: Code matched for phone {savedPhone}");
                    // Очищаем код после успешной проверки
                    HttpContext.Session.Remove("sms_code");
                    HttpContext.Session.Remove("sms_phone");


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
                    device = scanDevices.DevicesData.Devices.GetById(model.StationId);
                    if (device == null)
                    {
                        Logger.LogInformation($"Trying to get invalid device with Id: {model.StationId}\n");
                        return StatusCode(404);
                    }

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
                    var pbPush = scanDevices.DevicesData.PowerBanks.GetById(pbId);

                    if ((pbId > 1000) && (pbPush != null))
                    {
                        //set the key value in Cookie
                        Set("KeyCharge911", model.PhoneNumber, 1500);

                        payInfo.Taken = pbPush.Taken ? 1 : 0;
                        payInfo.UserId = model.PhoneNumber;
                        payInfo.Time = pbPush.Taken ? $"{(DateTime.Now - pbPush.LastGetTime).Hours.ToString()} hr {((DateTime.Now - pbPush.LastGetTime).Minutes - (DateTime.Now - pbPush.LastGetTime).Hours * 60).ToString()} min" : "-";
                        payInfo.Cost = _pricingService.CalculateCost(device.TypeOfUse, pbPush.LastGetTime, pbPush.Taken);
                        return View("User", payInfo);
                    }
                    await Task.Delay(100);
                }
            else
            {
                Logger.LogWarning($"CheckSMSCode: Code mismatch - entered '{model.SMSCode?.Trim()}' vs saved '{savedCode.Trim()}'");
                ModelState.AddModelError("", "Incorrect SMS code");
                return View("DoCheckSMSCode", model);
            }

            // Сюда не должны попасть
            return View("DoCheckSMSCode", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckSMSCodeAjax(UserSMS model)
        {
            var savedCode = HttpContext.Session.GetString("sms_code");
            var savedPhone = HttpContext.Session.GetString("sms_phone");

            Logger.LogInformation($"CheckSMSCodeAjax: SMSCode='{model.SMSCode}', savedCode='{savedCode}'");

            if (string.IsNullOrEmpty(savedCode))
            {
                return Json(new { success = false, message = "SMS code expired. Please request a new code", expired = true });
            }

            if (string.IsNullOrEmpty(model.SMSCode))
            {
                return Json(new { success = false, message = "Please enter the SMS code" });
            }

            if (model.SMSCode?.Trim() == savedCode.Trim())
            {
                Logger.LogInformation($"CheckSMSCodeAjax: Code matched for phone {savedPhone}");

                // Очищаем код после успешной проверки
                HttpContext.Session.Remove("sms_code");
                HttpContext.Session.Remove("sms_phone");

                var devicePbs = scanDevices.DevicesData.PowerBanks.Where(p => p.HostDeviceId == model.StationId).ToList<WebServer.Models.Device.PowerBank>();

                Models.Device.Device device = scanDevices.DevicesData.Devices.GetById(model.StationId);
                if (device == null)
                {
                    Logger.LogWarning($"CheckSMSCodeAjax: Device not found: {model.StationId}");
                    return Json(new { success = false, message = "Station not found" });
                }

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
                {
                    return Json(new { success = false, message = "No powerbanks available" });
                }

                var pbId = scanDevices.PushPowerBank(device.DeviceName, maxChargedSlot, model.PhoneNumber, new List<string> { "Guest" });
                var pbPush = scanDevices.DevicesData.PowerBanks.GetById(pbId);

                if ((pbId > 1000) && (pbPush != null))
                {
                    Set("KeyCharge911", model.PhoneNumber, 1500);

                    // Сохраняем данные в Session
                    HttpContext.Session.SetString("SmsUserId", model.PhoneNumber);
                    HttpContext.Session.SetString("SmsStationId", device.Id.ToString());

                    return Json(new { success = true, redirectUrl = "/User/SmsSuccess" });
                }

                // Повербанк не выдан - возможно занят или ошибка
                return Json(new { success = false, message = "Failed to dispense powerbank. Please try again." });
            }

            Logger.LogWarning($"CheckSMSCodeAjax: Code mismatch - entered '{model.SMSCode?.Trim()}' vs saved '{savedCode.Trim()}'");
            return Json(new { success = false, message = "The SMS code is incorrect" });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SmsSuccess()
        {
            var userId = HttpContext.Session.GetString("SmsUserId") ?? "";
            var stationIdStr = HttpContext.Session.GetString("SmsStationId");
            ulong stationId = 0;
            if (!string.IsNullOrEmpty(stationIdStr))
                ulong.TryParse(stationIdStr, out stationId);

            // Очищаем одноразовые данные
            HttpContext.Session.Remove("SmsTaken");
            HttpContext.Session.Remove("SmsTime");
            HttpContext.Session.Remove("SmsCost");

            // Получаем все повербанки этого пользователя
            var payInfo = await BuildPayInfoAsync(userId, stationId);

            return View("Do", payInfo);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> TakeMorePowerbank([FromBody] TakeMoreRequest request)
        {
            if (request == null || request.StationId == 0)
            {
                return Json(new { success = false, message = "Invalid request" });
            }

            // Получаем userId из cookie
            var userId = Request.Cookies["KeyCharge911"];
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not identified" });
            }

            var device = scanDevices.DevicesData.Devices.GetById(request.StationId);
            if (device == null)
            {
                return Json(new { success = false, message = "Station not found" });
            }

            // Проверяем, разрешен ли MultiTake
            if (device.TypeOfUse != TypeOfUse.FreeMultiTake && device.TypeOfUse != TypeOfUse.SMSTake)
            {
                return Json(new { success = false, message = "Multiple powerbanks not allowed for this station" });
            }

            // Проверяем сколько уже взято (максимум 4)
            var userPbs = scanDevices.DevicesData.PowerBanks
                .Where(p => p.UserId == userId && p.Taken)
                .Count();
            if (userPbs >= 4)
            {
                return Json(new { success = false, message = "Maximum 4 powerbanks allowed" });
            }

            // Ищем доступный повербанк
            var devicePbs = scanDevices.DevicesData.PowerBanks
                .Where(p => p.HostDeviceId == device.Id)
                .ToList();

            var maxCharge = 0;
            uint maxChargedSlot = 0;
            foreach (var pb in devicePbs)
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
            {
                return Json(new { success = false, message = "No powerbanks available" });
            }

            var pbId = scanDevices.PushPowerBank(device.DeviceName, maxChargedSlot, userId, new List<string> { "Guest" });
            var pbPush = scanDevices.DevicesData.PowerBanks.GetById(pbId);

            if ((pbId > 1000) && (pbPush != null))
            {
                Logger.LogInformation($"TakeMorePowerbank: User {userId} took additional powerbank {pbId}");
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Failed to dispense powerbank" });
        }

        private async Task<PayInfo> BuildPayInfoAsync(string userId, ulong stationId)
        {
            var payInfo = new PayInfo
            {
                UserId = userId,
                StationId = stationId
            };

            // Загружаем настройки поддержки
            var supportSettings = await _appSettingsService.GetSupportSettingsAsync();
            payInfo.SupportPhone = supportSettings.Phone;
            payInfo.SupportEmail = supportSettings.Email;

            // Получаем все повербанки этого пользователя
            var userPbs = scanDevices.DevicesData.PowerBanks
                .Where(p => p.UserId == userId && p.Taken)
                .ToList();

            if (userPbs.Count > 0)
            {
                payInfo.Taken = 1;
                payInfo.PowerBanks = new List<PowerBankInfo>();

                float totalCost = 0;
                foreach (var pb in userPbs)
                {
                    var device = scanDevices.DevicesData.Devices.GetById(pb.HostDeviceId);
                    var typeOfUse = device?.TypeOfUse ?? TypeOfUse.PayByCard;
                    var cost = _pricingService.CalculateCost(typeOfUse, pb.LastGetTime, pb.Taken);
                    var duration = DateTime.Now - pb.LastGetTime;

                    payInfo.PowerBanks.Add(new PowerBankInfo
                    {
                        PowerBankId = pb.Id_str,
                        PowerBankName = pb.Name ?? pb.Id_str,
                        Time = $"{(int)duration.TotalHours}h {duration.Minutes}m",
                        StartTime = pb.LastGetTime.ToString("dd.MM HH:mm"),
                        Cost = cost,
                        StationId = pb.HostDeviceId
                    });

                    totalCost += cost;

                    // Сохраняем StationId первого повербанка для кнопки "Take More"
                    if (payInfo.StationId == 0)
                        payInfo.StationId = pb.HostDeviceId;
                }

                payInfo.Cost = totalCost;
                payInfo.Time = payInfo.PowerBanks.FirstOrDefault()?.Time ?? "-";

                // Определяем, можно ли взять ещё
                var device2 = scanDevices.DevicesData.Devices.GetById(payInfo.StationId);
                if (device2 != null)
                {
                    payInfo.TypeOfUse = device2.TypeOfUse.ToString();

                    // Проверяем есть ли доступные повербанки на станции
                    var availablePbs = scanDevices.DevicesData.PowerBanks
                        .Where(p => p.HostDeviceId == payInfo.StationId && p.Plugged && !p.Taken && !p.Reserved)
                        .Any();

                    payInfo.CanTakeMore = (device2.TypeOfUse == TypeOfUse.FreeMultiTake || device2.TypeOfUse == TypeOfUse.SMSTake)
                                          && userPbs.Count < 4
                                          && availablePbs;

                    // Получаем язык и цвет из зоны станции
                    await LoadZoneSettingsAsync(payInfo, device2.UserLocation);
                }
            }

            return payInfo;
        }

        private async Task LoadZoneSettingsAsync(PayInfo payInfo, string userLocation)
        {
            if (string.IsNullOrEmpty(userLocation))
                return;

            var zones = await _appSettingsService.GetZonesAsync();
            if (int.TryParse(userLocation, out var zoneId))
            {
                var zone = zones.FirstOrDefault(z => z.Id == zoneId);
                if (zone != null)
                {
                    payInfo.Language = zone.Language;
                    payInfo.ZoneColor = zone.Color;
                }
            }
        }

        public class TakeMoreRequest
        {
            public ulong StationId { get; set; }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult CheckPowerBankStatus()
        {
            try
            {
                var userId = Request.Cookies["KeyCharge911"];
                if (string.IsNullOrEmpty(userId))
                {
                    userId = HttpContext.Session.GetString("SmsUserId");
                }

                Logger.LogInformation("CheckPowerBankStatus: userId={UserId}", userId ?? "null");

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not identified" });
                }

                var userPbs = scanDevices.DevicesData.PowerBanks
                    .Where(p => p.UserId == userId && p.Taken)
                    .ToList();

                Logger.LogInformation("CheckPowerBankStatus: Found {Count} powerbanks for userId={UserId}", userPbs.Count, userId);

                var powerBanks = new List<object>();
                float totalCost = 0;

                foreach (var pb in userPbs)
                {
                    var device = scanDevices.DevicesData.Devices.GetById(pb.HostDeviceId);
                    var typeOfUse = device?.TypeOfUse ?? TypeOfUse.PayByCard;
                    var cost = _pricingService.CalculateCost(typeOfUse, pb.LastGetTime, pb.Taken);
                    var duration = DateTime.Now - pb.LastGetTime;

                    powerBanks.Add(new
                    {
                        powerBankId = pb.Id_str,
                        powerBankName = pb.Name ?? pb.Id_str,
                        time = $"{(int)duration.TotalHours}h {duration.Minutes}m",
                        cost = cost
                    });

                    totalCost += cost;
                }

                return Json(new
                {
                    success = true,
                    count = powerBanks.Count,
                    powerBanks = powerBanks,
                    totalCost = totalCost
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CheckPowerBankStatus error");
                return Json(new { success = false, message = ex.Message });
            }
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
                await Task.Delay(100);
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

            // Event handler callback - no action needed
        }

    }
}