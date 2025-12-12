using System.ComponentModel.DataAnnotations;

namespace StoreApp.Models
{
    public class UserCart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string CartJson { get; set; } = "[]";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
