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


        // =========================================================
        // SHOW SHOPPING CART
        // GET: /Cart
        // =========================================================
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


        // =========================================================
        // ADD PRODUCT TO CART
        // POST: /Cart/AddToCart
        // =========================================================
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

            // Product is out of stock
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

            var cart = GetCart();

            var existingItem = cart.FirstOrDefault(
                item => item.ProductId == product.ProductId);

            // Product already exists in cart
            if (existingItem != null)
            {
                int newQuantity =
                    existingItem.Quantity + quantity;

                if (product.StockQuantity.HasValue &&
                    newQuantity > product.StockQuantity.Value)
                {
                    existingItem.Quantity =
                        product.StockQuantity.Value;

                    TempData["CartError"] =
                        $"Only {product.StockQuantity.Value} item(s) of {product.Title} are available.";
                }
                else
                {
                    existingItem.Quantity = newQuantity;

                    TempData["CartMessage"] =
                        $"{product.Title} was added to your cart.";
                }
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

                TempData["CartMessage"] =
                    $"{product.Title} was added to your cart.";
            }

            SaveCart(cart);

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // UPDATE PRODUCT QUANTITY
        // POST: /Cart/UpdateQuantity
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(
            int productId,
            int quantity)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(
                item => item.ProductId == productId);

            if (item == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.ProductId == productId &&
                    p.IsPublished);

            // Product no longer exists
            if (product == null)
            {
                cart.Remove(item);

                SaveCart(cart);

                TempData["CartError"] =
                    "This product is no longer available.";

                return RedirectToAction(nameof(Index));
            }

            // Quantity 0 = remove product
            if (quantity <= 0)
            {
                cart.Remove(item);

                SaveCart(cart);

                TempData["CartMessage"] =
                    $"{product.Title} was removed from your cart.";

                return RedirectToAction(nameof(Index));
            }

            // Product out of stock
            if (product.StockQuantity.HasValue &&
                product.StockQuantity.Value <= 0)
            {
                cart.Remove(item);

                SaveCart(cart);

                TempData["CartError"] =
                    $"{product.Title} is currently out of stock.";

                return RedirectToAction(nameof(Index));
            }

            // Quantity exceeds stock
            if (product.StockQuantity.HasValue &&
                quantity > product.StockQuantity.Value)
            {
                quantity = product.StockQuantity.Value;

                // Only save if quantity actually changed
                if (item.Quantity != quantity)
                {
                    item.Quantity = quantity;

                    SaveCart(cart);
                }

                TempData["CartError"] =
                    $"Only {product.StockQuantity.Value} item(s) of {product.Title} are available.";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // IMPORTANT:
            // If customer did not change quantity, do nothing
            // =====================================================
            if (item.Quantity == quantity)
            {
                return RedirectToAction(nameof(Index));
            }

            // Quantity really changed
            item.Quantity = quantity;

            SaveCart(cart);

            TempData["CartMessage"] =
                $"{product.Title} quantity was updated.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // REMOVE PRODUCT FROM CART
        // POST: /Cart/Remove
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(
                item => item.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);

                SaveCart(cart);

                TempData["CartMessage"] =
                    $"{item.Title} was removed from your cart.";
            }

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // CLEAR SHOPPING CART
        // POST: /Cart/Clear
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);

            TempData["CartMessage"] =
                "Your shopping cart is now empty.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // GET CART FROM SESSION
        // =========================================================
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


        // =========================================================
        // SAVE CART TO SESSION
        // =========================================================
        private void SaveCart(List<CartItem> cart)
        {
            // If cart is empty, remove session
            if (cart.Count == 0)
            {
                HttpContext.Session.Remove(CartSessionKey);
                return;
            }

            var cartJson =
                JsonSerializer.Serialize(cart);

            HttpContext.Session.SetString(
                CartSessionKey,
                cartJson);
        }
    }
}