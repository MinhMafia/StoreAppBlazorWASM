using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreApp.Models
{
    [Table("imports")]
    public class Import
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("import_number")]
        [MaxLength(100)]
        public string ImportNumber { get; set; } = string.Empty;

        [Column("supplier_id")]
        public int? SupplierId { get; set; }

        [Column("staff_id")]
        public int? StaffId { get; set; }

        [Column("status")]
        [MaxLength(50)]
        public string Status { get; set; } = "pending"; // pending, completed, cancelled

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        [ForeignKey("StaffId")]
        public virtual User? Staff { get; set; }

        public virtual ICollection<ImportItem> ImportItems { get; set; } = new List<ImportItem>();
    }
}
