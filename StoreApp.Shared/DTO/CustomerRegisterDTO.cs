using System.ComponentModel.DataAnnotations;

namespace StoreApp.Shared
{
    public class CustomerRegisterDTO
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [MinLength(3, ErrorMessage = "Họ tên phải có ít nhất 3 ký tự")]
        [MaxLength(150, ErrorMessage = "Họ tên không được quá 150 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [RegularExpression(@"^(0|\+84)[35789]\d{8}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        [MaxLength(250, ErrorMessage = "Địa chỉ không được quá 250 ký tự")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [MaxLength(128, ErrorMessage = "Mật khẩu không được quá 128 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}