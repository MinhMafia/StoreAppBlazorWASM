using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Models
{
    [Table("order_items")]
    [Index(nameof(OrderId), Name = "idx_oi_order")]
    [Index(nameof(ProductId), Name = "idx_oi_product")]
    public class OrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; } = 1;

        [Column("unit_price", TypeName = "decimal(12,2)")]
        public decimal UnitPrice { get; set; } = 0m;

        [Column("total_price", TypeName = "decimal(12,2)")]
        public decimal TotalPrice { get; set; } = 0m;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }
    }
}
