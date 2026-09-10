using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace SengeleMinistries.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalMembers = await _db.Members!.CountAsync(),
                TotalServeApplications = await _db.VolunteerApplications.CountAsync(),
                TotalProducts = await _db.Products.CountAsync(),
                RecentMembers = await _db.Members!
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(vm);
        }
    }
}

