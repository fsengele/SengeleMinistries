
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using SengeleMinistries.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SengeleMinistries.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public CheckoutController(
            ApplicationDbContext context,
            IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // ==========================================
        // CHECKOUT PAGE
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Member");
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m =>
                    m.Id == memberId.Value);

            if (member == null)
            {
                return RedirectToAction("Login", "Member");
            }

            var cart = await GetCartAsync(memberId.Value);

            if (cart.Count == 0)
            {
                TempData["CartError"] =
                    "Your shopping cart is empty or its products are not available for checkout.";

                return RedirectToAction("Index", "Cart");
            }

            decimal subtotal = cart.Sum(item => item.Total);

            var order = new Order
            {
                MemberId = member.Id,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                Subtotal = subtotal,
                ShippingAmount = 0m,
                TaxAmount = 0m,
                Total = subtotal
            };

            return View(order);
        }

        // ==========================================
        // PLACE ORDER
        // POST: /Checkout/PlaceOrder
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Order model)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Member");
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m =>
                    m.Id == memberId.Value);

            if (member == null)
            {
                return RedirectToAction("Login", "Member");
            }

            // ======================================
            // VALIDATE CUSTOMER INFORMATION
            // ======================================
            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(model.Phone))
            {
                ModelState.AddModelError(
                    nameof(model.Phone),
                    "Phone number is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Address))
            {
                ModelState.AddModelError(
                    nameof(model.Address),
                    "Street address is required.");
            }

            if (string.IsNullOrWhiteSpace(model.City))
            {
                ModelState.AddModelError(
                    nameof(model.City),
                    "City is required.");
            }

            if (string.IsNullOrWhiteSpace(model.State))
            {
                ModelState.AddModelError(
                    nameof(model.State),
                    "State is required.");
            }

            if (string.IsNullOrWhiteSpace(model.ZipCode))
            {
                ModelState.AddModelError(
                    nameof(model.ZipCode),
                    "ZIP code is required.");
            }

            // ======================================
            // LOAD CART FROM SQL SERVER
            // ======================================
            var databaseCartItems =
                await _context.ShoppingCartItems
                    .Where(c => c.MemberId == memberId.Value)
                    .ToListAsync();

            if (databaseCartItems.Count == 0)
            {
                TempData["CartError"] =
                    "Your shopping cart is empty.";

                return RedirectToAction("Index", "Cart");
            }

            // ======================================
            // ALWAYS TRUST DATABASE PRICES
            // ======================================
            decimal subtotal = 0m;

            var orderItems = new List<OrderItem>();

            foreach (var cartItem in databaseCartItems)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.ProductId == cartItem.ProductId &&
                        p.IsPublished);

                if (product == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "A product in your cart is no longer available.");

                    continue;
                }

                // ==================================
                // AMAZON AFFILIATE PROTECTION
                // ==================================
                if (product.ProductType == "AmazonAffiliate")
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"{product.Title} cannot be purchased through this checkout. Please buy it on Amazon.");

                    continue;
                }

                if (!string.IsNullOrWhiteSpace(product.ProductType) &&
                    product.ProductType != "OwnProduct")
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"{product.Title} has an unsupported product type.");

                    continue;
                }

                // ==================================
                // DIGITAL PRODUCTS ONLY FOR NOW
                // ==================================
                if (product.RequiresShipping)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"{product.Title} requires shipping. Physical product checkout is not available yet.");

                    continue;
                }

                // ==================================
                // VALIDATE QUANTITY
                // ==================================
                if (cartItem.Quantity <= 0)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"The quantity for {product.Title} is invalid.");

                    continue;
                }

                // ==================================
                // VALIDATE STOCK
                // ==================================
                if (product.StockQuantity.HasValue &&
                    product.StockQuantity.Value <= 0)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"{product.Title} is currently out of stock.");

                    continue;
                }

                if (product.StockQuantity.HasValue &&
                    cartItem.Quantity > product.StockQuantity.Value)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Only {product.StockQuantity.Value} item(s) of {product.Title} are available.");

                    continue;
                }

                // ==================================
                // VALIDATE PRODUCT PRICE
                // ==================================
                if (product.Price <= 0 ||
                    decimal.Round(product.Price, 2) != product.Price)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"The price for {product.Title} is invalid.");

                    continue;
                }

                subtotal += product.Price * cartItem.Quantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.ProductId,
                    ProductTitle = product.Title,
                    Price = product.Price,
                    Quantity = cartItem.Quantity
                });
            }

            // ======================================
            // VALIDATION ERRORS
            // ======================================
            if (!ModelState.IsValid || orderItems.Count == 0)
            {
                if (orderItems.Count == 0)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "There are no valid products to order.");
                }

                model.MemberId = member.Id;
                model.FirstName = member.FirstName;
                model.LastName = member.LastName;
                model.Email = member.Email;

                model.Subtotal = subtotal;
                model.ShippingAmount = 0m;
                model.TaxAmount = 0m;
                model.Total = subtotal;

                return View("Index", model);
            }

            // ======================================
            // PREPARE ORDER
            // ======================================
            var order = new Order
            {
                MemberId = member.Id,

                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,

                Phone = model.Phone,
                Address = model.Address,
                City = model.City,
                State = model.State,
                ZipCode = model.ZipCode,

                Subtotal = subtotal,

                // Shipping and tax calculations
                // must be finalized before payment.
                ShippingAmount = 0m,
                TaxAmount = 0m,
                Total = subtotal,

                OrderStatus = "Pending",
                PaymentStatus = "Pending",

                StripePaymentIntentId = null,
                PaymentMethod = null,
                PaidAt = null,

                CreatedAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            // ======================================
            // SAVE ORDER TO SQL SERVER
            // ======================================
            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            // ======================================
            // IMPORTANT: KEEP SHOPPING CART
            // ======================================
            // The cart must not be cleared before
            // Stripe confirms successful payment.
            //
            // The Stripe webhook will be responsible
            // for marking the order as paid.
            //
            // Payment integration is not enabled yet.

            // ======================================
            // SEND PENDING ORDER EMAIL
            // ======================================
            try
            {
                var emailBody = new StringBuilder();

                emailBody.Append(
                    "<h2>Sengele Ministries - Order Confirmation</h2>");

                emailBody.Append(
                    $"<p>Dear {WebUtility.HtmlEncode(order.FirstName)},</p>");

                emailBody.Append(
                    "<p>We have received your order request.</p>");

                emailBody.Append(
                    $"<p><strong>Order Number:</strong> #{order.OrderId}</p>");

                emailBody.Append(
                    $"<p><strong>Order Date:</strong> {order.CreatedAt:MMMM dd, yyyy} (UTC)</p>");

                emailBody.Append("<h3>Order Items</h3>");
                emailBody.Append("<ul>");

                foreach (var item in orderItems)
                {
                    emailBody.Append(
                        $"<li>{WebUtility.HtmlEncode(item.ProductTitle)} - " +
                        $"Quantity: {item.Quantity} - " +
                        $"${item.Total:0.00}</li>");
                }

                emailBody.Append("</ul>");

                emailBody.Append(
                    $"<p><strong>Subtotal:</strong> ${order.Subtotal:0.00}</p>");

                emailBody.Append(
                    $"<p><strong>Total:</strong> ${order.Total:0.00}</p>");

                emailBody.Append(
                    $"<p><strong>Payment Status:</strong> {WebUtility.HtmlEncode(order.PaymentStatus)}</p>");

                emailBody.Append(
                    "<p>Your payment has not been completed. This email is not a payment receipt.</p>");

                emailBody.Append(
                    "<p>Thank you for supporting Sengele Ministries.</p>");

                await _emailSender.SendEmailAsync(
                    order.Email,
                    $"Sengele Ministries - Pending Order #{order.OrderId}",
                    emailBody.ToString());
            }
            catch (Exception)
            {
                // Email failure must not cancel
                // an order saved in SQL Server.
                TempData["EmailWarning"] =
                    "Your order was saved, but the confirmation email could not be sent.";
            }

            TempData["OrderSuccess"] =
                $"Order #{order.OrderId} has been created. Payment is pending.";

            return RedirectToAction(
                nameof(Confirmation),
                new { id = order.OrderId });
        }

        // ==========================================
        // ORDER CONFIRMATION
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Member");
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.MemberId == memberId.Value);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // ==========================================
        // GET CURRENT MEMBER ID
        // ==========================================
        private int? GetMemberId()
        {
            var claim =
                User.FindFirst("MemberId")
                ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null &&
                int.TryParse(claim.Value, out int memberId))
            {
                return memberId;
            }

            return null;
        }

        // ==========================================
        // GET MEMBER CART FROM SQL SERVER
        // ==========================================
        private async Task<List<CartItem>> GetCartAsync(int memberId)
        {
            var databaseItems =
                await _context.ShoppingCartItems
                    .Where(c => c.MemberId == memberId)
                    .ToListAsync();

            if (databaseItems.Count == 0)
            {
                return new List<CartItem>();
            }

            var productIds = databaseItems
                .Select(c => c.ProductId)
                .ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            var cart = new List<CartItem>();

            foreach (var databaseItem in databaseItems)
            {
                var product = products.FirstOrDefault(p =>
                    p.ProductId == databaseItem.ProductId);

                if (product == null ||
                    !product.IsPublished ||
                    product.ProductType == "AmazonAffiliate" ||
                    (!string.IsNullOrWhiteSpace(product.ProductType) &&
                     product.ProductType != "OwnProduct") ||
                    product.RequiresShipping ||
                    product.Price <= 0 ||
                    decimal.Round(product.Price, 2) != product.Price ||
                    databaseItem.Quantity <= 0 ||
                    (product.StockQuantity.HasValue &&
                     (product.StockQuantity.Value <= 0 ||
                      databaseItem.Quantity > product.StockQuantity.Value)))
                {
                    continue;
                }

                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    Title = product.Title,
                    Price = product.Price,
                    Quantity = databaseItem.Quantity,
                    ImageUrl = product.ImageUrl
                });
            }

            return cart;
        }
    }
}
