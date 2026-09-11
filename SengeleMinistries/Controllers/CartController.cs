using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using System.Text.Json;

namespace SengeleMinistries.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        private const string CartSessionKey = "ShoppingCart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // CART
        // GET: /Cart
        // ==========================================
        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();

            var viewModel = new ShoppingCartViewModel
            {
                Items = cart
            };

            return View(viewModel);
        }

        // ==========================================
        // ADD TO CART
        // POST: /Cart/AddToCart
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(
            int productId,
            int quantity = 1)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.ProductId == productId &&
                    p.IsPublished);

            if (product == null)
            {
                return NotFound();
            }

            if (quantity < 1)
            {
                quantity = 1;
            }

            var cart = GetCart();

            var existingItem = cart
                .FirstOrDefault(item =>
                    item.ProductId == product.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    Title = product.Title,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl
                });
            }

            SaveCart(cart);

            TempData["CartMessage"] =
                $"{product.Title} was added to your cart.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // REMOVE FROM CART
        // POST: /Cart/Remove
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(
                p => p.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // SESSION HELPERS
        // ==========================================
        private List<CartItem> GetCart()
        {
            var cartJson =
                HttpContext.Session.GetString(CartSessionKey);

            if (string.IsNullOrWhiteSpace(cartJson))
            {
                return new List<CartItem>();
            }

            return JsonSerializer.Deserialize<List<CartItem>>(
                       cartJson)
                   ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            var cartJson =
                JsonSerializer.Serialize(cart);

            HttpContext.Session.SetString(
                CartSessionKey,
                cartJson);
        }
    }
}
