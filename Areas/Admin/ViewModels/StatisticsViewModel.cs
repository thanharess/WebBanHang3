namespace WebBanHang.Areas.Admin.ViewModels
{
    public class YearRevenueComparisonViewModel
    {
        public int Year { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public decimal ChangePercent { get; set; }
    }

    public class StatisticsViewModel
    {
        public decimal TotalRevenue { get; set; }

        public int TotalOrders { get; set; }

        public int CancelledOrders { get; set; }

        public decimal AverageOrderValue { get; set; }

        public int CustomerCount { get; set; }

        public int SelectedYear { get; set; }

        public int SelectedMonth { get; set; }

        public decimal SelectedMonthRevenue { get; set; }

        public decimal PreviousMonthRevenue { get; set; }

        public decimal MonthChangePercent { get; set; }

        public List<int> AvailableMonths { get; set; } = Enumerable.Range(1, 12).ToList();

        public List<int> AvailableYears { get; set; } = new List<int>();

        public List<string> YearLabels { get; set; } = new List<string>();

        public List<decimal> YearlyRevenue { get; set; } = new List<decimal>();

        public List<int> YearlyOrders { get; set; } = new List<int>();

        public List<YearRevenueComparisonViewModel> YearComparisons { get; set; } = new List<YearRevenueComparisonViewModel>();

        // Biểu đồ doanh thu 30 ngày gần nhất
        public List<string> DailyLabels { get; set; } = new List<string>();

        public List<decimal> DailyRevenue { get; set; } = new List<decimal>();

        public List<int> DailyOrders { get; set; } = new List<int>();

        // Biểu đồ doanh thu 12 tháng gần nhất
        public List<string> MonthlyLabels { get; set; } = new List<string>();

        public List<decimal> MonthlyRevenue { get; set; } = new List<decimal>();

        // Top sản phẩm bán chạy
        public List<string> TopProductNames { get; set; } = new List<string>();

        public List<int> TopProductQuantities { get; set; } = new List<int>();

        // Tỉ lệ đơn theo trạng thái
        public List<string> StatusLabels { get; set; } = new List<string>();

        public List<int> StatusCounts { get; set; } = new List<int>();
    }
}
