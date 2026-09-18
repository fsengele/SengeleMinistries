using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using System.Security.Claims;

namespace SengeleMinistries.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // SHOW SHOPPING CART
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

            var viewModel = new ShoppingCartViewModel
            {
                Items = cart
            };

            return View(viewModel);
        }


        // =========================================================
        // ADD PRODUCT TO CART
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(
            int productId,
            int quantity = 1)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Member");
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.ProductId == productId &&
                    p.IsPublished);

            if (product == null)
            {
                return NotFound();
            }

            // Product out of stock
            if (product.StockQuantity.HasValue &&
                product.StockQuantity.Value <= 0)
            {
                TempData["CartError"] =
                    $"{product.Title} is currently out of stock.";

                return RedirectToAction(
                    "Details",
                    "Shop",
                    new { id = productId });
            }

            // Minimum quantity = 1
            if (quantity < 1)
            {
                quantity = 1;
            }

            // Do not allow quantity above available stock
            if (product.StockQuantity.HasValue &&
                quantity > product.StockQuantity.Value)
            {
                quantity = product.StockQuantity.Value;
            }

            var existingItem =
                await _context.ShoppingCartItems
                    .FirstOrDefaultAsync(c =>
                        c.MemberId == memberId.Value &&
                        c.ProductId == productId);

            if (existingItem != null)
            {
                int newQuantity =
                    existingItem.Quantity + quantity;

                if (product.StockQuantity.HasValue &&
                    newQuantity > product.StockQuantity.Value)
                {
                    existingItem.Quantity =
                        product.StockQuantity.Value;

                    existingItem.UpdatedAt =
                        DateTime.UtcNow;

                    TempData["CartError"] =
                        $"Only {product.StockQuantity.Value} item(s) of {product.Title} are available.";
                }
                else
                {
                    existingItem.Quantity =
                        newQuantity;

                    existingItem.UpdatedAt =
                        DateTime.UtcNow;

                    TempData["CartMessage"] =
                        $"{product.Title} was added to your cart.";
                }
            }
            else
            {
                var cartItem =
                    new ShoppingCartItem
                    {
                        MemberId = memberId.Value,
                        ProductId = product.ProductId,
                        Quantity = quantity,
                        CreatedAt = DateTime.UtcNow
                    };

                _context.ShoppingCartItems.Add(cartItem);

                TempData["CartMessage"] =
                    $"{product.Title} was added to your cart.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // UPDATE PRODUCT QUANTITY
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(
            int productId,
            int quantity)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Member");
            }

            var cartItem =
                await _context.ShoppingCartItems
                    .FirstOrDefaultAsync(c =>
                        c.MemberId == memberId.Value &&
                        c.ProductId == productId);

            if (cartItem == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.ProductId == productId &&
                        p.IsPublished);

            // Product no longer available
            if (product == null)
            {
                _context.ShoppingCartItems.Remove(cartItem);

                await _context.SaveChangesAsync();

                TempData["CartError"] =
                    "This product is no longer available.";

                return RedirectToAction(nameof(Index));
            }

            // Quantity 0 = remove product
            if (quantity <= 0)
            {
                _context.ShoppingCartItems.Remove(cartItem);

                await _context.SaveChangesAsync();

                TempData["CartMessage"] =
                    $"{product.Title} was removed from your cart.";

                return RedirectToAction(nameof(Index));
            }

            // Product out of stock
            if (product.StockQuantity.HasValue &&
                product.StockQuantity.Value <= 0)
            {
                _context.ShoppingCartItems.Remove(cartItem);

                await _context.SaveChangesAsync();

                TempData["CartError"] =
                    $"{product.Title} is currently out of stock.";

                return RedirectToAction(nameof(Index));
            }

            // Requested quantity exceeds stock
            if (product.StockQuantity.HasValue &&
                quantity > product.StockQuantity.Value)
            {
                quantity =
                    product.StockQuantity.Value;

                cartItem.Quantity = quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["CartError"] =
                    $"Only {product.StockQuantity.Value} item(s) of {product.Title} are available.";

                return RedirectToAction(nameof(Index));
            }

            // Quantity did not change
            if (cartItem.Quantity == quantity)
            {
                return RedirectToAction(nameof(Index));
            }

            cartItem.Quantity = quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["CartMessage"] =
                $"{product.Title} quantity was updated.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // REMOVE PRODUCT FROM CART
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int productId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Member");
            }

            var cartItem =
                await _context.ShoppingCartItems
                    .FirstOrDefaultAsync(c =>
                        c.MemberId == memberId.Value &&
                        c.ProductId == productId);

            if (cartItem != null)
            {
                var product =
                    await _context.Products
                        .FirstOrDefaultAsync(p =>
                            p.ProductId == productId);

                _context.ShoppingCartItems.Remove(cartItem);

                await _context.SaveChangesAsync();

                TempData["CartMessage"] =
                    product != null
                        ? $"{product.Title} was removed from your cart."
                        : "The product was removed from your cart.";
            }

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // CLEAR SHOPPING CART
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Member");
            }

            var cartItems =
                await _context.ShoppingCartItems
                    .Where(c =>
                        c.MemberId == memberId.Value)
                    .ToListAsync();

            if (cartItems.Count > 0)
            {
                _context.ShoppingCartItems
                    .RemoveRange(cartItems);

                await _context.SaveChangesAsync();
            }

            TempData["CartMessage"] =
                "Your shopping cart is now empty.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // GET MEMBER ID FROM LOGIN CLAIM
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
                        c.MemberId == memberId)
                    .ToListAsync();

            if (databaseItems.Count == 0)
            {
                return new List<CartItem>();
            }

            var productIds =
                databaseItems
                    .Select(c => c.ProductId)
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