using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class CartController : Controller
    {
        private readonly ICartService _cart;
        private readonly AppDbContext _context;

        public CartController(ICartService cart, AppDbContext context)
        {
            _cart = cart;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var cart = _cart.GetCart();
            await CartPricing.SyncAsync(_context, cart);
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                return NotFound();
            }

            if (quantity < 1)
            {
                quantity = 1;
            }

            var alreadyInCart = _cart.GetQuantity(productId);
            var remaining = product.StockQuantity - alreadyInCart;

            if (remaining <= 0)
            {
                TempData["Error"] = $"Sản phẩm \"{product.Name}\" chỉ còn {product.StockQuantity} trong kho.";
                return RedirectBack(returnUrl, productId);
            }

            if (quantity > remaining)
            {
                quantity = remaining;
                TempData["Error"] = $"\"{product.Name}\" chỉ còn {remaining} sản phẩm có thể thêm vào giỏ.";
            }
            else
            {
                TempData["Success"] = $"Đã thêm \"{product.Name}\" vào giỏ hàng.";
            }

            // Re-check remaining to guard against concurrent requests updating the session
            var alreadyInCartAfterCheck = _cart.GetQuantity(productId);
            var remainingAfterCheck = product.StockQuantity - alreadyInCartAfterCheck;

            if (remainingAfterCheck <= 0)
            {
                TempData["Error"] = $"Sản phẩm \"{product.Name}\" chỉ còn {product.StockQuantity} trong kho.";
                if (Request.Headers.TryGetValue("X-Requested-With", out var _xrwErr) && _xrwErr == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = TempData["Error"], count = _cart.GetItemCount(), cartTotal = _cart.GetTotal() });
                }

                return RedirectBack(returnUrl, productId);
            }

            // Cap quantity by remaining after re-check
            if (quantity > remainingAfterCheck)
            {
                quantity = remainingAfterCheck;
                TempData["Error"] = $"\"{product.Name}\" chỉ còn {remainingAfterCheck} sản phẩm có thể thêm vào giỏ.";
            }

            _cart.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.DiscountPrice ?? product.Price,
                Quantity = quantity,
                ImageUrl = product.ImageUrl
            });

            // Sync pricing/totals from DB and return updated cart info for AJAX callers
            var cart = _cart.GetCart();
            await CartPricing.SyncAsync(_context, cart);

            // If request comes from AJAX, return JSON with updated count, item id and totals
            if (Request.Headers.TryGetValue("X-Requested-With", out var _xrw) && _xrw == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    itemId = product.Id,
                    count = _cart.GetItemCount(),
                    cartTotal = _cart.GetTotal(),
                    message = TempData["Success"] ?? TempData["Error"] ?? string.Empty
                });
            }

            return RedirectBack(returnUrl, productId);
        }

        [HttpGet]
        public IActionResult Count()
        {
            return Json(new { count = _cart.GetItemCount() });
        }

        [HttpGet]
        public IActionResult Dropdown()
        {
            var miniCart = _cart.GetCart();
            return PartialView("_MiniCart", miniCart);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                _cart.Remove(productId);
                TempData["Error"] = "Sản phẩm không còn bán nên đã được xoá khỏi giỏ.";
                if (Request.Headers.TryGetValue("X-Requested-With", out var _xrw2) && _xrw2 == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = TempData["Error"], count = _cart.GetItemCount(), cartTotal = _cart.GetTotal() });
                }

                return RedirectToAction(nameof(Index));
            }

            if (quantity > product.StockQuantity)
            {
                quantity = product.StockQuantity;
                TempData["Error"] = $"\"{product.Name}\" chỉ còn {product.StockQuantity} sản phẩm trong kho.";
            }

            _cart.UpdateQuantity(productId, quantity);

            if (Request.Headers.TryGetValue("X-Requested-With", out var _xrw3) && _xrw3 == "XMLHttpRequest")
            {
                return Json(new { success = true, count = _cart.GetItemCount(), itemId = productId, quantity = quantity, cartTotal = _cart.GetTotal(), message = TempData["Error"] ?? TempData["Success"] });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            _cart.Remove(productId);
            TempData["Success"] = "Đã xoá sản phẩm khỏi giỏ hàng.";
            if (Request.Headers.TryGetValue("X-Requested-With", out var _xrw4) && _xrw4 == "XMLHttpRequest")
            {
                return Json(new { success = true, count = _cart.GetItemCount(), cartTotal = _cart.GetTotal(), message = TempData["Success"] });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Clear()
        {
            _cart.Clear();
            TempData["Success"] = "Đã xoá toàn bộ giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Quay lại trang khách vừa bấm, chỉ chấp nhận đường dẫn nội bộ.</summary>
        private IActionResult RedirectBack(string? returnUrl, int productId) =>
            Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl!)
                : RedirectToAction("Details", "Products", new { id = productId });
    }
}
