using Microsoft.EntityFrameworkCore;
using SengeleMinistries.Models;

namespace SengeleMinistries.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ContactMessage> ContactMessages { get; set; } = null!;

        public DbSet<VolunteerApplication> VolunteerApplications { get; set; } = null!;

        public DbSet<Member> Members { get; set; } = null!;

        public DbSet<Product> Products { get; set; } = null!;

        public DbSet<Order> Orders { get; set; } = null!;

        public DbSet<OrderItem> OrderItems { get; set; } = null!;

        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; } = null!;

        public DbSet<PrayerRequest> PrayerRequests { get; set; } = null!;
    }
}