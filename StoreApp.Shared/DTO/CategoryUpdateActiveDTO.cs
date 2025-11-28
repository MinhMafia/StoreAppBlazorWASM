using System.ComponentModel.DataAnnotations;

namespace StoreApp.Shared
{
    public class CategoryUpdateActiveDTO
    {
        [Required(ErrorMessage = "IsActive is required")]
        public bool IsActive { get; set; }
    }
}

