namespace WebBanHang.Areas.Admin.ViewModels
{
    public class CustomerListItemViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
