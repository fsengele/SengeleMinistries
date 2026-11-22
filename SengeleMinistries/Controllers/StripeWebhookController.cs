
using Microsoft.AspNetCore.Mvc;
using Stripe;
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

        public StripeWebhookController(
            IConfiguration configuration,
            ILogger<StripeWebhookController> logger)
        {
            _configuration = configuration;
            _logger = logger;
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
            catch (System.Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid Stripe webhook payload.");

                return BadRequest();
            }

            // ==========================================
            // DONATION PAYMENT SUCCEEDED
            // ==========================================
            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                if (stripeEvent.Data.Object is PaymentIntent payment)
                {
                    bool isDonation =
                        payment.Metadata != null &&
                        payment.Metadata.TryGetValue(
                            "PaymentType",
                            out var paymentType) &&
                        paymentType == "Donation" &&
                        payment.Metadata.TryGetValue(
                            "Organization",
                            out var organization) &&
                        organization == "Sengele Ministries";

                    bool paymentSucceeded =
                        payment.Status == "succeeded" &&
                        payment.AmountReceived == payment.Amount &&
                        payment.Currency == "usd";

                    if (isDonation && paymentSucceeded)
                    {
                        _logger.LogInformation(
                            "Stripe donation confirmed. PaymentIntent: {PaymentIntentId}, Amount: {AmountInCents} cents.",
                            payment.Id,
                            payment.AmountReceived
                        );
                    }
                }
            }

            // Acknowledge valid Stripe events.
            return Ok();
        }
    }
}
