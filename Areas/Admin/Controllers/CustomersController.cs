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
    [AutoValidateAntiforgeryToken]
    public class CustomersController : Controller
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? q, bool? isActive, string? sort, string? dir, int page = 1)
        {
            const int pageSize = 10;
            var query = BuildQuery(q, isActive, sort, dir);
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, totalPages > 0 ? Math.Min(page, totalPages) : 1);
            ViewData["Keyword"] = q;
            ViewData["IsActive"] = isActive;
            ViewData["Sort"] = sort ?? "id";
            ViewData["Dir"] = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
            ViewData["Page"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalItems"] = totalItems;
            return View(await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(string? q, bool? isActive, string? sort, string? dir)
        {
            var list = await BuildQuery(q, isActive, sort, dir).ToListAsync();
            var rows = list.Select(u => new object?[]
            {
                u.FullName, u.Email, u.PhoneNumber, u.Address, u.OrderCount,
                u.TotalSpent, u.IsActive ? "Hoạt động" : "Khoá", u.CreatedAt
            });
            var bytes = ExcelWorkbook.Build("Khách hàng",
                new[] { "Họ tên", "Email", "SĐT", "Địa chỉ", "Số đơn", "Tổng chi tiêu", "Trạng thái", "Ngày tạo" }, rows);
            return File(bytes, ExcelWorkbook.ContentType, $"khach-hang-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
        }

        private IQueryable<CustomerListItemViewModel> BuildQuery(string? q, bool? isActive, string? sort, string? dir)
        {
            var query = _context.Users.AsNoTracking().Where(u => u.Role == UserRole.Customer);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim();
                query = query.Where(u => u.FullName.Contains(keyword) || u.Email.Contains(keyword) ||
                    u.PhoneNumber.Contains(keyword) || u.Address.Contains(keyword));
            }
            if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive.Value);

            var projected = query.Select(u => new CustomerListItemViewModel
            {
                Id = u.Id, FullName = u.FullName, Email = u.Email, PhoneNumber = u.PhoneNumber,
                Address = u.Address, IsActive = u.IsActive, CreatedAt = u.CreatedAt,
                OrderCount = u.Orders.Count,
                TotalSpent = u.Orders.Where(o => o.Status != OrderStatus.Cancelled)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0
            });
            var desc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
            return (sort?.ToLowerInvariant()) switch
            {
                "name" => desc ? projected.OrderByDescending(x => x.FullName) : projected.OrderBy(x => x.FullName),
                "email" => desc ? projected.OrderByDescending(x => x.Email) : projected.OrderBy(x => x.Email),
                "phone" => desc ? projected.OrderByDescending(x => x.PhoneNumber) : projected.OrderBy(x => x.PhoneNumber),
                "address" => desc ? projected.OrderByDescending(x => x.Address) : projected.OrderBy(x => x.Address),
                "orders" => desc ? projected.OrderByDescending(x => x.OrderCount) : projected.OrderBy(x => x.OrderCount),
                "spent" => desc ? projected.OrderByDescending(x => x.TotalSpent) : projected.OrderBy(x => x.TotalSpent),
                "active" => desc ? projected.OrderByDescending(x => x.IsActive) : projected.OrderBy(x => x.IsActive),
                "created" => desc ? projected.OrderByDescending(x => x.CreatedAt) : projected.OrderBy(x => x.CreatedAt),
                _ => desc ? projected.OrderByDescending(x => x.Id) : projected.OrderBy(x => x.Id)
            };
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Customer);

            if (user == null) return NotFound();

            var orders = await _context.Orders
                .Include(o => o.Payments)
                .AsNoTracking()
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            ViewData["Customer"] = user;
            return View(orders);
        }
    }
}
