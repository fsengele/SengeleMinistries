using System.ComponentModel.DataAnnotations;

namespace SengeleMinistries.Models
{
    public class VolunteerApplication
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = null!;

        [Required]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Phone]
        public string? Phone { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        /* Address / location */
        [Display(Name = "Street address")]
        public string? StreetAddress { get; set; }

        [Display(Name = "Apartment or unit")]
        public string? ApartmentOrUnit { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        [Display(Name = "ZIP code")]
        public string? ZipCode { get; set; }

        public string? Country { get; set; }

        [Required]
        [Display(Name = "Service area")]
        public string ServiceArea { get; set; } = null!;

        [Display(Name = "Are you a current member?")]
        public bool IsCurrentMember { get; set; }

        [Display(Name = "Why would you like to serve?")]
        [StringLength(2000)]
        public string? WhyServe { get; set; }

        [Display(Name = "May we contact you by phone or email?")]
        public bool MayContact { get; set; } = true;

        [Display(Name = "Submitted at")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
