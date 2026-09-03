using Microsoft.AspNetCore.Mvc;

namespace SengeleMinistries.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
