using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SengeleMinistries.Data;

#nullable disable

namespace SengeleMinistries.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            modelBuilder.Entity("SengeleMinistries.Models.ContactMessage", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");

                b.Property<string>("Email").IsRequired().HasColumnType("TEXT");

                b.Property<bool>("IsRead").HasColumnType("INTEGER");

                b.Property<string>("FullName").IsRequired().HasColumnType("TEXT");

                b.Property<string>("Message").IsRequired().HasColumnType("TEXT");

                b.Property<DateTime>("SubmittedAt").HasColumnType("TEXT");

                b.Property<string>("Subject").IsRequired().HasColumnType("TEXT");

                b.Property<string>("Phone").HasColumnType("TEXT");

                b.HasKey("Id");

                b.ToTable("ContactMessages");
            });
        }
    }
}
