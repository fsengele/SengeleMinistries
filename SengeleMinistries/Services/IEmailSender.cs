using System.Threading.Tasks;

namespace SengeleMinistries.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string htmlMessage);
    }
}
