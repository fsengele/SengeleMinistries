
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SengeleMinistries.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // CHECKOUT PAGE
        // GET: /Checkout
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
                .FirstOrDefaultAsync(m => m.Id == memberId.Value);

            if (member == null)
            {
                return RedirectToAction("Login", "Member");
            }

            var cart = await GetCartAsync(memberId.Value);

            if (cart.Count == 0)
            {
                TempData["CartError"] =
                    "Your shopping cart is empty or its products are not available.";

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
                Total = subtotal
            };

            return View(order);
        }

        // ==========================================
        // PREPARE ORDER
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
                .FirstOrDefaultAsync(m => m.Id == memberId.Value);

            if (member == null)
            {
                return RedirectToAction("Login", "Member");
            }

            var cartItems = await _context.ShoppingCartItems
                .Where(c => c.MemberId == memberId.Value)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                TempData["CartError"] = "Your shopping cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            decimal subtotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var cartItem in cartItems)
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

                if (product.ProductType == "AmazonAffiliate")
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"{product.Title} must be purchased on Amazon.");
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

                // Sandbox phase: digital products only.
                if (product.RequiresShipping)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"{product.Title} requires shipping. Physical product checkout is not available yet.");
                    continue;
                }

                if (cartItem.Quantity <= 0 ||
                    (product.StockQuantity.HasValue &&
                     cartItem.Quantity > product.StockQuantity.Value))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Invalid quantity or insufficient stock for {product.Title}.");
                    continue;
                }

                if (product.Price <= 0 ||
                    decimal.Round(product.Price, 2) != product.Price)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Invalid price for {product.Title}.");
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
                model.Total = subtotal;

                return View("Index", model);
            }

            // Do not accept amounts submitted by the browser.
            // Taxes must be resolved before payment is enabled.
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
                ShippingAmount = 0m,
                TaxAmount = 0m,
                Total = subtotal,

                OrderStatus = "Pending",
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // IMPORTANT:
            // Do not clear the cart.
            // Do not mark this order as paid.
            // Do not send a payment receipt.
            // Stripe payment integration is not enabled yet.

            TempData["OrderSuccess"] =
                $"Order #{order.OrderId} was saved. Payment is still pending.";

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
        // CURRENT MEMBER ID
        // ==========================================
        private int? GetMemberId()
        {
            var claim =
                User.FindFirst("MemberId") ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null &&
                int.TryParse(claim.Value, out int memberId))
            {
                return memberId;
            }

            return null;
        }

        // ==========================================
        // GET MEMBER CART
        // ==========================================
        private async Task<List<CartItem>> GetCartAsync(int memberId)
        {
            var databaseItems = await _context.ShoppingCartItems
                .Where(c => c.MemberId == memberId)
                .ToListAsync();

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
