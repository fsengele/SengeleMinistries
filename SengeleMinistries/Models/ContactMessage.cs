using System;
using System.ComponentModel.DataAnnotations;

namespace SengeleMinistries.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.MultilineText)]
        public string Message { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}
