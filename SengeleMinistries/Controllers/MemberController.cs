using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using System.Security.Cryptography;
using System.Text;

namespace SengeleMinistries.Controllers
{
    public class MemberController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MemberController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistrationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var exists = _db.Members?.FirstOrDefault(m => m.Email == model.Email);
            if (exists != null)
            {
                ModelState.AddModelError(string.Empty, "A member with that email already exists.");
                return View(model);
            }

            // Use ASP.NET Core's PasswordHasher for secure hashing
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Member>();

            var member = new Member
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                CreatedAt = DateTime.UtcNow
            };

            member.PasswordHash = hasher.HashPassword(member, model.Password);

            _db.Members?.Add(member);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful. You may now log in.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var member = _db.Members?.FirstOrDefault(m => m.Email == model.Email);
            if (member == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Member>();

            // First try PasswordHasher verification (newer hashes)
            var verify = hasher.VerifyHashedPassword(member, member.PasswordHash ?? string.Empty, model.Password);
            if (verify == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            {
                // Fallback: support legacy SHA256 hex hashes (migrated accounts)
                var legacy = HashPassword(model.Password);
                if (!string.Equals(legacy, member.PasswordHash, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View(model);
                }
                // If legacy matched, rehash with PasswordHasher and persist
                member.PasswordHash = hasher.HashPassword(member, model.Password);
                _db.Members?.Update(member);
                await _db.SaveChangesAsync();
            }

            // Create claims principal and sign in with cookie authentication
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, member.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, member.FirstName + " " + member.LastName),
                new System.Security.Claims.Claim("MemberId", member.Id.ToString())
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, principal);

            TempData["SuccessMessage"] = $"Welcome back, {member.FirstName}!";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult Dashboard()
        {
            return View();
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
