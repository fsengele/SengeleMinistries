using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Models;

namespace SengeleMinistries.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ContactMessage> ContactMessages { get; set; } = null!;
        public DbSet<SengeleMinistries.Models.VolunteerApplication> VolunteerApplications { get; set; } = null!;
        public DbSet<SengeleMinistries.Models.Member>? Members { get; set; }
        public DbSet<SengeleMinistries.Models.Product> Products { get; set; } = null!;
    }
}
