
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SengeleMinistries.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ZipCode { get; set; } = string.Empty;

        // ==========================================
        // ORDER AMOUNTS
        // ==========================================
        public decimal Subtotal { get; set; }

        public decimal ShippingAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal Total { get; set; }

        // ==========================================
        // ORDER STATUS
        // ==========================================
        [Required]
        [StringLength(50)]
        public string OrderStatus { get; set; } = "Pending";

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        // ==========================================
        // STRIPE PAYMENT INFORMATION
        // ==========================================
        [StringLength(150)]
        public string? StripePaymentIntentId { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        public DateTime? PaidAt { get; set; }

        // ==========================================
        // ORDER DATE
        // ==========================================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ==========================================
        // ORDER ITEMS
        // ==========================================
        public List<OrderItem> OrderItems { get; set; } = new();
    }
}
