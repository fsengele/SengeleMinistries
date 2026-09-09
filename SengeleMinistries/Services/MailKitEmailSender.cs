using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SengeleMinistries.Models;
using System.Threading.Tasks;

namespace SengeleMinistries.Services
{
    public class MailKitEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<MailKitEmailSender> _logger;

        public MailKitEmailSender(IOptions<EmailSettings> settings, ILogger<MailKitEmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName ?? "Sengele Ministries", _settings.FromEmail ?? "no-reply@localhost"));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using var client = new SmtpClient();
                // Disable certificate revocation checking only. Keep full certificate/hostname validation enabled.
                client.CheckCertificateRevocation = false;
                var secureSocket = _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, secureSocket);

                if (!string.IsNullOrWhiteSpace(_settings.SmtpUser))
                {
                    await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword ?? string.Empty);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (System.Exception ex)
            {
                // Do not rethrow - caller will log. But log here as well for diagnostics.
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }
    }
}
