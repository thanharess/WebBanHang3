using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        /// <summary>Khóa phiên chat khách (cookie), hoặc userId nếu đã đăng nhập.</summary>
        [Required, StringLength(64)]
        public string SessionKey { get; set; } = string.Empty;

        [StringLength(100)]
        public string GuestName { get; set; } = "Khách";

        [Required, StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>true = tin từ Admin/NV; false = tin từ khách.</summary>
        public bool IsFromStaff { get; set; }

        public bool IsReadByStaff { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
