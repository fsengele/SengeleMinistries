
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Data;
using Stripe;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SengeleMinistries.Controllers
{
    [ApiController]
    [Route("StripeWebhook")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeWebhookController> _logger;
        private readonly ApplicationDbContext _context;

        public StripeWebhookController(
            IConfiguration configuration,
            ILogger<StripeWebhookController> logger,
            ApplicationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        // ==========================================
        // RECEIVE STRIPE WEBHOOK EVENTS
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var webhookSecret =
                _configuration["Stripe:WebhookSecret"];

            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogError(
                    "Stripe webhook secret is not configured.");

                return StatusCode(503);
            }

            string json;

            using (var reader = new StreamReader(Request.Body))
            {
                json = await reader.ReadToEndAsync();
            }

            var signature =
                Request.Headers["Stripe-Signature"].ToString();

            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signature,
                    webhookSecret
                );
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid Stripe webhook signature.");

                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid Stripe webhook payload.");

                return BadRequest();
            }

            // ==========================================
            // PAYMENT SUCCEEDED
            // ==========================================
            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                if (stripeEvent.Data.Object is PaymentIntent payment)
                {
                    bool paymentSucceeded =
                        payment.Status == "succeeded" &&
                        payment.AmountReceived == payment.Amount &&
                        payment.Currency == "usd";

                    if (!paymentSucceeded)
                    {
                        _logger.LogWarning(
                            "Stripe payment validation failed: {PaymentIntentId}",
                            payment.Id);

                        return Ok();
                    }

                    // ==================================
                    // DONATION PAYMENT
                    // ==================================
                    bool isDonation =
                        payment.Metadata != null &&
                        payment.Metadata.TryGetValue(
                            "PaymentType",
                            out var donationType) &&
                        donationType == "Donation" &&
                        payment.Metadata.TryGetValue(
                            "Organization",
                            out var organization) &&
                        organization == "Sengele Ministries";

                    if (isDonation)
                    {
                        _logger.LogInformation(
                            "Stripe donation confirmed. PaymentIntent: {PaymentIntentId}, Amount: {AmountInCents} cents.",
                            payment.Id,
                            payment.AmountReceived);

                        return Ok();
                    }

                    // ==================================
                    // SHOP ORDER PAYMENT
                    // ==================================

                    // Initialize orderId before validation
                    // to prevent CS0165 error.
                    int orderId = 0;

                    bool isShopOrder =
                        payment.Metadata != null &&
                        payment.Metadata.TryGetValue(
                            "PaymentType",
                            out var shopType) &&
                        shopType == "ShopOrder" &&
                        payment.Metadata.TryGetValue(
                            "OrderId",
                            out var orderIdText) &&
                        int.TryParse(orderIdText, out orderId);

                    if (!isShopOrder)
                    {
                        _logger.LogInformation(
                            "Stripe payment event ignored: {PaymentIntentId}",
                            payment.Id);

                        return Ok();
                    }

                    // ==================================
                    // FIND ORDER IN SQL SERVER
                    // ==================================
                    var order = await _context.Orders
                        .FirstOrDefaultAsync(o =>
                            o.OrderId == orderId);

                    if (order == null)
                    {
                        _logger.LogWarning(
                            "Stripe payment received for unknown order: {OrderId}",
                            orderId);

                        return StatusCode(503);
                    }

                    // ==================================
                    // VERIFY PAYMENT INTENT ID
                    // ==================================
                    if (order.StripePaymentIntentId != payment.Id)
                    {
                        _logger.LogWarning(
                            "Stripe PaymentIntent mismatch for order {OrderId}",
                            order.OrderId);

                        return StatusCode(503);
                    }

                    // ==================================
                    // VERIFY EXACT ORDER AMOUNT
                    // ==================================
                    long expectedAmountInCents =
                        (long)(order.Total * 100m);

                    if (order.Total <= 0 ||
                        order.Total != decimal.Round(order.Total, 2) ||
                        payment.Amount != expectedAmountInCents)
                    {
                        _logger.LogWarning(
                            "Stripe amount mismatch for order {OrderId}",
                            order.OrderId);

                        return StatusCode(503);
                    }

                    // ==================================
                    // PREVENT DUPLICATE PROCESSING
                    // ==================================
                    if (order.PaymentStatus == "Paid")
                    {
                        _logger.LogInformation(
                            "Order {OrderId} already marked as paid.",
                            order.OrderId);

                        return Ok();
                    }

                    // ==================================
                    // MARK ORDER AS PAID
                    // ==================================
                    order.PaymentStatus = "Paid";
                    order.PaymentMethod = "Stripe";
                    order.PaidAt = DateTime.UtcNow;
                    order.OrderStatus = "Processing";

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Stripe payment confirmed for order {OrderId}. PaymentIntent: {PaymentIntentId}",
                        order.OrderId,
                        payment.Id);
                }
            }

            // ==========================================
            // ACKNOWLEDGE VALID STRIPE EVENTS
            // ==========================================
            return Ok();
        }
    }
}
