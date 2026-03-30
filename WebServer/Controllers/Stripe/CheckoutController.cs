using System;
using Stripe.Checkout;
using Stripe;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models.Stripe;
using WebServer.Models.Action;

namespace WebServer.Controllers.Stripe
{
    [ApiController]
    [Route("[controller]")]
    public class CheckoutController : Controller
    {
        private readonly ILogger<CheckoutController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ActionProcess _actionProcess;

        public CheckoutController(IConfiguration configuration, ILogger<CheckoutController> logger, IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _actionProcess = new ActionProcess(scopeFactory);
        }

        //[HttpPost("make-payment")]
        //public IActionResult MakePayment([FromBody] StripeCheckout model)
        //{
        //    StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        //    var options = new SessionCreateOptions
        //    {

        //        PaymentMethodTypes = new List<string> { "card" },
        //        LineItems = new List<SessionLineItemOptions>
        //        {
        //            new SessionLineItemOptions
        //            {
        //                PriceData = new SessionLineItemPriceDataOptions
        //                {
        //                    Currency = model.Currency,
        //                    ProductData = new SessionLineItemPriceDataProductDataOptions
        //                    {
        //                        Name = model.ProductName,
        //                        Description = model.ProductDescription,
        //                    },
        //                    UnitAmount = (long)(model.Amount*100),
        //                },
        //                Quantity = 1,
        //            },
        //        },
        //        Mode = "payment",
        //        BillingAddressCollection = "required",
        //        CustomerCreation = "always",
        //        SuccessUrl = $"{Request.Scheme}://{Request.Host}/checkout/success?stationId={model.StationId}&powerBankId={model.PowerBankId}&session_id={{CHECKOUT_SESSION_ID}}",
        //        CancelUrl = $"{Request.Scheme}://{Request.Host}/checkout/cancel",
        //    };

        //    var service = new SessionService();
        //    var session = service.Create(options);
        //    return Ok(new { sessionId = session.Id });
        //}



        //[HttpPost("hold-payment")]
        //public IActionResult HoldPayment([FromBody] StripeCheckout model)
        //{
        //    StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        //    var options = new SessionCreateOptions
        //    {
        //        PaymentIntentData = new SessionPaymentIntentDataOptions
        //        {
        //            CaptureMethod = "manual"
        //        },
        //        PaymentMethodTypes = new List<string> { "card" },
        //        LineItems = new List<SessionLineItemOptions>
        //        {
        //            new SessionLineItemOptions
        //            {
        //                PriceData = new SessionLineItemPriceDataOptions
        //                {
        //                    Currency = "eur",
        //                    ProductData = new SessionLineItemPriceDataProductDataOptions
        //                    {
        //                        Name = "Charging",
        //                        Description = "Charging with a-charge",
        //                    },
        //                    UnitAmount = 1000,
        //                },
        //                Quantity = 1,
        //            },
        //        },
        //        Mode = "payment",
        //        SuccessUrl = $"{Request.Scheme}://{Request.Host}/checkout/success?stationId={model.StationId}&powerBankId={model.PowerBankId}&session_id={{CHECKOUT_SESSION_ID}}",
        //        CancelUrl = $"{Request.Scheme}://{Request.Host}/checkout/cancel",
        //    };

        //    var service = new SessionService();
        //    var session = service.Create(options);
        //    model.SessionId = session.Id;
        //    //return Ok();
        //    return Ok(new { sessionId = session.Id });
        //}


        //[HttpPost("refund")]
        //public IActionResult RefundPayment([FromBody] RefundRequest model)
        //{
        //    try
        //    {
        //        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

        //        // 1️⃣ Получаем сессию по SessionId
        //        var sessionService = new SessionService();
        //        var session = sessionService.Get(model.SessionId);

        //        if (session == null)
        //            return NotFound(new { message = "Session not found." });

        //        // 2️⃣ Извлекаем PaymentIntent из сессии
        //        var paymentIntentId = session.PaymentIntentId;

        //        if (string.IsNullOrEmpty(paymentIntentId))
        //            return BadRequest(new { message = "PaymentIntent not found for this session." });

        //        // 3️⃣ Создаём возврат
        //        var refundOptions = new RefundCreateOptions
        //        {
        //            PaymentIntent = paymentIntentId,
        //            Amount = (long)(model.Amount * 100),
        //            Reason = "Return an exchange", // можно указать custom reason
        //        };

        //        var refundService = new RefundService();
        //        var refund = refundService.Create(refundOptions);

