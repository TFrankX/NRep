using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using WebServer.Models.Stripe;
using WebServer.Models.Action;
using WebServer.Models.Device;
using WebServer.Models.Finance;

namespace WebServer.Services.Stripe
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
        private readonly PaymentIntentService _paymentIntentService;
        private readonly SessionService _sessionService;
        private readonly RefundService _refundService;
        private readonly ActionProcess _actionProcess;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StripeRoutines> _logger;

        public StripeRoutines(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<StripeRoutines> logger)
        {
            StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];

            _paymentIntentService = new PaymentIntentService();
            _sessionService = new SessionService();
            _refundService = new RefundService();
            _actionProcess = new ActionProcess(scopeFactory);
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public static float CostCalculate(bool pbTaken, DateTime pbLastGetTime, float pbPrice)
        {
            var diff = DateTime.Now - pbLastGetTime;

            var calculateCost = 2 + Math.Round(diff.Hours * pbPrice / 6);

            if (diff.Hours == 0 && diff.Minutes < 1 && diff.Days == 0)
                calculateCost = 0;

            calculateCost = calculateCost > pbPrice ? pbPrice : calculateCost;

            return (float)Math.Round(pbTaken ? calculateCost : pbPrice);
        }

        public Session MakePayment(StripeCheckout model, HttpRequest request)
        {
            var options = CreateCheckoutOptions(model, request, false);
            return _sessionService.Create(options);
        }

        public Session HoldPayment(StripeCheckout model, HttpRequest request)
        {
            var options = CreateCheckoutOptions(model, request, true);
            var session = _sessionService.Create(options);

            // Log payment hold
            var paymentInfo = $"SessionId: {session.Id}, Currency: {model.Currency}";
            _actionProcess.ActionSavePayment(
                (int)ActionsDescription.PaymentHold,
                session.Id,
                model.StationId,
                model.PowerBankId,
                model.Amount,
                paymentInfo
            );

            return session;
        }

        private SessionCreateOptions CreateCheckoutOptions(
            StripeCheckout model,
            HttpRequest request,
            bool manualCapture)
        {
            return new SessionCreateOptions
            {
                PaymentIntentData = manualCapture
                    ? new SessionPaymentIntentDataOptions
                    {
                        CaptureMethod = "manual"
                    }
                    : null,

                PaymentMethodTypes = new List<string> { "card" },

                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = model.Currency,
                            UnitAmount = (long)(model.Amount * 100),

                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = model.ProductName,
                                Description = model.ProductDescription
                            }
                        },
                        Quantity = 1
                    }
                },

                Mode = "payment",
                CustomerCreation = "always",

                SuccessUrl =
                    $"{request.Scheme}://{request.Host}/checkout/success?stationId={model.StationId}&powerBankId={model.PowerBankId}&session_id={{CHECKOUT_SESSION_ID}}",

                CancelUrl =
                    $"{request.Scheme}://{request.Host}/checkout/cancel"
            };
        }

        public Refund RefundPayment(RefundRequest model)
        {
            var session = _sessionService.Get(model.SessionId)
                         ?? throw new Exception("Session not found.");

            if (string.IsNullOrEmpty(session.PaymentIntentId))
                throw new Exception("PaymentIntent not found.");

            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = session.PaymentIntentId,
                Amount = (long)(model.Amount * 100)
            };

            var refund = _refundService.Create(refundOptions);

            // Log refund
            var paymentInfo = BuildPaymentInfo(session, null);
            _actionProcess.ActionSavePayment(
                (int)ActionsDescription.PaymentRefund,
                session.CustomerDetails?.Name ?? session.Id,
                model.StationId,
                model.PowerBankId,
                model.Amount,
                paymentInfo
            );

            // Save to FinancialTransactions table
            SaveFinancialTransaction(
                TransactionType.Refund,
                (decimal)model.Amount,
                model.StationId ?? 0,
                model.StationName ?? "",
                model.PowerBankId ?? 0,
                session.CustomerDetails?.Email ?? "",
                session.CustomerDetails?.Name ?? "",
                session.PaymentIntentId ?? "",
                model.SessionId ?? "",
                "",
                "",
                "",
                $"Refund: {paymentInfo}"
            );

            return refund;
        }

        public PaymentIntent MakePostPayment(StripeCapture model)
        {
            if (string.IsNullOrEmpty(model.SessionId))
                throw new ArgumentException("SessionId is required.");

            try
            {
                var session = _sessionService.Get(model.SessionId)
                             ?? throw new Exception("Session not found.");

                var paymentIntentId = session.PaymentIntentId;

                if (string.IsNullOrEmpty(paymentIntentId))
                    throw new Exception("PaymentIntent not found.");

                var intent = _paymentIntentService.Get(paymentIntentId);

                // capture возможен только в requires_capture
                if (intent.Status != "requires_capture")
                    return intent;

                var captureOptions = new PaymentIntentCaptureOptions
                {
                    AmountToCapture = (long)(model.Amount * 100)
                };

                var capturedIntent = _paymentIntentService.Capture(paymentIntentId, captureOptions);

                // Log payment capture
                var chargeService = new ChargeService();
                var charges = chargeService.List(new ChargeListOptions { PaymentIntent = paymentIntentId, Limit = 1 });
                var charge = charges?.FirstOrDefault();

                var paymentInfo = BuildPaymentInfo(session, charge);
                _actionProcess.ActionSavePayment(
                    (int)ActionsDescription.PaymentCapture,
                    session.CustomerDetails?.Name ?? session.Id,
                    model.StationId,
                    model.PowerBankId,
                    model.Amount,
                    paymentInfo
                );

                // Save to FinancialTransactions table
                SaveFinancialTransaction(
                    TransactionType.Capture,
                    (decimal)model.Amount,
                    model.StationId ?? 0,
                    model.StationName ?? "",
                    model.PowerBankId ?? 0,
                    session.CustomerDetails?.Email ?? "",
                    session.CustomerDetails?.Name ?? "",
                    paymentIntentId ?? "",
                    model.SessionId ?? "",
                    BuildCardInfo(charge),
                    BuildCardExpiry(charge),
                    BuildCardCountry(charge),
                    paymentInfo
                );

                return capturedIntent;
            }
            catch (StripeException ex)
            {
                // Log payment failure
                _actionProcess.ActionSavePayment(
                    (int)ActionsDescription.PaymentFailed,
                    model.SessionId,
                    model.StationId,
                    model.PowerBankId,
                    model.Amount,
                    $"Error: {ex.Message}"
                );

                if (ex.StripeError?.Code == "payment_intent_unexpected_state")
                {
                    var session = _sessionService.Get(model.SessionId);
                    return _paymentIntentService.Get(session.PaymentIntentId);
                }

                throw;
            }
        }

        public PaymentIntent ReleaseHeldPayment(StripeCapture model)
        {
            if (string.IsNullOrEmpty(model.SessionId))
                throw new ArgumentException("SessionId is required.");

            try
            {
                var session = _sessionService.Get(model.SessionId)
                             ?? throw new Exception("Session not found.");

                var paymentIntentId = session.PaymentIntentId;

                if (string.IsNullOrEmpty(paymentIntentId))
                    throw new Exception("PaymentIntent not found.");

                var intent = _paymentIntentService.Get(paymentIntentId);

                if (intent.Status == "canceled" || intent.Status == "succeeded")
                    return intent;

                var canceledIntent = _paymentIntentService.Cancel(paymentIntentId);

                // Log payment release
                var paymentInfo = BuildPaymentInfo(session, null);
                _actionProcess.ActionSavePayment(
                    (int)ActionsDescription.PaymentRelease,
                    session.CustomerDetails?.Name ?? session.Id,
                    model.StationId,
                    model.PowerBankId,
                    model.Amount,
                    paymentInfo
                );

                // Save to FinancialTransactions table
                SaveFinancialTransaction(
                    TransactionType.Release,
                    (decimal)model.Amount,
                    model.StationId ?? 0,
                    model.StationName ?? "",
                    model.PowerBankId ?? 0,
                    session.CustomerDetails?.Email ?? "",
                    session.CustomerDetails?.Name ?? "",
                    paymentIntentId ?? "",
                    model.SessionId ?? "",
                    "",
                    "",
                    "",
                    "Hold released"
                );

                return canceledIntent;
            }
            catch (StripeException ex)
            {
                // Log payment failure
                _actionProcess.ActionSavePayment(
                    (int)ActionsDescription.PaymentFailed,
                    model.SessionId,
                    model.StationId,
                    model.PowerBankId,
                    model.Amount,
                    $"Release error: {ex.Message}"
                );
                throw;
            }
        }

        private string BuildPaymentInfo(Session session, Charge? charge)
        {
            var parts = new List<string>();

            // Сначала данные о плательщике
            if (session.CustomerDetails != null)
            {
                if (!string.IsNullOrEmpty(session.CustomerDetails.Name))
                    parts.Add($"Name: {session.CustomerDetails.Name}");
                if (!string.IsNullOrEmpty(session.CustomerDetails.Email))
                    parts.Add($"Email: {session.CustomerDetails.Email}");
            }

            // Затем данные о карте
            if (charge?.PaymentMethodDetails?.Card != null)
            {
                var card = charge.PaymentMethodDetails.Card;
                parts.Add($"Card: {card.Brand} *{card.Last4}");
                parts.Add($"Exp: {card.ExpMonth:D2}/{card.ExpYear}");
                if (!string.IsNullOrEmpty(card.Country))
                    parts.Add($"Country: {card.Country}");
            }

            // SessionId в конце
            parts.Add($"SessionId: {session.Id}");

            return string.Join(", ", parts);
        }

        private string BuildCardInfo(Charge? charge)
        {
            if (charge?.PaymentMethodDetails?.Card == null)
                return "";

            var card = charge.PaymentMethodDetails.Card;
            return $"{card.Brand} *{card.Last4}";
        }

        private string BuildCardExpiry(Charge? charge)
        {
            if (charge?.PaymentMethodDetails?.Card == null)
                return "";

            var card = charge.PaymentMethodDetails.Card;
            return $"{card.ExpMonth:D2}/{card.ExpYear}";
        }

        private string BuildCardCountry(Charge? charge)
        {
            return charge?.PaymentMethodDetails?.Card?.Country ?? "";
        }

        private void SaveFinancialTransaction(
            TransactionType type,
            decimal amount,
            ulong stationId,
            string stationName,
            ulong powerBankId,
            string userId,
            string customerName,
            string paymentReference,
            string sessionId,
            string cardInfo,
            string cardExpiry,
            string cardCountry,
            string description)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();

                var transaction = new FinancialTransaction
                {
                    TransactionTime = DateTime.Now,
                    Type = type,
                    Amount = amount,
                    StationId = stationId,
                    StationName = stationName,
                    PowerBankId = powerBankId,
                    UserId = userId,
                    CustomerName = customerName,
                    PaymentReference = paymentReference,
                    SessionId = sessionId,
                    CardInfo = cardInfo,
                    CardExpiry = cardExpiry,
                    CardCountry = cardCountry,
                    Description = description
                };

                db.FinancialTransactions.Add(transaction);
                db.SaveChanges();

                _logger.LogInformation("Saved financial transaction: Type={Type}, Amount={Amount}, Station={Station}",
                    type, amount, stationName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save financial transaction");
            }
        }
    }
}