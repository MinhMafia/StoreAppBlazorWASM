using System.ComponentModel.DataAnnotations;

namespace StoreApp.Models
{
    public class CustomerCart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public string CartJson { get; set; } = "[]";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Customer? Customer { get; set; }
    }
}

