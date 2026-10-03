using System.ComponentModel.DataAnnotations;
using WebBanHang.Helpers;

namespace WebBanHang.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [NoWhitespace]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã CAPTCHA")]
        [Display(Name = "Mã CAPTCHA")]
        public string CaptchaCode { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
