using System.ComponentModel.DataAnnotations;

namespace StoreApp.Shared
{
    public class CustomerCreateDTO
    {
        [Required(ErrorMessage = "FullName is required")]
        [MaxLength(150, ErrorMessage = "FullName must be at most 150 characters long")]
        [MinLength(3, ErrorMessage = "FullName must be at least 3 characters long")]
        [RegularExpression(@"^[a-zA-Z\sÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ]+$", ErrorMessage = "FullName can only contain letters and spaces")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
        [RegularExpression("^(0|\\+84)[35789]\\d{8}$", ErrorMessage = "Invalid phone number format")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address format")]
        public string? Email { get; set; }

        [MaxLength(250, ErrorMessage = "Address must be at most 250 characters long")]
        public string? Address { get; set; }
    }
}