using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Areas.Admin.ViewModels;
using WebBanHang.Helpers;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    [AutoValidateAntiforgeryToken]
    public class EmailController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _email;

        public EmailController(AppDbContext context, EmailService email)
        {
            _context = context;
            _email = email;
        }

        /// <summary>3 mẫu email sẵn + thẻ {{...}}.</summary>
        public static readonly Dictionary<string, (string Name, string Subject, string Body)> Templates = new()
        {
            ["order_confirm"] = (
                "Xác nhận đơn hàng",
                "CH&T GOLF — Xác nhận đơn {{MaDon}}",
                """
                <p>Xin chào <strong>{{TenKhach}}</strong>,</p>
                <p>Cảm ơn bạn đã đặt hàng tại <strong>CH&T GOLF</strong>.</p>
                <p><strong>Mã đơn:</strong> {{MaDon}}<br/>
                <strong>Ngày đặt:</strong> {{NgayDat}}<br/>
                <strong>Tổng tiền:</strong> {{TongTien}} đ<br/>
                <strong>Trạng thái:</strong> {{TrangThai}}</p>
                <p><strong>Địa chỉ giao:</strong> {{DiaChi}}<br/>
                <strong>SĐT:</strong> {{SDT}}<br/>
                <strong>Email:</strong> {{Email}}</p>
                <p>Chúng tôi sẽ liên hệ khi đơn được xử lý. Trân trọng!</p>
                """
            ),
            ["shipping"] = (
                "Đơn đang giao",
                "CH&T GOLF — Đơn {{MaDon}} đang được giao",
                """
                <p>Xin chào <strong>{{TenKhach}}</strong>,</p>
                <p>Đơn hàng <strong>{{MaDon}}</strong> của bạn đang được giao.</p>
                <p><strong>Địa chỉ nhận:</strong> {{DiaChi}}<br/>
                <strong>SĐT liên hệ:</strong> {{SDT}}<br/>
                <strong>Tổng tiền:</strong> {{TongTien}} đ</p>
                <p>Vui lòng giữ máy để shipper liên hệ. Cảm ơn bạn!</p>
                """
            ),
            ["thank_you"] = (
                "Cảm ơn / Hoàn tất",
                "CH&T GOLF — Cảm ơn bạn đã mua hàng ({{MaDon}})",
                """
                <p>Xin chào <strong>{{TenKhach}}</strong>,</p>
                <p>Đơn <strong>{{MaDon}}</strong> đã hoàn tất. Cảm ơn bạn đã tin tưởng <strong>CH&T GOLF</strong>.</p>
                <p>Nếu cần hỗ trợ thêm, hãy trả lời email này hoặc chat trên website.</p>
                <p>Trân trọng,<br/>Đội ngũ CH&T GOLF</p>
                """
            )
        };

        [HttpGet]
        public async Task<IActionResult> Index(int? orderId, int? customerId)
        {
            var model = await BuildModelAsync(orderId, customerId, "order_confirm");
            ViewData["EmailConfigured"] = _email.IsConfigured;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(SendEmailViewModel model)
        {
            ViewData["EmailConfigured"] = _email.IsConfigured;
            model.RecentOrders = await LoadRecentOrdersAsync();
            model.Customers = await LoadCustomersAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var html = model.Body.Contains('<') ? model.Body : WebUtility.HtmlEncode(model.Body).Replace("\n", "<br/>");
                // Nếu body đã là HTML từ template thì gửi trực tiếp
                if (!model.Body.TrimStart().StartsWith("<"))
                {
                    html = "<div style='font-family:sans-serif;line-height:1.5'>" +
                           WebUtility.HtmlEncode(model.Body).Replace("\n", "<br/>") + "</div>";
                }
                else
                {
                    html = model.Body;
                }

                await _email.SendAsync(model.ToEmail.Trim(), model.Subject.Trim(), html);
                TempData["Success"] = $"Đã gửi email tới {model.ToEmail}.";
                return RedirectToAction(nameof(Index), new { orderId = model.OrderId, customerId = model.CustomerId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Gửi thất bại: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ApplyTemplate(string key, int? orderId, int? customerId)
        {
            if (!Templates.ContainsKey(key)) key = "order_confirm";
            var model = await BuildModelAsync(orderId, customerId, key);
            ViewData["EmailConfigured"] = _email.IsConfigured;
            return View("Index", model);
        }

        private async Task<SendEmailViewModel> BuildModelAsync(int? orderId, int? customerId, string templateKey)
        {
            var tags = await ResolveTagsAsync(orderId, customerId);
            if (!Templates.TryGetValue(templateKey, out var tpl))
            {
                tpl = Templates["order_confirm"];
                templateKey = "order_confirm";
            }

            var subject = ReplaceTags(tpl.Subject, tags);
            var body = ReplaceTags(tpl.Body, tags);

            var email = tags.GetValueOrDefault("Email", "");
            return new SendEmailViewModel
            {
                OrderId = orderId,
                CustomerId = customerId,
                ToEmail = email,
                Subject = subject,
                Body = body,
                TemplateKey = templateKey,
                RecentOrders = await LoadRecentOrdersAsync(),
                Customers = await LoadCustomersAsync(),
                TagValues = tags
            };
        }

        private async Task<Dictionary<string, string>> ResolveTagsAsync(int? orderId, int? customerId)
        {
            var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["MaDon"] = "",
                ["NgayDat"] = "",
                ["TongTien"] = "",
                ["TrangThai"] = "",
                ["DiaChi"] = "",
                ["SDT"] = "",
                ["Email"] = "",
                ["TenKhach"] = "",
                ["GhiChu"] = ""
            };

            Order? order = null;
            if (orderId.HasValue)
            {
                order = await _context.Orders
                    .Include(o => o.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId.Value);
            }

            if (order != null)
            {
                tags["MaDon"] = order.OrderCode;
                tags["NgayDat"] = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
                tags["TongTien"] = order.TotalAmount.ToString("N0");
                tags["TrangThai"] = order.Status.ToVietnamese();
                tags["DiaChi"] = order.ShippingAddress ?? "";
                tags["GhiChu"] = order.Note ?? "";
                tags["TenKhach"] = order.User?.FullName ?? "";
                tags["SDT"] = order.User?.PhoneNumber ?? "";
                tags["Email"] = order.User?.Email ?? "";
                return tags;
            }

            if (customerId.HasValue)
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == customerId.Value);
                if (user != null)
                {
                    tags["TenKhach"] = user.FullName;
                    tags["SDT"] = user.PhoneNumber ?? "";
                    tags["Email"] = user.Email;
                    tags["DiaChi"] = user.Address ?? "";
                }
            }

            return tags;
        }

        private static string ReplaceTags(string text, Dictionary<string, string> tags)
        {
            return Regex.Replace(text, @"\{\{(\w+)\}\}", m =>
            {
                var key = m.Groups[1].Value;
                return tags.TryGetValue(key, out var val) ? val : m.Value;
            });
        }


        private async Task<List<CustomerPickItem>> LoadCustomersAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Customer && u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new CustomerPickItem
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    LabelByName = u.FullName + " — " + u.Email + (u.PhoneNumber != null && u.PhoneNumber != "" ? " · " + u.PhoneNumber : ""),
                    LabelByEmail = u.Email + " — " + u.FullName
                })
                .Take(200)
                .ToListAsync();
        }

        private async Task<List<OrderPickItem>> LoadRecentOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.User)
                .AsNoTracking()
                .OrderByDescending(o => o.Id)
                .Take(30)
                .Select(o => new OrderPickItem
                {
                    Id = o.Id,
                    OrderCode = o.OrderCode,
                    CustomerName = o.User != null ? o.User.FullName : "",
                    Email = o.User != null ? o.User.Email : "",
                    Label = o.OrderCode + " — " + (o.User != null ? o.User.FullName : "") + " (" + (o.User != null ? o.User.Email : "") + ")"
                })
                .ToListAsync();
        }
    }
}
