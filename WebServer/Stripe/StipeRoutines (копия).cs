using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using WebServer.Models.Stripe;

namespace WebServer.Services.Stripe_bak
{




    public interface IStripeRoutines
    {

        Session MakePayment(StripeCheckout model, HttpRequest request);
        Session HoldPayment(StripeCheckout model, HttpRequest request);
        Refund RefundPayment(RefundRequest model);
        PaymentIntent MakePostPayment(StripeCapture model);
        PaymentIntent ReleaseHeldPayment(StripeCapture model);
    }

    


    public class StripeRoutines : IStripeRoutines
    {
        private readonly IConfiguration _configuration;

        public StripeRoutines(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static float CostCalculate(bool pbTaken, DateTime pbLastGetTime, float pbPrice)
        {
            var calculateCost = (2 + Math.Round(((DateTime.Now - pbLastGetTime).Hours) * pbPrice / 6));
            if ((DateTime.Now - pbLastGetTime).Hours == 0 && (DateTime.Now - pbLastGetTime).Minutes < 1 && (DateTime.Now - pbLastGetTime).Days == 0)
            {
                calculateCost = 0;
            }
            calculateCost=(calculateCost > pbPrice) ? pbPrice : calculateCost;
            return (float)Math.Round(pbTaken ? calculateCost : pbPrice);
        }

        public Session MakePayment(StripeCheckout model, HttpRequest request)
        {
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                //PaymentMethodOptions = new SessionPaymentMethodOptionsOptions
                //{
                //    Card = new SessionPaymentMethodOptionsCardOptions
                //    {
                //        Wallets = new SessionPaymentMethodOptionsCardWalletsOptions
                //        {
                //            GooglePay = new SessionPaymentMethodOptionsCardWalletsGooglePayOptions
                //            {
                //                Enabled = true
                //            },
                //            ApplePay = new SessionPaymentMethodOptionsCardWalletsApplePayOptions
                //            {
                //                Enabled = true
                //            }
                //        }
                //    }
                //},
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = model.Currency,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = model.ProductName,
                                Description = model.ProductDescription,
                            },
                            UnitAmount = (long)(model.Amount * 100),

                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                //BillingAddressCollection = "required",
                CustomerCreation = "always",
                SuccessUrl = $"{request.Scheme}://{request.Host}/checkout/success?stationId={model.StationId}&powerBankId={model.PowerBankId}&session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{request.Scheme}://{request.Host}/checkout/cancel",
            };

            var service = new SessionService();
            return service.Create(options);
        }

        public Session HoldPayment(StripeCheckout model, HttpRequest request)
        {
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

            var options = new SessionCreateOptions
            {
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    CaptureMethod = "manual"
                },
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = model.Currency,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = model.ProductName,
                                Description = model.ProductDescription,
                            },
                            UnitAmount = (long)(model.Amount * 100),

                        },
                        Quantity = 1,
                    },

                    //new SessionLineItemOptions
                    //{
                    //    PriceData = new SessionLineItemPriceDataOptions
                    //    {
                    //        Currency = "eur",
                    //        ProductData = new SessionLineItemPriceDataProductDataOptions
                    //        {
                    //            Name = "Charging",
                    //            Description = "Charging with a-charge",
                    //        },
                    //        UnitAmount = 1000,
                    //    },
                    //    Quantity = 1,
                    //},
                },
                Mode = "payment",
                CustomerCreation = "always",                
                SuccessUrl = $"{request.Scheme}://{request.Host}/checkout/success?stationId={model.StationId}&powerBankId={model.PowerBankId}&session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{request.Scheme}://{request.Host}/checkout/cancel",
            };

            var service = new SessionService();
            var session = service.Create(options);
            //model.SessionId = session.Id;
            return session;
        }

        public Refund RefundPayment(RefundRequest model)
        {
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

            var sessionService = new SessionService();
            var session = sessionService.Get(model.SessionId);

            if (session == null)
                throw new Exception("Session not found.");

            var paymentIntentId = session.PaymentIntentId;

            if (string.IsNullOrEmpty(paymentIntentId))
                throw new Exception("PaymentIntent not found for this session.");

            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = (long)(model.Amount * 100),                
            };

            var refundService = new RefundService();
            return refundService.Create(refundOptions);
        }


        public PaymentIntent MakePostPayment(StripeCapture model)
        {
            if (string.IsNullOrEmpty(model.SessionId))
                throw new ArgumentException("SessionId is required.");

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

            // Получаем сессию по sessionId
            var sessionService = new SessionService();
            var session = sessionService.Get(model.SessionId);

            if (session == null)
                throw new Exception("Session not found.");

            // Извлекаем PaymentIntent из сессии
            var paymentIntentId = session.PaymentIntentId;

            if (string.IsNullOrEmpty(paymentIntentId))
                throw new Exception("PaymentIntent not found for this session.");

            // Захватываем корректированную сумму
            var paymentIntentService = new PaymentIntentService();
            var captureOptions = new PaymentIntentCaptureOptions
            {
                AmountToCapture = (long)(model.Amount * 100) // конвертируем в центы
            };

            var capturedPayment = paymentIntentService.Capture(paymentIntentId, captureOptions);

            return capturedPayment;
        }

        public PaymentIntent ReleaseHeldPayment(StripeCapture model)
        {
            if (string.IsNullOrEmpty(model.SessionId))
                throw new ArgumentException("SessionId is required.");

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

            var sessionService = new SessionService();
            var session = sessionService.Get(model.SessionId);

            if (session == null)
                throw new Exception("Session not found.");

            var paymentIntentId = session.PaymentIntentId;

            if (string.IsNullOrEmpty(paymentIntentId))
                throw new Exception("PaymentIntent not found for this session.");

            var paymentIntentService = new PaymentIntentService();

            // Отменяем PaymentIntent (средства будут разблокированы)
            var canceledIntent = paymentIntentService.Cancel(paymentIntentId);

            return canceledIntent;
        }

    }
}
