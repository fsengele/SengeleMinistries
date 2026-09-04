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
    }
}
