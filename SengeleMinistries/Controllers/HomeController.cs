
using Microsoft.AspNetCore.Mvc;
using SengeleMinistries.Models;
using System.Diagnostics;

namespace SengeleMinistries.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        // ==========================================
        // CONSTRUCTOR
        // ==========================================
        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ==========================================
        // HOME PAGE
        // ==========================================
        public IActionResult Index()
        {
            return View();
        }

        // ==========================================
        // DONATE PAGE - STRIPE
        // ==========================================
        public IActionResult Donate()
        {
            // Public Stripe key for the donation page
            ViewBag.StripePublishableKey =
                _configuration["Stripe:PublishableKey"];

            return View();
        }

        // ==========================================
        // PRIVACY PAGE
        // ==========================================
        public IActionResult Privacy()
        {
            return View();
        }

        // ==========================================
        // ERROR PAGE
        // ==========================================
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}
