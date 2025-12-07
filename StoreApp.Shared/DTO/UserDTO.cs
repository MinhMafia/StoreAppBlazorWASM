using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StoreApp.Shared
{
    public class UserDTO
    {
        public int Id { get; set; }

        [Required]
        [MinLength(3)]
        [MaxLength(150)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email is invalid.")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? FullName { get; set; }

        [Required]
        [RegularExpression("^(admin|staff|customer)$", ErrorMessage = "Role must be either 'admin', 'staff', or 'customer'.")]
        public string Role { get; set; } = "staff";

        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }

        [MinLength(6)]
        [MaxLength(128)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Password { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}
