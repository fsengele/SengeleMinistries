using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using SengeleMinistries.Services;
using System.Security.Cryptography;
using System.Text;

namespace SengeleMinistries.Controllers
{
    public class MemberController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<MemberController> _logger;

        public MemberController(
            ApplicationDbContext db,
            IEmailSender emailSender,
            ILogger<MemberController> logger)
        {
            _db = db;
            _emailSender = emailSender;
            _logger = logger;
        }


        // ==========================================
        // REGISTER - GET
        // ==========================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        // ==========================================
        // REGISTER - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // ==========================================
            // CHECK IF EMAIL ALREADY EXISTS
            // ==========================================

            var exists = await _db.Members
                .FirstOrDefaultAsync(
                    m => m.Email == model.Email);


            if (exists != null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "A member with that email already exists.");

                return View(model);
            }


            // ==========================================
            // CREATE MEMBER
            // ==========================================

            var hasher =
                new Microsoft.AspNetCore.Identity
                    .PasswordHasher<Member>();


            var member = new Member
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                CreatedAt = DateTime.UtcNow,

                EmailConfirmed = false,

                EmailConfirmationToken =
                    Convert.ToHexString(
                        RandomNumberGenerator.GetBytes(32)),

                EmailConfirmationTokenExpiresAt =
                    DateTime.UtcNow.AddHours(24)
            };


            // ==========================================
            // HASH PASSWORD
            // ==========================================

            member.PasswordHash =
                hasher.HashPassword(
                    member,
                    model.Password);


            // ==========================================
            // SAVE MEMBER
            // ==========================================

            _db.Members.Add(member);

            await _db.SaveChangesAsync();


            // ==========================================
            // CREATE CONFIRMATION LINK
            // ==========================================

            var confirmationUrl =
                Url.Action(
                    "ConfirmEmail",
                    "Member",
                    new
                    {
                        memberId = member.Id,
                        token = member.EmailConfirmationToken
                    },
                    Request.Scheme);


            // ==========================================
            // SEND CONFIRMATION EMAIL
            // ==========================================

            try
            {
                var firstName =
                    System.Net.WebUtility.HtmlEncode(
                        member.FirstName);


                var safeConfirmationUrl =
                    System.Net.WebUtility.HtmlEncode(
                        confirmationUrl);


                var subject =
                    "Confirm your email — Sengele Ministries";


                var html = $@"
<div style='font-family:Arial,Helvetica,sans-serif;
            color:#222;
            line-height:1.6;
            max-width:650px;'>

    <h2 style='color:#111;'>
        SENGELE MINISTRIES
    </h2>

    <p>
        Hello {firstName},
    </p>

    <p>
        Thank you for creating your
        Sengele Ministries account.
    </p>

    <p>
        Please confirm your email address
        by clicking the button below.
    </p>

    <p style='margin:30px 0;'>

        <a href='{safeConfirmationUrl}'
           style='background:#111;
                  color:#ffffff;
                  padding:14px 24px;
                  text-decoration:none;
                  display:inline-block;
                  font-weight:600;'>

            CONFIRM MY EMAIL

        </a>

    </p>

    <p>
        This confirmation link will expire
        in 24 hours.
    </p>

    <p style='color:#666;'>
        If you did not create this account,
        you can ignore this email.
    </p>

</div>";


                await _emailSender.SendEmailAsync(
                    member.Email,
                    subject,
                    html);
            }
            catch (Exception ex)
            {
                // Member is already saved.
                // Email failure must not delete account.

                _logger.LogError(
                    ex,
                    "Error sending confirmation email " +
                    "for member {MemberId}",
                    member.Id);
            }


            // ==========================================
            // SUCCESS
            // ==========================================

            TempData["SuccessMessage"] =
                "Registration successful. " +
                "Please check your email to confirm your account.";


            return RedirectToAction("Login");
        }


        // ==========================================
        // CONFIRM EMAIL
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(
            int memberId,
            string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] =
                    "Invalid email confirmation link.";

                return RedirectToAction("Login");
            }


            var member = await _db.Members
                .FirstOrDefaultAsync(
                    m => m.Id == memberId);


            if (member == null)
            {
                TempData["ErrorMessage"] =
                    "Invalid email confirmation link.";

                return RedirectToAction("Login");
            }


            // Already confirmed
            if (member.EmailConfirmed)
            {
                TempData["SuccessMessage"] =
                    "Your email is already confirmed. " +
                    "You may log in.";

                return RedirectToAction("Login");
            }


            // Check token
            if (string.IsNullOrWhiteSpace(
                    member.EmailConfirmationToken) ||
                !string.Equals(
                    member.EmailConfirmationToken,
                    token,
                    StringComparison.Ordinal))
            {
                TempData["ErrorMessage"] =
                    "Invalid email confirmation link.";

                return RedirectToAction("Login");
            }


            // Check expiration
            if (member.EmailConfirmationTokenExpiresAt == null ||
                member.EmailConfirmationTokenExpiresAt <
                    DateTime.UtcNow)
            {
                TempData["ErrorMessage"] =
                    "This email confirmation link has expired.";

                return RedirectToAction("Login");
            }


            // ==========================================
            // CONFIRM EMAIL
            // ==========================================

            member.EmailConfirmed = true;
            member.EmailConfirmationToken = null;
            member.EmailConfirmationTokenExpiresAt = null;

            await _db.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Your email has been confirmed successfully. " +
                "You may now log in.";


            return RedirectToAction("Login");
        }

        // ==========================================
        // FORGOT PASSWORD - GET
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }



        // ==========================================
        // FORGOT PASSWORD - POST
        // ==========================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            var member = await _db.Members
                .FirstOrDefaultAsync(m => m.Email == email);

            if (member != null)
            {




                // ==========================================
                // GENERATE SECURE RESET TOKEN
                // ==========================================

                var token = Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(32));

                // Store only the token hash in database
                var tokenHash = SHA256.HashData(
                    Encoding.UTF8.GetBytes(token));

                member.PasswordResetToken =
                    Convert.ToHexString(tokenHash);

                // Token expires after 30 minutes
                member.PasswordResetTokenExpiresAt =
                    DateTime.UtcNow.AddMinutes(30);

                await _db.SaveChangesAsync();

                // ==========================================
                // CREATE PASSWORD RESET LINK
                // ==========================================

                var resetUrl = Url.Action(
                    "ResetPassword",
                    "Member",
                    new
                    {
                        memberId = member.Id,
                        token = token
                    },
                    Request.Scheme);

                // ==========================================
                // SEND PASSWORD RESET EMAIL
                // ==========================================

                if (string.IsNullOrEmpty(resetUrl))
                {
                    _logger.LogError(
                        "Could not generate reset URL for member {MemberId}",
                        member.Id);
                }
                else
                {
                    try
                    {
                        var safeUrl =
                            System.Net.WebUtility.HtmlEncode(resetUrl);

                        var subject =
                            "Reset your password - Sengele Ministries";

                        var html = $@"
                            <div style='font-family:Arial,Helvetica,sans-serif;
                                        color:#222;
                                        line-height:1.6;
                                        max-width:650px;'>

                                <h2>SENGELE MINISTRIES</h2>

                                <p>Hello,</p>

                                <p>
                                    We received a request to reset
                                    your account password.
                                </p>

                                <p style='margin:30px 0;'>
                                    <a href='{safeUrl}'
                                       style='background:#111;
                                              color:#ffffff;
                                              padding:14px 24px;
                                              text-decoration:none;
                                              display:inline-block;
                                              font-weight:600;'>
                                        RESET MY PASSWORD
                                    </a>
                                </p>

                                <p>
                                    This link will expire in 30 minutes.
                                </p>

                                <p>
                                    If you did not request a password reset,
                                    you can safely ignore this email.
                                </p>

                            </div>";

                        await _emailSender.SendEmailAsync(
                            member.Email,
                            subject,
                            html);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error sending password reset email for member {MemberId}",
                            member.Id);
                    }
                }
            }

            // ==========================================
            // SAME RESPONSE FOR ALL EMAIL ADDRESSES
            // ==========================================

            TempData["SuccessMessage"] =
                "If an account exists with this email, " +
                "a password reset link will be sent.";

            return RedirectToAction("Login");
        }





        // ==========================================
        // RESET PASSWORD - GET
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(int memberId, string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest("Invalid password reset link.");
            }

            var member = await _db.Members
                .FirstOrDefaultAsync(m => m.Id == memberId);

            if (member == null ||
                string.IsNullOrWhiteSpace(member.PasswordResetToken) ||
                member.PasswordResetTokenExpiresAt == null ||
                member.PasswordResetTokenExpiresAt <= DateTime.UtcNow)
            {
                return BadRequest("This password reset link is invalid or expired.");
            }

            var tokenHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(token)));

            if (!string.Equals(member.PasswordResetToken, tokenHash,
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("This password reset link is invalid or expired.");
            }

            var model = new ResetPasswordViewModel
            {
                MemberId = member.Id,
                Token = token
            };

            return View(model);
        }


        // ==========================================
        // LOGIN - GET
        // ==========================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // ==========================================
        // LOGIN - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var member = await _db.Members
                .FirstOrDefaultAsync(
                    m => m.Email == model.Email);


            if (member == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View(model);
            }


            var hasher =
                new Microsoft.AspNetCore.Identity
                    .PasswordHasher<Member>();


            // First try PasswordHasher verification
            var verify =
                hasher.VerifyHashedPassword(
                    member,
                    member.PasswordHash ?? string.Empty,
                    model.Password);


            if (verify ==
                Microsoft.AspNetCore.Identity
                    .PasswordVerificationResult.Failed)
            {
                // Support older SHA256 passwords
                var legacy =
                    HashPassword(model.Password);


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


                // Convert old password hash
                // to PasswordHasher
                member.PasswordHash =
                    hasher.HashPassword(
                        member,
                        model.Password);


                _db.Members.Update(member);

                await _db.SaveChangesAsync();
            }


            // ==========================================
            // CREATE CLAIMS
            // ==========================================

            var claims =
                new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims
                            .ClaimTypes.NameIdentifier,
                        member.Id.ToString()),

                    new System.Security.Claims.Claim(
                        System.Security.Claims
                            .ClaimTypes.Name,
                        member.FirstName + " " +
                        member.LastName),

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
                        .CookieAuthenticationDefaults
                        .AuthenticationScheme);


            var principal =
                new System.Security.Claims.ClaimsPrincipal(
                    identity);


            await HttpContext.SignInAsync(
                Microsoft.AspNetCore.Authentication.Cookies
                    .CookieAuthenticationDefaults
                    .AuthenticationScheme,
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
                    .CookieAuthenticationDefaults
                    .AuthenticationScheme);


            return RedirectToAction(
                "Index",
                "Home");
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
            var memberIdClaim =
                User.FindFirst("MemberId");


            if (memberIdClaim == null ||
                !int.TryParse(
                    memberIdClaim.Value,
                    out int memberId))
            {
                return RedirectToAction("Login");
            }


            var member = await _db.Members
                .FirstOrDefaultAsync(
                    m => m.Id == memberId);


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
            var memberIdClaim =
                User.FindFirst("MemberId");


            if (memberIdClaim == null ||
                !int.TryParse(
                    memberIdClaim.Value,
                    out int memberId))
            {
                return RedirectToAction("Login");
            }


            var member = await _db.Members
                .FirstOrDefaultAsync(
                    m => m.Id == memberId);


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
            var memberIdClaim =
                User.FindFirst("MemberId");


            if (memberIdClaim == null ||
                !int.TryParse(
                    memberIdClaim.Value,
                    out int memberId))
            {
                return RedirectToAction("Login");
            }


            var member = await _db.Members
                .FirstOrDefaultAsync(
                    m => m.Id == memberId);


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

            if (profileImage != null &&
                profileImage.Length > 0)
            {
                var allowedExtensions =
                    new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".webp"
                    };


                var extension =
                    Path.GetExtension(
                        profileImage.FileName)
                        .ToLowerInvariant();


                if (!allowedExtensions.Contains(
                    extension))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Please choose a JPG, PNG or WebP image.");

                    return View(member);
                }


                // Create profile image folder
                var profilesFolder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "images",
                        "profiles");


                Directory.CreateDirectory(
                    profilesFolder);


                // Create unique file name
                var fileName =
                    $"member-{member.Id}-" +
                    $"{Guid.NewGuid()}{extension}";


                var filePath =
                    Path.Combine(
                        profilesFolder,
                        fileName);


                // Save image
                using (var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
                {
                    await profileImage
                        .CopyToAsync(stream);
                }


                // Save image path
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
            var memberIdClaim =
                User.FindFirst("MemberId");


            if (memberIdClaim == null ||
                !int.TryParse(
                    memberIdClaim.Value,
                    out int memberId))
            {
                return RedirectToAction("Login");
            }


            var orders =
                await _db.Orders
                    .Where(
                        o => o.MemberId == memberId)
                    .OrderByDescending(
                        o => o.CreatedAt)
                    .ToListAsync();


            return View(orders);
        }


        // ==========================================
        // ORDER DETAILS
        // ==========================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> OrderDetails(
            int id)
        {
            var memberIdClaim =
                User.FindFirst("MemberId");


            if (memberIdClaim == null ||
                !int.TryParse(
                    memberIdClaim.Value,
                    out int memberId))
            {
                return RedirectToAction("Login");
            }


            var order =
                await _db.Orders
                    .Include(
                        o => o.OrderItems)
                    .FirstOrDefaultAsync(
                        o =>
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

        private static string HashPassword(
            string password)
        {
            using var sha =
                SHA256.Create();


            var bytes =
                Encoding.UTF8.GetBytes(
                    password ?? string.Empty);


            var hash =
                sha.ComputeHash(bytes);


            return Convert.ToHexString(hash);
        }
    }
}