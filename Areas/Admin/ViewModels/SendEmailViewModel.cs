using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Areas.Admin.ViewModels
{
    public class SendEmailViewModel
    {
        public int? OrderId { get; set; }
        public int? CustomerId { get; set; }

        [Required(ErrorMessage = "Nhập email người nhận")]
        [EmailAddress]
        [Display(Name = "Gửi tới")]
        public string ToEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập tiêu đề")]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập nội dung")]
        [Display(Name = "Nội dung")]
        public string Body { get; set; } = string.Empty;

        public string TemplateKey { get; set; } = "order_confirm";

        public List<OrderPickItem> RecentOrders { get; set; } = new();
        public List<CustomerPickItem> Customers { get; set; } = new();
        public Dictionary<string, string> TagValues { get; set; } = new();
    }

    public class OrderPickItem
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public class CustomerPickItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string LabelByName { get; set; } = "";
        public string LabelByEmail { get; set; } = "";
    }
}
