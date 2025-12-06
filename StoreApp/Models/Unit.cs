using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Models
{
    [Index(nameof(Code), IsUnique = true, Name = "ux_units_code")]
    [Table("units")]
    public class Unit
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("code")]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Column("name")]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation property: 1 Unit có nhiều Product
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}