using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using System.Security.Claims;
using System.Text.Json;

namespace SengeleMinistries.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "ShoppingCart";

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();

            if (cart.Count == 0)
            {
                TempData["CartError"] = "Your shopping cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

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

            var order = new Order
            {
                MemberId = member.Id,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                Subtotal = cart.Sum(item => item.Total),
                Total = cart.Sum(item => item.Total)
            };

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Order model)
        {
            var cart = GetCart();

            if (cart.Count == 0)
            {
                TempData["CartError"] = "Your shopping cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

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

            // Recalculate prices from the database.
            decimal subtotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var cartItem in cart)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.ProductId == cartItem.ProductId &&
                        p.IsPublished);

                if (product == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"{cartItem.Title} is no longer available.");

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

                subtotal += product.Price * cartItem.Quantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.ProductId,
                    ProductTitle = product.Title,
                    Price = product.Price,
                    Quantity = cartItem.Quantity
                });
            }

            if (!ModelState.IsValid)
            {
                model.MemberId = member.Id;
                model.FirstName = member.FirstName;
                model.LastName = member.LastName;
                model.Email = member.Email;
                model.Subtotal = subtotal;
                model.Total = subtotal;

                return View("Index", model);
            }

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
                Total = subtotal,

                OrderStatus = "Pending",
                PaymentStatus = "Pending",

                CreatedAt = DateTime.UtcNow,

                OrderItems = orderItems
            };

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove(CartSessionKey);

            TempData["OrderSuccess"] =
                $"Thank you! Your order #{order.OrderId} has been created.";

            return RedirectToAction(
                nameof(Confirmation),
                new { id = order.OrderId });
        }

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

        private int? GetMemberId()
        {
            var claim = User.FindFirst("MemberId");

            if (claim == null)
            {
                claim = User.FindFirst(ClaimTypes.NameIdentifier);
            }

            if (claim != null &&
                int.TryParse(claim.Value, out int memberId))
            {
                return memberId;
            }

            return null;
        }

        private List<CartItem> GetCart()
        {
            var cartJson =
                HttpContext.Session.GetString(CartSessionKey);

            if (string.IsNullOrWhiteSpace(cartJson))
            {
                return new List<CartItem>();
            }

            return JsonSerializer
                       .Deserialize<List<CartItem>>(cartJson)
                   ?? new List<CartItem>();
        }
    }
}