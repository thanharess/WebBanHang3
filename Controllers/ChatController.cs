using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class ChatController : Controller
    {
        private const string CookieName = "CHT_ChatSession";
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Mỗi khách một SessionKey riêng:
        /// - Đã đăng nhập: user-{id}
        /// - Khách ẩn danh: cookie GUID
        /// </summary>
        private string GetOrCreateSessionKey()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(id))
                {
                    return "user-" + id;
                }
            }

            if (Request.Cookies.TryGetValue(CookieName, out var key) && !string.IsNullOrWhiteSpace(key))
            {
                return key;
            }

            key = "guest-" + Guid.NewGuid().ToString("N");
            Response.Cookies.Append(CookieName, key, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.Now.AddDays(30)
            });
            return key;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Poll(int afterId = 0)
        {
            var session = GetOrCreateSessionKey();
            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.SessionKey == session && m.Id > afterId)
                .OrderBy(m => m.Id)
                .Select(m => new
                {
                    m.Id,
                    m.Content,
                    m.IsFromStaff,
                    m.GuestName,
                    At = m.CreatedAt.ToString("HH:mm")
                })
                .ToListAsync();

            return Json(new { session, messages });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Send(string content, string? guestName, string? gender)
        {
            content = (content ?? "").Trim();
            if (string.IsNullOrEmpty(content) || content.Length > 1000)
            {
                return BadRequest(new { error = "Tin nhắn không hợp lệ." });
            }

            var session = GetOrCreateSessionKey();
            var name = string.IsNullOrWhiteSpace(guestName) ? "Khách" : guestName.Trim();
            if (User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name))
            {
                name = User.Identity.Name!;
            }

            gender = (gender ?? "").Trim();
            if (gender is "Nam" or "Nữ")
            {
                // Hiển thị trên admin: "Nam · Nguyễn Văn A"
                if (!name.StartsWith(gender, StringComparison.OrdinalIgnoreCase))
                {
                    name = gender + " · " + name;
                }
            }

            if (name.Length > 100) name = name[..100];

            var msg = new ChatMessage
            {
                SessionKey = session,
                GuestName = name,
                Content = content,
                IsFromStaff = false,
                IsReadByStaff = false,
                CreatedAt = DateTime.Now
            };

            _context.ChatMessages.Add(msg);
            await _context.SaveChangesAsync();

            return Json(new
            {
                id = msg.Id,
                content = msg.Content,
                isFromStaff = false,
                at = msg.CreatedAt.ToString("HH:mm")
            });
        }
    }
}
