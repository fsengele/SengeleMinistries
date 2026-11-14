using System;
using System.ComponentModel.DataAnnotations;

namespace SengeleMinistries.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty; // Books, Teachings, EventCollections, Apparel

        [StringLength(500)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        [Range(0, 1000000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [StringLength(1000)]
        [DataType(DataType.ImageUrl)]
        public string? ImageUrl { get; set; }

        public int? StockQuantity { get; set; }

        // Identifies whether the product belongs to
        // Sengele Ministries or Amazon Affiliate.
        [Required]
        [StringLength(30)]
        public string ProductType { get; set; } = "OwnProduct";

        // Stores the Amazon affiliate link.
        // This field is optional for Sengele Ministries products.
        [StringLength(2000)]
        [Url]
        public string? AffiliateUrl { get; set; }

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
