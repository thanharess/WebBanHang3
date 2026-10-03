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
    public class OrdersController : Controller
    {
        private const int PageSize = 10;

        /// <summary>Luồng trạng thái hợp lệ của một đơn hàng.</summary>
        private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
        {
            [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new[] { OrderStatus.Shipping, OrderStatus.Cancelled },
            [OrderStatus.Shipping] = new[] { OrderStatus.Completed, OrderStatus.Cancelled },
            [OrderStatus.Completed] = Array.Empty<OrderStatus>(),
            [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
        };

        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(OrderStatus? status, string? q, DateTime? dateFrom, DateTime? dateTo,
            decimal? totalMin, decimal? totalMax, string? sort, string? dir, int page = 1)
        {
            var query = BuildQuery(status, q, dateFrom, dateTo, totalMin, totalMax, sort, dir);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            if (page < 1)
            {
                page = 1;
            }
            else if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var model = new OrderListViewModel
            {
                Status = status,
                Keyword = q,
                DateFrom = dateFrom,
                DateTo = dateTo,
                TotalMin = totalMin,
                TotalMax = totalMax,
                Sort = sort ?? "id",
                Direction = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc",
                Page = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                AllCount = await _context.Orders.CountAsync(),
                StatusCounts = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count),
                Orders = await query
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(OrderStatus? status, string? q, DateTime? dateFrom, DateTime? dateTo,
            decimal? totalMin, decimal? totalMax, string? sort, string? dir)
        {
            var orders = await BuildQuery(status, q, dateFrom, dateTo, totalMin, totalMax, sort, dir).ToListAsync();
            var rows = orders.Select(o => new object?[]
            {
                o.OrderCode, o.User?.FullName ?? string.Empty, o.User?.PhoneNumber ?? string.Empty,
                o.OrderDate, o.TotalAmount, o.Payments.FirstOrDefault()?.Method.ToVietnamese() ?? string.Empty,
                o.Payments.FirstOrDefault()?.Status.ToVietnamese() ?? string.Empty, o.Status.ToVietnamese()
            });
            var bytes = ExcelWorkbook.Build("Đơn hàng",
                new[] { "Mã đơn", "Khách hàng", "Điện thoại", "Ngày đặt", "Tổng tiền", "Phương thức TT", "Trạng thái TT", "Trạng thái đơn" }, rows);
            return File(bytes, ExcelWorkbook.ContentType, $"don-hang-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
        }

        private IQueryable<Order> BuildQuery(OrderStatus? status, string? q, DateTime? dateFrom, DateTime? dateTo,
            decimal? totalMin, decimal? totalMax, string? sort, string? dir)
        {
            var query = _context.Orders.Include(o => o.User).Include(o => o.Payments).AsNoTracking().AsQueryable();
            if (status.HasValue) query = query.Where(o => o.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim();
                query = query.Where(o => o.OrderCode.Contains(keyword) ||
                    (o.User != null && (o.User.FullName.Contains(keyword) || o.User.PhoneNumber.Contains(keyword))));
            }
            if (dateFrom.HasValue) query = query.Where(o => o.OrderDate >= dateFrom.Value.Date);
            if (dateTo.HasValue) query = query.Where(o => o.OrderDate < dateTo.Value.Date.AddDays(1));
            if (totalMin.HasValue) query = query.Where(o => o.TotalAmount >= totalMin.Value);
            if (totalMax.HasValue) query = query.Where(o => o.TotalAmount <= totalMax.Value);

            var desc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
            return (sort?.ToLowerInvariant()) switch
            {
                "code" => desc ? query.OrderByDescending(o => o.OrderCode) : query.OrderBy(o => o.OrderCode),
                "customer" => desc ? query.OrderByDescending(o => o.User!.FullName) : query.OrderBy(o => o.User!.FullName),
                "phone" => desc ? query.OrderByDescending(o => o.User!.PhoneNumber) : query.OrderBy(o => o.User!.PhoneNumber),
                "date" => desc ? query.OrderByDescending(o => o.OrderDate) : query.OrderBy(o => o.OrderDate),
                "total" => desc ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
                "status" => desc ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
                _ => desc ? query.OrderByDescending(o => o.Id) : query.OrderBy(o => o.Id)
            };
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await LoadOrderAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            ViewData["AllowedNextStatuses"] = AllowedTransitions[order.Status];
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            // Phải dùng bản có theo dõi thay đổi thì SaveChanges mới ghi được
            var order = await LoadOrderForUpdateAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == status)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!AllowedTransitions[order.Status].Contains(status))
            {
                TempData["Error"] = $"Không thể chuyển đơn {order.OrderCode} từ \"{order.Status.ToVietnamese()}\" " +
                                    $"sang \"{status.ToVietnamese()}\".";
                return RedirectToAction(nameof(Details), new { id });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            if (status == OrderStatus.Cancelled)
            {
                // Huỷ đơn thì trả lại số lượng đã trừ khỏi kho
                var productIds = order.OrderDetails.Select(d => d.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                foreach (var detail in order.OrderDetails)
                {
                    if (products.TryGetValue(detail.ProductId, out var product))
                    {
                        product.StockQuantity += detail.Quantity;
                    }
                }

                // Đơn đã thu tiền thì ghi nhận đã hoàn tiền
                foreach (var payment in order.Payments.Where(p => p.Status == PaymentStatus.Paid))
                {
                    payment.Status = PaymentStatus.Refunded;
                }
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Đơn {order.OrderCode} đã chuyển sang \"{status.ToVietnamese()}\"." +
                                  (status == OrderStatus.Cancelled ? " Tồn kho đã được hoàn lại." : string.Empty);

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Sửa địa chỉ giao hàng khi đơn đang xử lý / đang giao.</summary>
        [HttpPost]
        public async Task<IActionResult> UpdateShipping(int id, string shippingAddress, string? note)
        {
            var order = await LoadOrderForUpdateAsync(id);
            if (order == null) return NotFound();

            if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled)
            {
                TempData["Error"] = "Không thể sửa địa chỉ khi đơn đã hoàn thành hoặc đã huỷ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            shippingAddress = (shippingAddress ?? "").Trim();
            if (string.IsNullOrWhiteSpace(shippingAddress) || shippingAddress.Length > 300)
            {
                TempData["Error"] = "Địa chỉ giao hàng không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.ShippingAddress = shippingAddress;
            if (note != null)
            {
                order.Note = note.Trim();
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật thông tin giao hàng cho đơn {order.OrderCode}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Ghi nhận đã thu được tiền (dùng cho COD và chuyển khoản ngân hàng).</summary>
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var order = await LoadOrderForUpdateAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                TempData["Error"] = $"Đơn {order.OrderCode} đã huỷ nên không thể ghi nhận thanh toán.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var payment = order.Payments.FirstOrDefault();
            if (payment == null)
            {
                TempData["Error"] = $"Đơn {order.OrderCode} chưa có bản ghi thanh toán.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (payment.Status == PaymentStatus.Paid)
            {
                TempData["Error"] = $"Đơn {order.OrderCode} đã được ghi nhận thanh toán trước đó.";
                return RedirectToAction(nameof(Details), new { id });
            }

            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã ghi nhận thanh toán cho đơn {order.OrderCode}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private Task<Order?> LoadOrderAsync(int id) =>
            _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

        /// <summary>Bản có theo dõi thay đổi, dùng cho thao tác cập nhật.</summary>
        private Task<Order?> LoadOrderForUpdateAsync(int id) =>
            _context.Orders
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

    }
}
