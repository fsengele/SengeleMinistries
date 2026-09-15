using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SengeleMinistries.Controllers
{
    public class MemberController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MemberController(ApplicationDbContext db)
        {
            _db = db;
        }


        // ==========================================
        // REGISTER
        // ==========================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistrationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var exists = _db.Members?
                .FirstOrDefault(m => m.Email == model.Email);

            if (exists != null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "A member with that email already exists.");

                return View(model);
            }

            // Use ASP.NET Core PasswordHasher
            var hasher =
                new Microsoft.AspNetCore.Identity.PasswordHasher<Member>();

            var member = new Member
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                CreatedAt = DateTime.UtcNow
            };

            member.PasswordHash =
                hasher.HashPassword(member, model.Password);

            _db.Members?.Add(member);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Registration successful. You may now log in.";

            return RedirectToAction("Login");
        }


        // ==========================================
        // LOGIN
        // ==========================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var member = _db.Members?
                .FirstOrDefault(m => m.Email == model.Email);

            if (member == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View(model);
            }

            var hasher =
                new Microsoft.AspNetCore.Identity.PasswordHasher<Member>();

            // First try PasswordHasher verification
            var verify = hasher.VerifyHashedPassword(
                member,
                member.PasswordHash ?? string.Empty,
                model.Password);

            if (verify ==
                Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            {
                // Support older SHA256 passwords
                var legacy = HashPassword(model.Password);

                if (!string.Equals(
                    legacy,
                    member.PasswordHash,
                    StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Invalid email or password.");

                    return View(model);
                }

                // Convert old password hash to PasswordHasher
                member.PasswordHash =
                    hasher.HashPassword(member, model.Password);

                _db.Members?.Update(member);
                await _db.SaveChangesAsync();
            }

            // Create claims for the logged-in member
            var claims =
                new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier,
                        member.Id.ToString()),

                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.Name,
                        member.FirstName + " " + member.LastName),

                    new System.Security.Claims.Claim(
                        "MemberId",
                        member.Id.ToString())
                };

            // Add administrator claim when applicable
            if (member.IsAdmin)
            {
                claims.Add(
                    new System.Security.Claims.Claim(
                        "IsAdmin",
                        "true"));
            }

            var identity =
                new System.Security.Claims.ClaimsIdentity(
                    claims,
                    Microsoft.AspNetCore.Authentication.Cookies
                        .CookieAuthenticationDefaults.AuthenticationScheme);

            var principal =
                new System.Security.Claims.ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                Microsoft.AspNetCore.Authentication.Cookies
                    .CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            TempData["SuccessMessage"] =
                $"Welcome back, {member.FirstName}!";

            return RedirectToAction("Dashboard");
        }


        // ==========================================
        // LOGOUT
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                Microsoft.AspNetCore.Authentication.Cookies
                    .CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }


        // ==========================================
        // MEMBER DASHBOARD
        // ==========================================

        [HttpGet]
        [Authorize]
        public IActionResult Dashboard()
        {
            return View();
        }


        // ==========================================
        // MY PROFILE
        // ==========================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var memberIdClaim = User.FindFirst("MemberId");

            if (memberIdClaim == null ||
                !int.TryParse(memberIdClaim.Value, out int memberId))
            {
                return RedirectToAction("Login");
            }

            var member = await _db.Members
                .FirstOrDefaultAsync(m => m.Id == memberId);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }


        // ==========================================
        // EDIT PROFILE - GET
        // ==========================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var memberIdClaim = User.FindFirst("MemberId");

            if (memberIdClaim == null ||
                !int.TryParse(memberIdClaim.Value, out int memberId))
            {
                return RedirectToAction("Login");
            }

            var member = await _db.Members
                .FirstOrDefaultAsync(m => m.Id == memberId);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }


        // ==========================================
        // EDIT PROFILE - POST
        // ==========================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(
            Member model,
            IFormFile? profileImage)
        {
            var memberIdClaim = User.FindFirst("MemberId");

            if (memberIdClaim == null ||
                !int.TryParse(memberIdClaim.Value, out int memberId))
            {
                return RedirectToAction("Login");
            }

            var member = await _db.Members
                .FirstOrDefaultAsync(m => m.Id == memberId);

            if (member == null)
            {
                return NotFound();
            }


            // ==========================================
            // UPDATE PERSONAL INFORMATION
            // ==========================================

            member.FirstName = model.FirstName;
            member.LastName = model.LastName;
            member.Email = model.Email;
            member.Phone = model.Phone;
            member.DateOfBirth = model.DateOfBirth;
            member.StreetAddress = model.StreetAddress;
            member.City = model.City;
            member.State = model.State;
            member.ZipCode = model.ZipCode;
            member.Country = model.Country;


            // ==========================================
            // PROFILE PICTURE
            // ==========================================

            if (profileImage != null && profileImage.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

                var extension =
                    Path.GetExtension(profileImage.FileName)
                        .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Please choose a JPG, PNG or WebP image.");

                    return View(member);
                }


                // Create the profile images folder if needed
                var profilesFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "images",
                    "profiles");

                Directory.CreateDirectory(profilesFolder);


                // Create a unique image file name
                var fileName =
                    $"member-{member.Id}-{Guid.NewGuid()}{extension}";

                var filePath =
                    Path.Combine(profilesFolder, fileName);


                // Save the image inside wwwroot/images/profiles
                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }


                // Save the image path in the database
                member.ProfileImageUrl =
                    $"/images/profiles/{fileName}";
            }


            // ==========================================
            // SAVE PROFILE CHANGES
            // ==========================================

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your profile was updated successfully.";

            return RedirectToAction("Profile");
        }


        // ==========================================
        // MY ORDERS
        // ==========================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Orders()
        {
            var memberIdClaim = User.FindFirst("MemberId");

            if (memberIdClaim == null ||
                !int.TryParse(memberIdClaim.Value, out int memberId))
            {
                return RedirectToAction("Login");
            }

            var orders = await _db.Orders
                .Where(o => o.MemberId == memberId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }


        // ==========================================
        // ORDER DETAILS
        // ==========================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var memberIdClaim = User.FindFirst("MemberId");

            if (memberIdClaim == null ||
                !int.TryParse(memberIdClaim.Value, out int memberId))
            {
                return RedirectToAction("Login");
            }

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.MemberId == memberId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }


        // ==========================================
        // LEGACY PASSWORD SUPPORT
        // ==========================================

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();

            var bytes =
                Encoding.UTF8.GetBytes(password ?? string.Empty);

            var hash =
                sha.ComputeHash(bytes);

            return Convert.ToHexString(hash);
        }
    }
}