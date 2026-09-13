using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using System.Security.Claims;

namespace SengeleMinistries.Controllers
{
    public class PrayerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrayerController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================
        // PRAYER FORM
        // =========================================

        [HttpGet]
        public IActionResult Index()
        {
            return View(new PrayerRequest());
        }


        // =========================================
        // SAVE PRAYER REQUEST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(PrayerRequest prayerRequest)
        {
            if (!ModelState.IsValid)
            {
                return View(prayerRequest);
            }

            prayerRequest.SubmittedAt = DateTime.UtcNow;


            // If the person is logged in,
            // connect this prayer request to the account.
            if (User.Identity != null &&
                User.Identity.IsAuthenticated)
            {
                var email =
                    User.FindFirstValue(ClaimTypes.Email)
                    ?? User.Identity.Name;

                prayerRequest.MemberEmail = email;
            }


            _context.PrayerRequests.Add(prayerRequest);

            await _context.SaveChangesAsync();


            TempData["PrayerSuccess"] =
                "Your prayer request was sent successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================
        // MY PRAYER REQUESTS
        // =========================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            var email =
                User.FindFirstValue(ClaimTypes.Email)
                ?? User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(
                    "Login",
                    "Member");
            }


            var requests =
                await _context.PrayerRequests
                    .Where(p => p.MemberEmail == email)
                    .OrderByDescending(p => p.SubmittedAt)
                    .ToListAsync();


            return View(requests);
        }
    }
}