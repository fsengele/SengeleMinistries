
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Globalization;

namespace SengeleMinistries.Controllers
{
    [Route("StripePayment")]
    public class StripePaymentController : Controller
    {
        private readonly IConfiguration _configuration;

        public StripePaymentController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ==========================================
        // CREATE DONATION PAYMENT INTENT
        // ==========================================
        [HttpPost("CreateDonationIntent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDonationIntent(
            [FromForm] string? amount)
        {
            if (!decimal.TryParse(
                    amount,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out decimal donationAmount))
            {
                return BadRequest(new
                {
                    error = "Invalid donation amount."
                });
            }

            // Accept donations between $1 and $10,000
            // Maximum two decimal places
            if (donationAmount < 1 ||
                donationAmount > 10000 ||
                decimal.Round(donationAmount, 2) != donationAmount)
            {
                return BadRequest(new
                {
                    error = "Please enter an amount between $1 and $10,000 with no more than two decimal places."
                });
            }

            // Convert dollars to cents
            long amountInCents = (long)(donationAmount * 100);

            var secretKey = _configuration["Stripe:SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                return StatusCode(503, new
                {
                    error = "Stripe is not configured."
                });
            }

            try
            {
                var service = new PaymentIntentService();

                var options = new PaymentIntentCreateOptions
                {
                    Amount = amountInCents,
                    Currency = "usd",

                    AutomaticPaymentMethods =
                        new PaymentIntentAutomaticPaymentMethodsOptions
                        {
                            Enabled = true
                        },

                    Description = "Donation to Sengele Ministries",

                    Metadata = new Dictionary<string, string>
                    {
                        { "PaymentType", "Donation" },
                        { "Organization", "Sengele Ministries" }
                    }
                };

                var requestOptions = new RequestOptions
                {
                    ApiKey = secretKey
                };

                var paymentIntent = await service.CreateAsync(
                    options,
                    requestOptions);

                return Ok(new
                {
                    clientSecret = paymentIntent.ClientSecret
                });
            }
            catch (StripeException)
            {
                return StatusCode(502, new
                {
                    error = "Unable to start the payment. Please try again."
                });
            }
        }

        // ==========================================
        // VERIFY DONATION PAYMENT STATUS
        // ==========================================
        [HttpPost("VerifyDonation")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyDonation(
            [FromForm] string? paymentIntentId)
        {
            if (string.IsNullOrWhiteSpace(paymentIntentId) ||
                !paymentIntentId.StartsWith(
                    "pi_",
                    StringComparison.Ordinal))
            {
                return BadRequest(new
                {
                    error = "Invalid payment reference."
                });
            }

            var secretKey = _configuration["Stripe:SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                return StatusCode(503, new
                {
                    error = "Stripe is not configured."
                });
            }

            try
            {
                var service = new PaymentIntentService();

                var requestOptions = new RequestOptions
                {
                    ApiKey = secretKey
                };

                var paymentIntent = await service.GetAsync(
                    paymentIntentId,
                    null,
                    requestOptions);

                // Make sure this is a donation
                // created for Sengele Ministries.
                bool isDonation =
                    paymentIntent.Metadata != null &&
                    paymentIntent.Metadata.TryGetValue(
                        "PaymentType",
                        out var paymentType) &&
                    paymentType == "Donation" &&
                    paymentIntent.Metadata.TryGetValue(
                        "Organization",
                        out var organization) &&
                    organization == "Sengele Ministries";

                if (!isDonation)
                {
                    return BadRequest(new
                    {
                        error = "This payment is not a valid donation."
                    });
                }

                bool succeeded =
                    paymentIntent.Status == "succeeded" &&
                    paymentIntent.AmountReceived ==
                        paymentIntent.Amount &&
                    paymentIntent.Currency == "usd";

                return Ok(new
                {
                    succeeded = succeeded,
                    status = paymentIntent.Status,
                    amount = paymentIntent.Amount / 100m,
                    currency = paymentIntent.Currency
                });
            }
            catch (StripeException)
            {
                return StatusCode(502, new
                {
                    error = "Unable to verify the donation. Please try again."
                });
            }
        }
    }
}
