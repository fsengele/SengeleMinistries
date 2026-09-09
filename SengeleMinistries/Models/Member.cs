using System.ComponentModel.DataAnnotations;

namespace SengeleMinistries.Models
{
    public class Member
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Stored password hash (simple SHA256-based hash for demo; do not use for production)
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Administrator flag - do not expose in public registration forms
        public bool IsAdmin { get; set; } = false;
    }
}
