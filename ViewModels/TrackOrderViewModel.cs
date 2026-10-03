using System.ComponentModel.DataAnnotations;

namespace WebBanHang.ViewModels
{
    /// <summary>Tra cứu đơn: bắt buộc email; mã đơn tùy chọn.</summary>
    public class TrackOrderViewModel
    {
        [StringLength(30)]
        [Display(Name = "Mã đơn hàng (không bắt buộc)")]
        public string? OrderCode { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email đã dùng khi đặt hàng")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
