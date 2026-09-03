using Microsoft.AspNetCore.Mvc;

namespace SengeleMinistries.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
