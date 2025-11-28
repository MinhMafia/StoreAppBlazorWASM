using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Models
{
    [Table("categories")]
    [Index(nameof(Slug), IsUnique = true, Name = "ux_categories_slug")]
    public class Category
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [StringLength(191)]
        public string Name { get; set; } = string.Empty;

        [Column("slug")]
        [StringLength(191)]
        public string? Slug { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation: 1 Category -> N Products
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
