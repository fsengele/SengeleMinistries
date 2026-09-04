using Microsoft.AspNetCore.Mvc;
using SengeleMinistries.Data;
using SengeleMinistries.Models;

using Microsoft.EntityFrameworkCore;


namespace SengeleMinistries.Controllers
{
    public class ContactController : Controller
    {
        // The Contact page is informational only. No form submission is handled here.
        public IActionResult Index()
        {
            return View();
        }
    }
}
