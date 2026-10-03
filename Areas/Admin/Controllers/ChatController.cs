using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBanHang.Areas.Admin.ViewModels;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    [AutoValidateAntiforgeryToken]
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;
        private readonly StaffPresenceService _presence;

        public ChatController(AppDbContext context, StaffPresenceService presence)
        {
            _context = context;
            _presence = presence;
        }

        public async Task<IActionResult> Index()
        {
            var threads = await _context.ChatMessages
                .AsNoTracking()
                .GroupBy(m => m.SessionKey)
                .Select(g => new
                {
                    SessionKey = g.Key,
                    GuestName = g.OrderByDescending(x => x.Id).Select(x => x.GuestName).FirstOrDefault() ?? "Khách",
                    LastMessage = g.OrderByDescending(x => x.Id).Select(x => x.Content).FirstOrDefault(),
                    LastAt = g.Max(x => x.CreatedAt),
                    Unread = g.Count(x => !x.IsFromStaff && !x.IsReadByStaff)
                })
                .OrderByDescending(t => t.LastAt)
                .ToListAsync();

            ViewData["StaffPresence"] = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Admin || u.Role == UserRole.Staff)
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .Select(u => new StaffPresenceViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Role = u.Role,
                    IsOnline = _presence.IsOnline(u.Id)
                })
                .ToListAsync();

            return View(threads);
        }

        [HttpGet]
        public IActionResult Heartbeat()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out var userId))
            {
                _presence.MarkOnline(userId);
            }

            return NoContent();
        }

        public async Task<IActionResult> Thread(string sessionKey)
        {
            if (string.IsNullOrWhiteSpace(sessionKey)) return NotFound();

            var messages = await _context.ChatMessages
                .Where(m => m.SessionKey == sessionKey)
                .OrderBy(m => m.Id)
                .ToListAsync();

            foreach (var m in messages.Where(x => !x.IsFromStaff && !x.IsReadByStaff))
            {
                m.IsReadByStaff = true;
            }

            await _context.SaveChangesAsync();
            ViewData["SessionKey"] = sessionKey;
            ViewData["GuestName"] = messages.LastOrDefault()?.GuestName ?? "Khách";
            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> Reply(string sessionKey, string content)
        {
            content = (content ?? "").Trim();
            if (string.IsNullOrWhiteSpace(sessionKey) || string.IsNullOrEmpty(content) || content.Length > 1000)
            {
                TempData["Error"] = "Không gửi được tin nhắn.";
                return RedirectToAction(nameof(Index));
            }

            var guestName = await _context.ChatMessages
                .Where(m => m.SessionKey == sessionKey)
                .OrderByDescending(m => m.Id)
                .Select(m => m.GuestName)
                .FirstOrDefaultAsync() ?? "Khách";

            _context.ChatMessages.Add(new ChatMessage
            {
                SessionKey = sessionKey,
                GuestName = guestName,
                Content = content,
                IsFromStaff = true,
                IsReadByStaff = true,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Thread), new { sessionKey });
        }

        [HttpGet]
        public async Task<IActionResult> PollThread(string sessionKey, int afterId = 0)
        {
            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.SessionKey == sessionKey && m.Id > afterId)
                .OrderBy(m => m.Id)
                .Select(m => new
                {
                    m.Id,
                    m.Content,
                    m.IsFromStaff,
                    At = m.CreatedAt.ToString("HH:mm")
                })
                .ToListAsync();

            // đánh dấu đã đọc
            var unread = await _context.ChatMessages
                .Where(m => m.SessionKey == sessionKey && !m.IsFromStaff && !m.IsReadByStaff)
                .ToListAsync();
            foreach (var m in unread) m.IsReadByStaff = true;
            if (unread.Count > 0) await _context.SaveChangesAsync();

            return Json(new { messages });
        }
    }
}
