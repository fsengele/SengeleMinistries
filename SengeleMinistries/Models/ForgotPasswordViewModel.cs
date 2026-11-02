
using System.ComponentModel.DataAnnotations;

namespace SengeleMinistries.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
