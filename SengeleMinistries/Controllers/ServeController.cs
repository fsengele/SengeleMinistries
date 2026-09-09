using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SengeleMinistries.Data;
using SengeleMinistries.Models;
using SengeleMinistries.Services;

namespace SengeleMinistries.Controllers
{
    public class ServeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ServeController> _logger;

        public ServeController(ApplicationDbContext db, IEmailSender emailSender, ILogger<ServeController> logger)
        {
            _db = db;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(VolunteerApplication model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.SubmittedAt = DateTime.UtcNow;
            _db.VolunteerApplications.Add(model);
            await _db.SaveChangesAsync();

            // Prepare admin notification email (HTML)
            try
            {
                var subject = "New Serve With Us Application — Sengele Ministries";
                var html = $@"<div style='font-family: Arial, Helvetica, sans-serif; color:#222; line-height:1.4'>
<h2 style='color:#111;'>NEW SERVE WITH US APPLICATION</h2>
<p>A new volunteer application has been submitted through Sengele Ministries.</p>
<table style='width:100%; border-collapse:collapse; margin-top:16px;'>
  <tr><td style='padding:6px; font-weight:600; width:220px;'>Application ID</td><td style='padding:6px'>{model.Id}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>First name</td><td style='padding:6px'>{model.FirstName}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Last name</td><td style='padding:6px'>{model.LastName}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Email</td><td style='padding:6px'>{model.Email}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Phone</td><td style='padding:6px'>{model.Phone ?? string.Empty}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Date of birth</td><td style='padding:6px'>{(model.DateOfBirth.HasValue ? model.DateOfBirth.Value.ToString("yyyy-MM-dd") : string.Empty)}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Country</td><td style='padding:6px'>{model.Country ?? string.Empty}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Street address</td><td style='padding:6px'>{model.StreetAddress ?? string.Empty}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Apartment / Unit</td><td style='padding:6px'>{model.ApartmentOrUnit ?? string.Empty}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>City</td><td style='padding:6px'>{model.City ?? string.Empty}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>State / Province</td><td style='padding:6px'>{model.State ?? string.Empty}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Postal / ZIP</td><td style='padding:6px'>{model.ZipCode ?? string.Empty}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Service area</td><td style='padding:6px'>{model.ServiceArea}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Current member</td><td style='padding:6px'>{(model.IsCurrentMember ? "Yes" : "No")}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Permission to contact</td><td style='padding:6px'>{(model.MayContact ? "Yes" : "No")}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Why serve</td><td style='padding:6px'>{(string.IsNullOrWhiteSpace(model.WhyServe) ? string.Empty : System.Net.WebUtility.HtmlEncode(model.WhyServe))}</td></tr>
  <tr><td style='padding:6px; font-weight:600;'>Submitted at (UTC)</td><td style='padding:6px'>{model.SubmittedAt:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
</table>
<p style='margin-top:18px; color:#666;'>This application has been saved securely in the Sengele Ministries database.</p>
</div>";

                // Send to admin email configured in EmailSettings (MailKitEmailSender will use settings)
                // Fetch AdminEmail from configuration via DI from RequestServices
                var config = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Options.IOptions<SengeleMinistries.Models.EmailSettings>)) as Microsoft.Extensions.Options.IOptions<SengeleMinistries.Models.EmailSettings>;
                var adminEmail = config?.Value?.AdminEmail;
                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    await _emailSender.SendEmailAsync(adminEmail, subject, html);
                }
                else
                {
                    _logger.LogWarning("AdminEmail is not configured. Skipping admin notification email.");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending Serve With Us notification for application {ApplicationId}", model.Id);
            }

            return RedirectToAction("ThankYou");
        }

        [HttpGet]
        public IActionResult ThankYou()
        {
            return View();
        }
    }
}

