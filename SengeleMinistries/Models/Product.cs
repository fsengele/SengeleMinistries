
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
        public string Category { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        [Range(0, 1000000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        // ==========================================
        // PRODUCT IMAGES
        // ==========================================

        [StringLength(1000)]
        [DataType(DataType.ImageUrl)]
        public string? ImageUrl { get; set; }

        [StringLength(1000)]
        [DataType(DataType.ImageUrl)]
        public string? ImageUrl2 { get; set; }

        [StringLength(1000)]
        [DataType(DataType.ImageUrl)]
        public string? ImageUrl3 { get; set; }

        [StringLength(1000)]
        [DataType(DataType.ImageUrl)]
        public string? ImageUrl4 { get; set; }

        // ==========================================
        // INVENTORY
        // ==========================================

        public int? StockQuantity { get; set; }

        // ==========================================
        // PRODUCT TYPE
        // ==========================================

        [Required]
        [StringLength(30)]
        public string ProductType { get; set; } = "OwnProduct";

        [StringLength(2000)]
        [Url]
        public string? AffiliateUrl { get; set; }

        // ==========================================
        // SHIPPING INFORMATION
        // ==========================================

        // True = physical product
        // False = digital product
        public bool RequiresShipping { get; set; } = true;

        // Product weight in ounces
        [Range(0.01, 100000)]
        public decimal? WeightOz { get; set; }

        // Product length in inches
        [Range(0.01, 10000)]
        public decimal? LengthIn { get; set; }

        // Product width in inches
        [Range(0.01, 10000)]
        public decimal? WidthIn { get; set; }

        // Product height in inches
        [Range(0.01, 10000)]
        public decimal? HeightIn { get; set; }

        // ==========================================
        // PUBLICATION
        // ==========================================

        public bool IsPublished { get; set; } = false;

        // ==========================================
        // DATES
        // ==========================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
