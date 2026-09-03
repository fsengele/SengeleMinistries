using Microsoft.AspNetCore.Mvc;

namespace SengeleMinistries.Controllers
{
    public class GalleryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