        //        // 4️⃣ Возвращаем результат
        //        return Ok(new
        //        {
        //            success = true,
        //            refundId = refund.Id,
        //            status = refund.Status
        //        });
        //    }
        //    catch (StripeException ex)
        //    {
        //        return BadRequest(new { message = ex.StripeError?.Message });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = ex.Message });
        //    }
        //}



        ////[HttpPost("hold-payment")]
        ////public IActionResult HoldPayment([FromBody] StripeCheckout model)
        ////{

        ////    //var sesOptions = new SessionCreateOptions();



        ////    StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];


        ////    var options = new PaymentIntentCreateOptions
        ////    {
        ////        Amount = 1099,
        ////        Currency = "eur",
        ////        AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
        ////        {
        ////            Enabled = true,
        ////        },
        ////        PaymentMethodOptions = new PaymentIntentPaymentMethodOptionsOptions
        ////        {
        ////            Card = new PaymentIntentPaymentMethodOptionsCardOptions
        ////            {
        ////                CaptureMethod = "manual",
        ////            },
        ////        },
        ////        Confirm = true,
        ////        ReturnUrl = $"{Request.Scheme}://{Request.Host}/checkout/success",
        ////    };

        ////    var service = new PaymentIntentService();
        ////    PaymentIntent paymentIntent = service.Create(options);
        ////    return Ok(new { sessionId = paymentIntent.Id });
        ////}







        //[HttpPost("make-post-payment")]
        //public IActionResult MakePostPayment([FromBody] StripeCapture model)
        //{
        //    if (string.IsNullOrEmpty(model.SessionId))
        //        throw new ArgumentException("SessionId is required.");

        //    StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

        //    // Получаем сессию по sessionId
        //    var sessionService = new SessionService();
        //    var session = sessionService.Get(model.SessionId);

        //    if (session == null)
        //        throw new Exception("Session not found.");

        //    // Извлекаем PaymentIntent из сессии
        //    var paymentIntentId = session.PaymentIntentId;

        //    if (string.IsNullOrEmpty(paymentIntentId))
        //        throw new Exception("PaymentIntent not found for this session.");

        //    // Захватываем корректированную сумму
        //    var paymentIntentService = new PaymentIntentService();
        //    var captureOptions = new PaymentIntentCaptureOptions
        //    {
        //        AmountToCapture = (long)(model.Amount * 100) // конвертируем в центы
        //    };

        //    var capturedPayment = paymentIntentService.Capture(paymentIntentId, captureOptions);

        //    return capturedPayment;
        //}





        [HttpGet("success")]
        public IActionResult Success([FromQuery] string session_id, [FromQuery] ulong? stationId, [FromQuery] ulong? powerBankId)
        {
            if (!string.IsNullOrEmpty(session_id))
            {
                try
                {
                    StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

                    // Get session with expanded customer and payment intent
                    var sessionService = new SessionService();
                    var session = sessionService.Get(session_id, new SessionGetOptions
                    {
                        Expand = new List<string> { "customer", "payment_intent", "payment_intent.payment_method" }
                    });

                    string customerName = "Unknown";
                    string paymentInfo = $"SessionId: {session_id}";

                    // Get customer name
                    if (session.Customer != null)
                    {
                        customerName = session.Customer.Name ?? session.Customer.Email ?? "Unknown";
                    }
                    else if (session.CustomerDetails != null)
                    {
                        customerName = session.CustomerDetails.Name ?? session.CustomerDetails.Email ?? "Unknown";
                    }

                    // Get card details from payment method
                    if (session.PaymentIntent?.PaymentMethod?.Card != null)
                    {
                        var card = session.PaymentIntent.PaymentMethod.Card;
                        paymentInfo = $"{card.Brand?.ToUpper()} *{card.Last4}, Exp: {card.ExpMonth:D2}/{card.ExpYear}";

                        // Add billing name if available
                        var billingName = session.PaymentIntent.PaymentMethod.BillingDetails?.Name;
                        if (!string.IsNullOrEmpty(billingName))
                        {
                            customerName = billingName;
                        }
                    }

                    // Update the action record
                    _actionProcess.UpdatePaymentInfo(session_id, customerName, paymentInfo);

                    _logger.LogInformation("Payment success: SessionId={SessionId}, Customer={Customer}, Card={Card}",
                        session_id, customerName, paymentInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting payment details for session {SessionId}", session_id);
                }
            }

            return View();
        }

        [HttpGet("cancel")]
        public IActionResult Cancel()
        {
            return View();
        }

    }
}

