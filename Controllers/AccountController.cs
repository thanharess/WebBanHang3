using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly CaptchaService _captcha;

        public AccountController(AppDbContext context, CaptchaService captcha)
        {
            _context = context;
            _captcha = captcha;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            _captcha.Create(HttpContext);
            return View(new RegisterViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!_captcha.ValidateAndConsume(HttpContext, model.CaptchaCode))
            {
                ModelState.AddModelError(nameof(model.CaptchaCode), "Mã CAPTCHA không đúng hoặc đã hết hạn.");
            }

            if (!ModelState.IsValid)
            {
                _captcha.Create(HttpContext);
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null && !string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.AddModelError(nameof(model.Email), "Email này đã được đăng ký. Hãy đăng nhập.");
                return View(model);
            }

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Role = UserRole.Customer,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.Users.Add(user);
            }
            // Nếu user != null: đây là tài khoản được tạo tự động lúc khách đặt hàng trước đây,
            // giờ khách đăng ký nên chỉ cần đặt mật khẩu cho tài khoản đó.

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = model.PhoneNumber.Trim();
            user.Address = model.Address.Trim();
            user.PasswordHash = PasswordHasher.Hash(model.Password);

            await _context.SaveChangesAsync();
            await SignInAsync(user, isPersistent: false);

            TempData["Success"] = $"Đăng ký thành công. Xin chào {user.FullName}!";
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            _captcha.Create(HttpContext);
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!_captcha.ValidateAndConsume(HttpContext, model.CaptchaCode))
            {
                ModelState.AddModelError(nameof(model.CaptchaCode), "Mã CAPTCHA không đúng hoặc đã hết hạn.");
            }

            if (!ModelState.IsValid)
            {
                _captcha.Create(HttpContext);
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !user.IsActive || !PasswordHasher.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            await SignInAsync(user, model.RememberMe);
            TempData["Success"] = $"Xin chào {user.FullName}!";

            return Url.IsLocalUrl(model.ReturnUrl)
                ? Redirect(model.ReturnUrl!)
                : RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Bạn đã đăng xuất.";
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied() => View();

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            _captcha.Create(HttpContext);
            return View(new ForgotPasswordViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!_captcha.ValidateAndConsume(HttpContext, model.CaptchaCode))
            {
                ModelState.AddModelError(nameof(model.CaptchaCode), "Mã CAPTCHA không đúng hoặc đã hết hạn.");
            }

            if (!ModelState.IsValid)
            {
                _captcha.Create(HttpContext);
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();
            var phone = VietnamesePhoneAttribute.Normalize(model.PhoneNumber);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !user.IsActive
                || VietnamesePhoneAttribute.Normalize(user.PhoneNumber) != phone
                || string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty,
                    "Không tìm thấy tài khoản khớp email và số điện thoại, hoặc tài khoản chưa đặt mật khẩu (chỉ đặt hàng khách).");
                return View(model);
            }

            user.PasswordHash = PasswordHasher.Hash(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã đặt lại mật khẩu. Hãy đăng nhập bằng mật khẩu mới.";
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Captcha()
        {
            var code = _captcha.Create(HttpContext);
            return File(_captcha.RenderSvg(code), "image/svg+xml");
        }

        private async Task SignInAsync(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = isPersistent });
        }
    }
}
