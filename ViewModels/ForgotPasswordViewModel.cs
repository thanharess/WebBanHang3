using System.ComponentModel.DataAnnotations;
using WebBanHang.Helpers;

namespace WebBanHang.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại đã đăng ký")]
        [VietnamesePhone]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [NoWhitespace]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [DataType(DataType.Password)]
        [NoWhitespace]
        [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp")]
        [Display(Name = "Nhập lại mật khẩu mới")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã CAPTCHA")]
        [Display(Name = "Mã CAPTCHA")]
        public string CaptchaCode { get; set; } = string.Empty;
    }
}
