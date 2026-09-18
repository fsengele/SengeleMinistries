using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using SengeleMinistries.Services;
using System.Security.Claims;
using System.Net;

namespace SengeleMinistries.Controllers
{
    public class PrayerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IOptions<EmailSettings> _emailSettings;
        private readonly ILogger<PrayerController> _logger;

        public PrayerController(
            ApplicationDbContext context,
            IEmailSender emailSender,
            IOptions<EmailSettings> emailSettings,
            ILogger<PrayerController> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _emailSettings = emailSettings;
            _logger = logger;
        }


        // =========================================
        // PRAYER FORM
        // =========================================

        [HttpGet]
        public IActionResult Index()
        {
            return View(new PrayerRequest());
        }


        // =========================================
        // SAVE PRAYER REQUEST + SEND EMAIL
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            PrayerRequest prayerRequest)
        {
            if (!ModelState.IsValid)
            {
                return View(prayerRequest);
            }


            // =====================================
            // DATE
            // =====================================

            prayerRequest.SubmittedAt = DateTime.UtcNow;


            // =====================================
            // CONNECT TO LOGGED-IN MEMBER
            // =====================================

            if (User.Identity != null &&
                User.Identity.IsAuthenticated)
            {
                var memberEmail =
                    User.FindFirstValue(ClaimTypes.Email)
                    ?? User.Identity.Name;

                prayerRequest.MemberEmail = memberEmail;
            }


            // =====================================
            // SAVE TO DATABASE
            // =====================================

            _context.PrayerRequests.Add(prayerRequest);

            await _context.SaveChangesAsync();


            // =====================================
            // SEND EMAIL TO ADMIN
            // =====================================

            try
            {
                var adminEmail =
                    _emailSettings.Value.AdminEmail;

                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    var subject =
                        "New Prayer Request — Sengele Ministries";


                    var firstName =
                        WebUtility.HtmlEncode(
                            prayerRequest.FirstName);

                    var lastName =
                        WebUtility.HtmlEncode(
                            prayerRequest.LastName);

                    var email =
                        WebUtility.HtmlEncode(
                            prayerRequest.Email);

                    var phone =
                        WebUtility.HtmlEncode(
                            prayerRequest.Phone ?? "");

                    var request =
                        WebUtility.HtmlEncode(
                            prayerRequest.Request)
                            .Replace("\r\n", "<br />")
                            .Replace("\n", "<br />");


                    var html = $@"
<div style='font-family:Arial,Helvetica,sans-serif;
            color:#222;
            line-height:1.6;
            max-width:700px;'>

    <h2 style='color:#111;'>
        NEW PRAYER REQUEST
    </h2>

    <p>
        A new prayer request has been submitted
        through Sengele Ministries.
    </p>

    <table style='width:100%;
                  border-collapse:collapse;
                  margin-top:20px;'>

        <tr>
            <td style='padding:8px;
                       font-weight:600;
                       width:180px;'>
                Request ID
            </td>

            <td style='padding:8px;'>
                {prayerRequest.Id}
            </td>
        </tr>


        <tr>
            <td style='padding:8px;
                       font-weight:600;'>
                First Name
            </td>

            <td style='padding:8px;'>
                {firstName}
            </td>
        </tr>


        <tr>
            <td style='padding:8px;
                       font-weight:600;'>
                Last Name
            </td>

            <td style='padding:8px;'>
                {lastName}
            </td>
        </tr>


        <tr>
            <td style='padding:8px;
                       font-weight:600;'>
                Email
            </td>

            <td style='padding:8px;'>
                {email}
            </td>
        </tr>


        <tr>
            <td style='padding:8px;
                       font-weight:600;'>
                Phone
            </td>

            <td style='padding:8px;'>
                {phone}
            </td>
        </tr>


        <tr>
            <td style='padding:8px;
                       font-weight:600;
                       vertical-align:top;'>
                Prayer Request
            </td>

            <td style='padding:8px;'>
                {request}
            </td>
        </tr>


        <tr>
            <td style='padding:8px;
                       font-weight:600;'>
                Submitted At
            </td>

            <td style='padding:8px;'>
                {prayerRequest.SubmittedAt:yyyy-MM-dd HH:mm:ss} UTC
            </td>
        </tr>

    </table>


    <p style='margin-top:22px;
              color:#666;'>

        This prayer request has also been saved
        in the Sengele Ministries database.

    </p>

</div>";


                    await _emailSender.SendEmailAsync(
                        adminEmail,
                        subject,
                        html);
                }
                else
                {
                    _logger.LogWarning(
                        "AdminEmail is not configured. " +
                        "Prayer request {PrayerRequestId} " +
                        "was saved but email was not sent.",
                        prayerRequest.Id);
                }
            }
            catch (Exception ex)
            {
                // IMPORTANT:
                // The prayer request is already saved.
                // An email problem must not delete the request.

                _logger.LogError(
                    ex,
                    "Error sending prayer request email " +
                    "for request {PrayerRequestId}",
                    prayerRequest.Id);
            }


            // =====================================
            // SUCCESS
            // =====================================

            TempData["PrayerSuccess"] =
                "Your prayer request was sent successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================
        // MY PRAYER REQUESTS
        // =========================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            var memberEmail =
                User.FindFirstValue(ClaimTypes.Email)
                ?? User.Identity?.Name;


            if (string.IsNullOrWhiteSpace(memberEmail))
            {
                return RedirectToAction(
                    "Login",
                    "Member");
            }


            var requests =
                await _context.PrayerRequests
                    .Where(p =>
                        p.MemberEmail == memberEmail)
                    .OrderByDescending(p =>
                        p.SubmittedAt)
                    .ToListAsync();


            return View(requests);
        }
    }
}