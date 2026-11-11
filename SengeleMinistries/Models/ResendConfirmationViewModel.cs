
using System.ComponentModel.DataAnnotations;

namespace SengeleMinistries.Models
{
    public class ResendConfirmationViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;
    }
}

