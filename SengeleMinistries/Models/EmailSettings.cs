namespace SengeleMinistries.Models
{
    public class EmailSettings
    {
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string? FromName { get; set; }
        public string? FromEmail { get; set; }
        public string? AdminEmail { get; set; }
        // SmtpUser and SmtpPassword will be provided via user-secrets or env vars
        public string? SmtpUser { get; set; }
        public string? SmtpPassword { get; set; }
    }
}
