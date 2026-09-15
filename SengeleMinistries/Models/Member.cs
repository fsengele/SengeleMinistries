using System.ComponentModel.DataAnnotations;

namespace SengeleMinistries.Models
{
    public class Member
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [Display(Name = "Street address")]
        public string? StreetAddress { get; set; }

        public string? City { get; set; }

        [Display(Name = "State / Province")]
        public string? State { get; set; }

        [Display(Name = "ZIP / Postal code")]
        public string? ZipCode { get; set; }

        public string? Country { get; set; }

        [Display(Name = "Profile picture")]
        public string? ProfileImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Administrator flag - do not expose in public forms
        public bool IsAdmin { get; set; } = false;
    }
}