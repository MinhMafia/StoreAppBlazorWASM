using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreApp.Models
{
    [Table("import_items")]
    public class ImportItem
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("import_id")]
        public int ImportId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("unit_cost")]
        public decimal UnitCost { get; set; }

        [Column("total_cost")]
        public decimal TotalCost { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ImportId")]
        public virtual Import? Import { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
