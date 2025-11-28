using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Models
{
    [Table("inventory_adjustments")]
    [Index(nameof(ProductId), Name = "idx_inv_adj_product")]
    [Index(nameof(UserId), Name = "idx_inv_adj_user")]
    public class InventoryAdjustment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("change_amount")]
        public int ChangeAmount { get; set; }

        [Column("reason")]
        [StringLength(255)]
        public string? Reason { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}
