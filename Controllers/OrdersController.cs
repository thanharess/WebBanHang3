using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBanHang.Helpers;
using WebBanHang.Models;
using WebBanHang.Services;
using WebBanHang.ViewModels;

namespace WebBanHang.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class OrdersController : Controller
    {
        /// <summary>
        /// Danh sách mã đơn mà khách đã chứng minh được quyền xem trong phiên này
        /// (nhờ tra cứu đúng mã đơn kèm số điện thoại/email).
        /// </summary>
        private const string TrackedOrdersKey = "TrackedOrderIds";
        private const string LastOrderSessionKey = "LastOrderId";

        private readonly AppDbContext _context;
        private readonly ICartService _cart;

        public OrdersController(AppDbContext context, ICartService cart)
        {
            _context = context;
            _cart = cart;
        }

        /// <summary>Danh sách đơn hàng của tài khoản đang đăng nhập.</summary>
        [Authorize]
        public async Task<IActionResult> Index()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index)) });
            }

            var orders = await _context.Orders
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(orders);
        }

        /// <summary>Tra cứu đơn hàng không cần đăng nhập.</summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Track() => View(new TrackOrderViewModel());

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Track(TrackOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();
            var code = model.OrderCode?.Trim();

            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payments)
                .AsNoTracking()
                .Where(o => o.User != null && o.User.Email.ToLower() == email);

            if (!string.IsNullOrWhiteSpace(code))
            {
                query = query.Where(o => o.OrderCode == code);
            }

            var orders = await query.OrderByDescending(o => o.Id).ToListAsync();

            if (orders.Count == 0)
            {
                ModelState.AddModelError(string.Empty,
                    "Không tìm thấy đơn hàng với email này" +
                    (string.IsNullOrWhiteSpace(code) ? "." : " và mã đơn đã nhập."));
                return View(model);
            }

            foreach (var o in orders)
            {
                RememberTrackedOrder(o.Id);
            }

            // Một đơn → chi tiết; nhiều đơn → danh sách kết quả
            if (orders.Count == 1)
            {
                return RedirectToAction(nameof(Details), new { id = orders[0].Id });
            }

            return View("TrackResults", orders);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || !CanViewOrder(order))
            {
                return NotFound();
            }

            return View(order);
        }

        /// <summary>Khách sửa tên/SĐT/địa chỉ khi đơn chưa giao (Pending/Confirmed).</summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateShippingInfo(int id, string fullName, string phoneNumber, string shippingAddress, string? note)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || !CanViewOrder(order))
            {
                return NotFound();
            }

            if (order.Status is not OrderStatus.Pending and not OrderStatus.Confirmed)
            {
                TempData["Error"] = "Chỉ sửa được đơn khi chưa chuyển sang giao hàng.";
                return RedirectToAction(nameof(Details), new { id });
            }

            fullName = (fullName ?? "").Trim();
            phoneNumber = VietnamesePhoneAttribute.Normalize(phoneNumber);
            shippingAddress = (shippingAddress ?? "").Trim();

            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100
                || !new VietnamesePhoneAttribute().IsValid(phoneNumber)
                || string.IsNullOrWhiteSpace(shippingAddress) || shippingAddress.Length > 300)
            {
                TempData["Error"] = "Thông tin không hợp lệ. Kiểm tra họ tên, SĐT và địa chỉ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (order.User != null)
            {
                order.User.FullName = fullName;
                order.User.PhoneNumber = phoneNumber;
            }

            order.ShippingAddress = shippingAddress;
            if (note != null)
            {
                order.Note = note.Trim();
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật thông tin giao hàng.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Khách huỷ đơn khi chưa giao (hoàn kho).</summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || !CanViewOrder(order))
            {
                return NotFound();
            }

            if (order.Status is not OrderStatus.Pending and not OrderStatus.Confirmed)
            {
                TempData["Error"] = "Chỉ huỷ được đơn khi chưa giao hàng.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            var productIds = order.OrderDetails.Select(d => d.ProductId).Distinct().ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
            foreach (var detail in order.OrderDetails)
            {
                if (products.TryGetValue(detail.ProductId, out var product))
                {
                    product.StockQuantity += detail.Quantity;
                }
            }

            foreach (var payment in order.Payments.Where(p => p.Status == PaymentStatus.Paid))
            {
                payment.Status = PaymentStatus.Refunded;
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = $"Đã huỷ đơn {order.OrderCode}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Đặt lại: đưa sản phẩm từ đơn đã huỷ vào giỏ.</summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Reorder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || !CanViewOrder(order))
            {
                return NotFound();
            }

            if (order.Status != OrderStatus.Cancelled)
            {
                TempData["Error"] = "Chỉ đặt lại được đơn đã huỷ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var added = 0;
            foreach (var d in order.OrderDetails)
            {
                if (d.Product == null || d.Product.StockQuantity < 1) continue;
                var qty = Math.Min(d.Quantity, d.Product.StockQuantity);
                _cart.Add(new CartItem
                {
                    ProductId = d.ProductId,
                    ProductName = d.Product.Name,
                    Price = d.Product.DiscountPrice ?? d.Product.Price,
                    Quantity = qty,
                    ImageUrl = d.Product.ImageUrl
                });
                added++;
            }

            if (added == 0)
            {
                TempData["Error"] = "Không thể thêm sản phẩm vào giỏ (hết hàng hoặc đã xoá).";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = "Đã thêm sản phẩm từ đơn cũ vào giỏ hàng.";
            return RedirectToAction("Index", "Cart");
        }

        private bool CanViewOrder(Order order)
        {
            if (GetTrackedOrderIds().Contains(order.Id) ||
                HttpContext.Session.GetInt32(LastOrderSessionKey) == order.Id)
            {
                return true;
            }

            return TryGetCurrentUserId(out var userId) && order.UserId == userId;
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out userId);
        }

        private List<int> GetTrackedOrderIds() =>
            HttpContext.Session.Get<List<int>>(TrackedOrdersKey) ?? new List<int>();

        private void RememberTrackedOrder(int orderId)
        {
            var ids = GetTrackedOrderIds();
            if (!ids.Contains(orderId))
            {
                ids.Add(orderId);
                HttpContext.Session.Set(TrackedOrdersKey, ids);
            }
        }
    }
}
