
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

        // ==========================================
        // INDEX
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // ==========================================
        // AMAZON LINK VALIDATION
        // ==========================================
        private static bool IsValidAmazonLink(string? link)
        {
            if (string.IsNullOrWhiteSpace(link))
                return false;

            if (!Uri.TryCreate(
                link.Trim(),
                UriKind.Absolute,
                out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttps)
                return false;

            string host = uri.Host.ToLowerInvariant();

            if (host == "link.amazon" ||
                host == "amzn.to")
                return true;

            string[] amazonDomains =
            {
                "amazon.com",
                "amazon.ca",
                "amazon.co.uk"
            };

            return amazonDomains.Any(domain =>
                host == domain ||
                host.EndsWith(
                    "." + domain,
                    StringComparison.Ordinal));
        }

        // ==========================================
        // PRODUCT VALIDATION
        // ==========================================
        private void ValidateProduct(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Title))
            {
                ModelState.AddModelError(
                    nameof(Product.Title),
                    "Title is required.");
            }

            if (string.IsNullOrWhiteSpace(product.Category))
            {
                ModelState.AddModelError(
                    nameof(Product.Category),
                    "Category is required.");
            }

            if (product.ProductType != "OwnProduct" &&
                product.ProductType != "AmazonAffiliate")
            {
                ModelState.AddModelError(
                    nameof(Product.ProductType),
                    "Please select a valid product type.");
            }

            // ======================================
            // OWN PRODUCTS
            // ======================================
            if (product.ProductType == "OwnProduct")
            {
                if (product.Price < 0)
                {
                    ModelState.AddModelError(
                        nameof(Product.Price),
                        "Price cannot be negative.");
                }

                product.AffiliateUrl = null;

                ModelState.Remove(
                    nameof(Product.AffiliateUrl));

                // Digital products do not need
                // physical shipping measurements.
                if (!product.RequiresShipping)
                {
                    product.WeightOz = null;
                    product.LengthIn = null;
                    product.WidthIn = null;
                    product.HeightIn = null;

                    ModelState.Remove(nameof(Product.WeightOz));
                    ModelState.Remove(nameof(Product.LengthIn));
                    ModelState.Remove(nameof(Product.WidthIn));
                    ModelState.Remove(nameof(Product.HeightIn));
                }
            }

            // ======================================
            // AMAZON AFFILIATE PRODUCTS
            // ======================================
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

                product.Price = 0;
                product.StockQuantity = null;

                // Amazon handles delivery.
                product.RequiresShipping = false;
                product.WeightOz = null;
                product.LengthIn = null;
                product.WidthIn = null;
                product.HeightIn = null;

                ModelState.Remove(nameof(Product.Price));
                ModelState.Remove(nameof(Product.StockQuantity));
                ModelState.Remove(nameof(Product.RequiresShipping));
                ModelState.Remove(nameof(Product.WeightOz));
                ModelState.Remove(nameof(Product.LengthIn));
                ModelState.Remove(nameof(Product.WidthIn));
                ModelState.Remove(nameof(Product.HeightIn));
            }
        }

        // ==========================================
        // CREATE - GET
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Product());
        }

        // ==========================================
        // CREATE - POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            ModelState.Remove(nameof(Product.ProductId));
            ModelState.Remove(nameof(Product.CreatedAt));
            ModelState.Remove(nameof(Product.UpdatedAt));

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

        // ==========================================
        // EDIT - GET
        // ==========================================
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

        // ==========================================
        // EDIT - POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Product.CreatedAt));
            ModelState.Remove(nameof(Product.UpdatedAt));

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

            // Existing product information
            existingProduct.Title = product.Title;
            existingProduct.Category = product.Category;
            existingProduct.ShortDescription =
                product.ShortDescription;
            existingProduct.Description =
                product.Description;
            existingProduct.Price = product.Price;
            existingProduct.ImageUrl = product.ImageUrl;

            // Preserve additional images unless
            // their fields are submitted by the form.
            if (Request.Form.ContainsKey(nameof(Product.ImageUrl2)))
                existingProduct.ImageUrl2 = product.ImageUrl2;

            if (Request.Form.ContainsKey(nameof(Product.ImageUrl3)))
                existingProduct.ImageUrl3 = product.ImageUrl3;

            if (Request.Form.ContainsKey(nameof(Product.ImageUrl4)))
                existingProduct.ImageUrl4 = product.ImageUrl4;

            existingProduct.StockQuantity =
                product.StockQuantity;

            existingProduct.IsPublished =
                product.IsPublished;

            existingProduct.ProductType =
                product.ProductType;

            existingProduct.AffiliateUrl =
                product.AffiliateUrl;

            // ======================================
            // SHIPPING INFORMATION
            // ======================================
            // Preserve the current shipping setting
            // until the admin form includes this field.
            if (Request.Form.ContainsKey(
                nameof(Product.RequiresShipping)))
            {
                existingProduct.RequiresShipping =
                    product.RequiresShipping;
            }

            if (existingProduct.ProductType ==
                "AmazonAffiliate")
            {
                existingProduct.RequiresShipping = false;
            }

            if (!existingProduct.RequiresShipping)
            {
                existingProduct.WeightOz = null;
                existingProduct.LengthIn = null;
                existingProduct.WidthIn = null;
                existingProduct.HeightIn = null;
            }
            else
            {
                if (Request.Form.ContainsKey(nameof(Product.WeightOz)))
                    existingProduct.WeightOz = product.WeightOz;

                if (Request.Form.ContainsKey(nameof(Product.LengthIn)))
                    existingProduct.LengthIn = product.LengthIn;

                if (Request.Form.ContainsKey(nameof(Product.WidthIn)))
                    existingProduct.WidthIn = product.WidthIn;

                if (Request.Form.ContainsKey(nameof(Product.HeightIn)))
                    existingProduct.HeightIn = product.HeightIn;
            }

            existingProduct.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Product updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // DELETE - GET
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // ==========================================
        // DELETE - POST
        // ==========================================
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
