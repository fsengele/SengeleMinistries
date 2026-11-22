
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Data;
using Stripe;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SengeleMinistries.Controllers
{
    [Route("StripePayment")]
    public class StripePaymentController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public StripePaymentController(
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
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

            if (donationAmount < 1 ||
                donationAmount > 10000 ||
                decimal.Round(donationAmount, 2) != donationAmount)
            {
                return BadRequest(new
                {
                    error = "Please enter an amount between $1 and $10,000 with no more than two decimal places."
                });
            }

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

        // ==========================================
        // CREATE SHOP ORDER PAYMENT INTENT
        // ==========================================
        [Authorize]
        [HttpPost("CreateShopOrderIntent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShopOrderIntent(
            [FromForm] int orderId)
        {
            var claim =
                User.FindFirst("MemberId") ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null ||
                !int.TryParse(claim.Value, out int memberId))
            {
                return Unauthorized(new
                {
                    error = "Please sign in before checkout."
                });
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == orderId &&
                    o.MemberId == memberId);

            if (order == null)
            {
                return NotFound(new
                {
                    error = "Order not found."
                });
            }

            if (order.PaymentStatus != "Pending" ||
                order.OrderStatus != "Pending")
            {
                return BadRequest(new
                {
                    error = "This order is not available for payment."
                });
            }

            if (order.OrderItems == null ||
                order.OrderItems.Count == 0)
            {
                return BadRequest(new
                {
                    error = "This order has no products."
                });
            }

            // Verify product eligibility.
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.ProductId == item.ProductId);

                if (product == null ||
                    !product.IsPublished ||
                    product.ProductType == "AmazonAffiliate" ||
                    (!string.IsNullOrWhiteSpace(product.ProductType) &&
                     product.ProductType != "OwnProduct") ||
                    product.RequiresShipping)
                {
                    return BadRequest(new
                    {
                        error = "This order contains a product that cannot be paid for yet."
                    });
                }

                if (item.Quantity <= 0 ||
                    item.Price <= 0 ||
                    item.Price != decimal.Round(item.Price, 2))
                {
                    return BadRequest(new
                    {
                        error = "Invalid order item."
                    });
                }
            }

            // Recalculate the saved order items.
            decimal calculatedSubtotal =
                order.OrderItems.Sum(item =>
                    item.Price * item.Quantity);

            if (calculatedSubtotal != order.Subtotal)
            {
                return BadRequest(new
                {
                    error = "Order subtotal verification failed."
                });
            }

            // Tax calculation is not configured yet.
            // Prevent charging a possibly incomplete total.
            return StatusCode(503, new
            {
                error = "Shop payments are not enabled until applicable taxes are configured."
            });
        }
    }
}
