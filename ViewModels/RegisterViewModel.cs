using System.ComponentModel.DataAnnotations;
using WebBanHang.Helpers;

namespace WebBanHang.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [VietnamesePhone]
        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [NoWhitespace]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [DataType(DataType.Password)]
        [NoWhitespace]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp")]
        [Display(Name = "Nhập lại mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã CAPTCHA")]
        [Display(Name = "Mã CAPTCHA")]
        public string CaptchaCode { get; set; } = string.Empty;
    }
}
