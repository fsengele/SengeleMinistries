using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SengeleMinistries.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ShopController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Shop
        public async Task<IActionResult> Index()
        {
            var products = await _db.Products
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // GET: /Shop/Category?category=Books
        public async Task<IActionResult> Category(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return RedirectToAction(nameof(Index));
            }

            // Accept only known categories; handle unknown safely by redirecting to Index
            var allowed = new[] { "Books", "Teachings", "EventCollections", "Apparel" };
            if (!allowed.Contains(category))
            {
                return RedirectToAction(nameof(Index));
            }

            var products = await _db.Products
                .Where(p => p.IsPublished && p.Category == category)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewData["Category"] = category;
            return View(products);
        }

        // GET: /Shop/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products
                .Where(p => p.ProductId == id && p.IsPublished)
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}
