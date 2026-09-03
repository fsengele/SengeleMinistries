using Microsoft.AspNetCore.Mvc;

namespace SengeleMinistries.Controllers
{
    public class PrayerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
