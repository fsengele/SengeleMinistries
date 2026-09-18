using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using System.Security.Claims;

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


        // =========================================================
        // CHECKOUT PAGE
        // GET: /Checkout
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Member");
            }

            var cart = await GetCartAsync(memberId.Value);

            if (cart.Count == 0)
            {
                TempData["CartError"] =
                    "Your shopping cart is empty.";

                return RedirectToAction("Index", "Cart");
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m =>
                    m.Id == memberId.Value);

            if (member == null)
            {
                return RedirectToAction("Login", "Member");
            }

            decimal subtotal =
                cart.Sum(item => item.Total);

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


        // =========================================================
        // PLACE ORDER
        // POST: /Checkout/PlaceOrder
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            Order model)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Member");
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m =>
                    m.Id == memberId.Value);

            if (member == null)
            {
                return RedirectToAction(
                    "Login",
                    "Member");
            }

            var databaseCartItems =
                await _context.ShoppingCartItems
                    .Where(c =>
                        c.MemberId == memberId.Value)
                    .ToListAsync();

            if (databaseCartItems.Count == 0)
            {
                TempData["CartError"] =
                    "Your shopping cart is empty.";

                return RedirectToAction(
                    "Index",
                    "Cart");
            }


            // =====================================================
            // ALWAYS TRUST DATABASE PRICES
            // =====================================================
            decimal subtotal = 0;

            var orderItems =
                new List<OrderItem>();


            foreach (var cartItem
                     in databaseCartItems)
            {
                var product =
                    await _context.Products
                        .FirstOrDefaultAsync(p =>
                            p.ProductId ==
                            cartItem.ProductId &&
                            p.IsPublished);

                if (product == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "A product in your cart is no longer available.");

                    continue;
                }


                // Product is out of stock
                if (product.StockQuantity.HasValue &&
                    product.StockQuantity.Value <= 0)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"{product.Title} is currently out of stock.");

                    continue;
                }


                // Quantity exceeds available stock
                if (product.StockQuantity.HasValue &&
                    cartItem.Quantity >
                    product.StockQuantity.Value)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Only {product.StockQuantity.Value} item(s) of {product.Title} are available.");

                    continue;
                }


                subtotal +=
                    product.Price *
                    cartItem.Quantity;


                orderItems.Add(
                    new OrderItem
                    {
                        ProductId =
                            product.ProductId,

                        ProductTitle =
                            product.Title,

                        Price =
                            product.Price,

                        Quantity =
                            cartItem.Quantity
                    });
            }


            // =====================================================
            // VALIDATION ERROR
            // RETURN CUSTOMER TO CHECKOUT
            // =====================================================
            if (!ModelState.IsValid)
            {
                model.MemberId =
                    member.Id;

                model.FirstName =
                    member.FirstName;

                model.LastName =
                    member.LastName;

                model.Email =
                    member.Email;

                model.Subtotal =
                    subtotal;

                model.Total =
                    subtotal;

                return View(
                    "Index",
                    model);
            }


            // =====================================================
            // CREATE ORDER
            // =====================================================
            var order = new Order
            {
                MemberId =
                    member.Id,

                FirstName =
                    member.FirstName,

                LastName =
                    member.LastName,

                Email =
                    member.Email,

                Phone =
                    model.Phone,

                Address =
                    model.Address,

                City =
                    model.City,

                State =
                    model.State,

                ZipCode =
                    model.ZipCode,

                Subtotal =
                    subtotal,

                Total =
                    subtotal,

                OrderStatus =
                    "Pending",

                PaymentStatus =
                    "Pending",

                CreatedAt =
                    DateTime.UtcNow,

                OrderItems =
                    orderItems
            };


            // =====================================================
            // SAVE ORDER TO SQL SERVER
            // =====================================================
            _context.Orders.Add(order);

            await _context.SaveChangesAsync();


            // =====================================================
            // CLEAR MEMBER CART FROM SQL SERVER
            // ONLY AFTER ORDER WAS SAVED SUCCESSFULLY
            // =====================================================
            _context.ShoppingCartItems
                .RemoveRange(databaseCartItems);

            await _context.SaveChangesAsync();


            TempData["OrderSuccess"] =
                $"Thank you! Your order #{order.OrderId} has been created.";


            return RedirectToAction(
                nameof(Confirmation),
                new
                {
                    id = order.OrderId
                });
        }


        // =========================================================
        // ORDER CONFIRMATION
        // GET: /Checkout/Confirmation/1
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Confirmation(
            int id)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Member");
            }

            var order =
                await _context.Orders

                    .Include(o =>
                        o.OrderItems)

                    .FirstOrDefaultAsync(o =>
                        o.OrderId == id &&
                        o.MemberId ==
                        memberId.Value);


            if (order == null)
            {
                return NotFound();
            }


            return View(order);
        }


        // =========================================================
        // GET CURRENT MEMBER ID
        // =========================================================
        private int? GetMemberId()
        {
            var claim =
                User.FindFirst("MemberId")
                ?? User.FindFirst(
                    ClaimTypes.NameIdentifier);


            if (claim != null &&
                int.TryParse(
                    claim.Value,
                    out int memberId))
            {
                return memberId;
            }


            return null;
        }


        // =========================================================
        // GET MEMBER CART FROM SQL SERVER
        // =========================================================
        private async Task<List<CartItem>>
            GetCartAsync(int memberId)
        {
            var databaseItems =
                await _context.ShoppingCartItems

                    .Where(c =>
                        c.MemberId ==
                        memberId)

                    .ToListAsync();


            if (databaseItems.Count == 0)
            {
                return new List<CartItem>();
            }


            var productIds =
                databaseItems

                    .Select(c =>
                        c.ProductId)

                    .ToList();


            var products =
                await _context.Products

                    .Where(p =>
                        productIds.Contains(
                            p.ProductId))

                    .ToListAsync();


            var cart =
                new List<CartItem>();


            foreach (var databaseItem
                     in databaseItems)
            {
                var product =
                    products.FirstOrDefault(p =>
                        p.ProductId ==
                        databaseItem.ProductId);


                if (product == null)
                {
                    continue;
                }


                cart.Add(
                    new CartItem
                    {
                        ProductId =
                            product.ProductId,

                        Title =
                            product.Title,

                        Price =
                            product.Price,

                        Quantity =
                            databaseItem.Quantity,

                        ImageUrl =
                            product.ImageUrl
                    });
            }


            return cart;
        }
    }
}