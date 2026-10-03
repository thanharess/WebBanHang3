using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.ViewModels
{
    public class StaffPresenceViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsOnline { get; set; }
    }
}
