using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Areas.Admin.ViewModels;
using WebBanHang.Helpers;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StatisticsController : Controller
    {
        private const int TopProductCount = 5;

        private readonly AppDbContext _context;

        public StatisticsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? year, int? month)
        {
            var today = DateTime.Today;
            var selectedYear = year ?? today.Year;
            var selectedMonth = month is >= 1 and <= 12 ? month.Value : today.Month;
            var firstMonth = new DateTime(selectedYear, 1, 1);
            var nextYear = firstMonth.AddYears(1);
            var selectedMonthStart = new DateTime(selectedYear, selectedMonth, 1);
            var previousMonthStart = selectedMonthStart.AddMonths(-1);

            // Doanh thu chỉ tính các đơn không bị huỷ
            var validOrders = _context.Orders.Where(o => o.Status != OrderStatus.Cancelled);

            var monthly = await validOrders
                .Where(o => o.OrderDate >= firstMonth && o.OrderDate < nextYear)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            var selectedMonthRevenue = await validOrders
                .Where(o => o.OrderDate >= selectedMonthStart && o.OrderDate < selectedMonthStart.AddMonths(1))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var previousMonthRevenue = await validOrders
                .Where(o => o.OrderDate >= previousMonthStart && o.OrderDate < selectedMonthStart)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var yearly = await validOrders
                .GroupBy(o => o.OrderDate.Year)
                .Select(g => new { Year = g.Key, Revenue = g.Sum(o => o.TotalAmount), Count = g.Count() })
                .OrderBy(x => x.Year)
                .ToListAsync();

            var topProducts = await _context.OrderDetails
                .Where(d => d.Order!.Status != OrderStatus.Cancelled)
                .GroupBy(d => d.Product!.Name)
                .Select(g => new { Name = g.Key, Quantity = g.Sum(d => d.Quantity) })
                .OrderByDescending(x => x.Quantity)
                .Take(TopProductCount)
                .ToListAsync();

            var statusGroups = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var model = new StatisticsViewModel
            {
                TotalOrders = await _context.Orders.CountAsync(),
                CancelledOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled),
                TotalRevenue = await validOrders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m,
                CustomerCount = await _context.Users.CountAsync(u => u.Role == UserRole.Customer)
                ,SelectedYear = selectedYear
                ,SelectedMonth = selectedMonth
                ,SelectedMonthRevenue = selectedMonthRevenue
                ,PreviousMonthRevenue = previousMonthRevenue
                ,MonthChangePercent = ChangePercent(selectedMonthRevenue, previousMonthRevenue)
                ,AvailableYears = yearly.Select(x => x.Year).ToList()
            };
            if (!model.AvailableYears.Contains(selectedYear)) model.AvailableYears.Add(selectedYear);
            model.AvailableYears = model.AvailableYears.OrderByDescending(x => x).ToList();

            var validOrderCount = model.TotalOrders - model.CancelledOrders;
            model.AverageOrderValue = validOrderCount > 0 ? model.TotalRevenue / validOrderCount : 0m;

            var monthlyMap = monthly.ToDictionary(x => (x.Year, x.Month), x => x);
            for (var i = 0; i < 12; i++)
            {
                var monthDate = firstMonth.AddMonths(i);
                model.MonthlyLabels.Add($"T{monthDate.Month}/{monthDate.Year}");
                model.MonthlyRevenue.Add(monthlyMap.TryGetValue((monthDate.Year, monthDate.Month), out var m) ? m.Revenue : 0m);
            }

            foreach (var item in yearly)
            {
                model.YearLabels.Add(item.Year.ToString());
                model.YearlyRevenue.Add(item.Revenue);
                model.YearlyOrders.Add(item.Count);
            }

            for (var i = 0; i < yearly.Count; i++)
            {
                var previousRevenue = i > 0 ? yearly[i - 1].Revenue : 0m;
                model.YearComparisons.Add(new YearRevenueComparisonViewModel
                {
                    Year = yearly[i].Year,
                    Revenue = yearly[i].Revenue,
                    Orders = yearly[i].Count,
                    ChangePercent = ChangePercent(yearly[i].Revenue, previousRevenue)
                });
            }

            foreach (var item in topProducts)
            {
                model.TopProductNames.Add(item.Name);
                model.TopProductQuantities.Add(item.Quantity);
            }

            foreach (var status in Enum.GetValues<OrderStatus>())
            {
                model.StatusLabels.Add(status.ToVietnamese());
                model.StatusCounts.Add(statusGroups.FirstOrDefault(g => g.Status == status)?.Count ?? 0);
            }

            return View(model);
        }

        private static decimal ChangePercent(decimal current, decimal previous) =>
            previous == 0m ? (current > 0m ? 100m : 0m) : (current - previous) / previous * 100m;

        [HttpGet]
        public async Task<IActionResult> ExportExcel(int? year, int? month)
        {
            var selectedYear = year ?? DateTime.Today.Year;
            var selectedMonth = month is >= 1 and <= 12 ? month.Value : DateTime.Today.Month;
            var from = new DateTime(selectedYear, selectedMonth, 1);
            var to = from.AddMonths(1);
            var orders = await _context.Orders
                .Include(o => o.User)
                .AsNoTracking()
                .Where(o => o.Status != OrderStatus.Cancelled && o.OrderDate >= from && o.OrderDate < to)
                .OrderBy(o => o.OrderDate)
                .ToListAsync();
            var rows = orders.Select(o => new object?[]
            {
                o.OrderCode, o.OrderDate, o.User?.FullName ?? string.Empty,
                o.User?.Email ?? string.Empty, o.TotalAmount, o.Status.ToVietnamese()
            });
            var bytes = ExcelWorkbook.Build("Chi tiết doanh thu",
                new[] { "Mã đơn hàng", "Ngày đặt", "Khách hàng", "Email", "Tổng tiền", "Trạng thái" }, rows);
            return File(bytes, ExcelWorkbook.ContentType, $"thong-ke-{selectedYear}-{selectedMonth:00}.xlsx");
        }
    }
}
