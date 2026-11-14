
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Data;
using SengeleMinistries.Models;

namespace SengeleMinistries.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // GET: /Admin/Products
        // Shows all products in the admin dashboard.
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // =========================================================
        // AMAZON LINK VALIDATION
        // Checks if the link belongs to Amazon.
        // =========================================================
        private static bool IsValidAmazonLink(string? link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return false;
            }

            if (!Uri.TryCreate(link.Trim(),
                UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            string host = uri.Host.ToLowerInvariant();

            // Amazon short affiliate links.
            if (host == "link.amazon" ||
                host == "amzn.to")
            {
                return true;
            }

            // Amazon product pages.
            string[] amazonDomains =
            {
                "amazon.com",
                "amazon.ca",
                "amazon.co.uk"
            };

            return amazonDomains.Any(domain =>
                host == domain ||
                host.EndsWith("." + domain,
                    StringComparison.Ordinal));
        }

        // =========================================================
        // PRODUCT VALIDATION
        // Used by Create and Edit.
        // =========================================================
        private void ValidateProduct(Product product)
        {
            // Check the product title.
            if (string.IsNullOrWhiteSpace(product.Title))
            {
                ModelState.AddModelError(
                    nameof(Product.Title),
                    "Title is required.");
            }

            // Check the product category.
            if (string.IsNullOrWhiteSpace(product.Category))
            {
                ModelState.AddModelError(
                    nameof(Product.Category),
                    "Category is required.");
            }

            // Check the product type.
            if (product.ProductType != "OwnProduct" &&
                product.ProductType != "AmazonAffiliate")
            {
                ModelState.AddModelError(
                    nameof(Product.ProductType),
                    "Please select a valid product type.");
            }

            // Validate Sengele Ministries products.
            if (product.ProductType == "OwnProduct")
            {
                if (product.Price < 0)
                {
                    ModelState.AddModelError(
                        nameof(Product.Price),
                        "Price cannot be negative.");
                }

                // Own products do not use affiliate links.
                product.AffiliateUrl = null;
                ModelState.Remove(
                    nameof(Product.AffiliateUrl));
            }

            // Validate Amazon affiliate products.
            if (product.ProductType == "AmazonAffiliate")
            {
                if (string.IsNullOrWhiteSpace(
                    product.AffiliateUrl))
                {
                    ModelState.AddModelError(
                        nameof(Product.AffiliateUrl),
                        "Amazon affiliate link is required.");
                }
                else if (!IsValidAmazonLink(
                    product.AffiliateUrl))
                {
                    ModelState.AddModelError(
                        nameof(Product.AffiliateUrl),
                        "Please enter a valid Amazon HTTPS link.");
                }

                // Amazon manages its own prices and stock.
                product.Price = 0;
                product.StockQuantity = null;

                ModelState.Remove(nameof(Product.Price));
                ModelState.Remove(
                    nameof(Product.StockQuantity));
            }
        }

        // =========================================================
        // CREATE - GET
        // GET: /Admin/Products/Create
        // =========================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Product());
        }

        // =========================================================
        // CREATE - POST
        // POST: /Admin/Products/Create
        // Saves a new product.
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            // These values are set by the application.
            ModelState.Remove(nameof(Product.ProductId));
            ModelState.Remove(nameof(Product.CreatedAt));
            ModelState.Remove(nameof(Product.UpdatedAt));

            // Validate product information.
            ValidateProduct(product);

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = null;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Product added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT - GET
        // GET: /Admin/Products/Edit/5
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // =========================================================
        // EDIT - POST
        // POST: /Admin/Products/Edit/5
        // Updates an existing product.
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Product.CreatedAt));
            ModelState.Remove(nameof(Product.UpdatedAt));

            // Validate both types of products.
            ValidateProduct(product);

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            var existingProduct =
                await _context.Products.FindAsync(id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            // Update product information.
            existingProduct.Title = product.Title;
            existingProduct.Category = product.Category;
            existingProduct.ShortDescription =
                product.ShortDescription;
            existingProduct.Description =
                product.Description;
            existingProduct.Price = product.Price;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.StockQuantity =
                product.StockQuantity;
            existingProduct.IsPublished =
                product.IsPublished;

            // Save the product type and affiliate link.
            existingProduct.ProductType =
                product.ProductType;
            existingProduct.AffiliateUrl =
                product.AffiliateUrl;

            existingProduct.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Product updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE - GET
        // GET: /Admin/Products/Delete/5
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // =========================================================
        // DELETE - POST
        // POST: /Admin/Products/Delete/5
        // =========================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product =
                await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Product deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
