using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Models
{
    [Table("customers")]
    [Index(nameof(Phone), IsUnique = true, Name = "ux_customers_phone")]
    [Index(nameof(Email), IsUnique = true, Name = "ux_customers_email")]
    public class Customer
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("email")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        [StringLength(512)]
        public string? PasswordHash { get; set; }

        [Required]
        [Column("full_name")]
        [StringLength(255)]
        public string FullName { get; set; } = string.Empty;

        [Column("phone")]
        [StringLength(50)]
        public string? Phone { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation: one customer -> many orders, many promotion redemptions
        public virtual ICollection<Order>? Orders { get; set; }
        public virtual ICollection<PromotionRedemption>? PromotionRedemptions { get; set; }
    }
}
