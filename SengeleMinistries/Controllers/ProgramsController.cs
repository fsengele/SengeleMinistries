using Microsoft.AspNetCore.Mvc;

namespace SengeleMinistries.Controllers
{
    public class ProgramsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
