using Microsoft.AspNetCore.Mvc;

namespace SengeleMinistries.Controllers
{
    public class MessagesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            ViewData["Id"] = id;
            return View();
        }
    }
}
