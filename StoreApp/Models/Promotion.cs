using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Models
{
    [Table("promotions")]
    [Index(nameof(Code), IsUnique = true, Name = "ux_promotions_code")]
    public class Promotion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("code")]
        [StringLength(100)]
        public string Code { get; set; } = string.Empty;

        // "percent" or "fixed" as in DB ENUM
        [Column("type")]
        [StringLength(50)]
        public string Type { get; set; } = "percent";

        [Column("value", TypeName = "decimal(12,2)")]
        public decimal Value { get; set; } = 0m;

        [Column("min_order_amount", TypeName = "decimal(12,2)")]
        public decimal MinOrderAmount { get; set; } = 0m;

        [Column("max_discount", TypeName = "decimal(12,2)")]
        public decimal? MaxDiscount { get; set; }

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Column("usage_limit")]
        public int? UsageLimit { get; set; }

        [Column("used_count")]
        public int UsedCount { get; set; } = 0;

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Soft delete
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public virtual ICollection<Order>? Orders { get; set; }
        public virtual ICollection<PromotionRedemption>? Redemptions { get; set; }
    }
}
