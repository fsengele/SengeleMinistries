using Microsoft.AspNetCore.Mvc;

namespace SengeleMinistries.Controllers
{
    public class GivingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
