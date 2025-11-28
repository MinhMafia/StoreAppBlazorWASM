using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Models
{
    [Table("inventory")]
    [Index(nameof(ProductId), IsUnique = true, Name = "ux_inventory_product")]
    public class Inventory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        // FK -> products.id (one-to-one)
        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; } = 0;

        [Column("last_checked_at")]
        public DateTime? LastCheckedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation: Inventory -> Product (one-to-one)
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }
    }
}
