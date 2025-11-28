using System.ComponentModel.DataAnnotations;

namespace StoreApp.Shared
{
    public class CategoryCreateDTO
    {
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(191, ErrorMessage = "Name must be at most 191 characters long")]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters long")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description must be at most 500 characters long")]
        public string? Description { get; set; }
    }
}

