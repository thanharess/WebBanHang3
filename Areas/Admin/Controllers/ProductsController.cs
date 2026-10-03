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
    public class ProductsController : Controller
    {
        private const int PageSize = 10;

        private readonly AppDbContext _context;
        private readonly ImageStorage _images;

        public ProductsController(AppDbContext context, ImageStorage images)
        {
            _context = context;
            _images = images;
        }

        public async Task<IActionResult> Index(string? q, int? categoryId, decimal? priceMin, decimal? priceMax,
            int? stockMin, int? stockMax, string? sort, string? dir, int page = 1)
        {
            var query = BuildQuery(q, categoryId, priceMin, priceMax, stockMin, stockMax, sort, dir);

            var desc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            if (page < 1) page = 1;
            else if (totalPages > 0 && page > totalPages) page = totalPages;

            ViewData["Keyword"] = q;
            ViewData["CategoryId"] = categoryId;
            ViewData["PriceMin"] = priceMin;
            ViewData["PriceMax"] = priceMax;
            ViewData["StockMin"] = stockMin;
            ViewData["StockMax"] = stockMax;
            ViewData["Sort"] = sort ?? "id";
            ViewData["Dir"] = desc ? "desc" : "asc";
            ViewData["Page"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalItems"] = totalItems;
            ViewData["Categories"] = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

            var products = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(string? q, int? categoryId, decimal? priceMin, decimal? priceMax,
            int? stockMin, int? stockMax, string? sort, string? dir)
        {
            var products = await BuildQuery(q, categoryId, priceMin, priceMax, stockMin, stockMax, sort, dir).ToListAsync();
            var rows = products.Select(p => new object?[]
            {
                p.Name, p.Category?.Name ?? string.Empty, p.Price, p.DiscountPrice,
                p.StockQuantity, p.Id
            });
            var bytes = ExcelWorkbook.Build("Sản phẩm",
                new[] { "Tên sản phẩm", "Danh mục", "Giá bán", "Giá khuyến mãi", "Tồn kho", "Mã" }, rows);
            return File(bytes, ExcelWorkbook.ContentType, $"san-pham-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
        }

        private IQueryable<Product> BuildQuery(string? q, int? categoryId, decimal? priceMin, decimal? priceMax,
            int? stockMin, int? stockMax, string? sort, string? dir)
        {
            var query = _context.Products.Include(p => p.Category).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim();
                query = query.Where(p => p.Name.Contains(keyword) || (p.Category != null && p.Category.Name.Contains(keyword)));
            }
            if (categoryId is > 0) query = query.Where(p => p.CategoryId == categoryId.Value);
            if (priceMin.HasValue) query = query.Where(p => p.Price >= priceMin.Value);
            if (priceMax.HasValue) query = query.Where(p => p.Price <= priceMax.Value);
            if (stockMin.HasValue) query = query.Where(p => p.StockQuantity >= stockMin.Value);
            if (stockMax.HasValue) query = query.Where(p => p.StockQuantity <= stockMax.Value);

            var desc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
            return (sort?.ToLowerInvariant()) switch
            {
                "name" => desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "category" => desc ? query.OrderByDescending(p => p.Category!.Name) : query.OrderBy(p => p.Category!.Name),
                "price" => desc ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "discount" => desc ? query.OrderByDescending(p => p.DiscountPrice) : query.OrderBy(p => p.DiscountPrice),
                "stock" => desc ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity),
                _ => desc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id)
            };
        }

        public async Task<IActionResult> Create()
        {
            var model = new ProductFormViewModel { StockQuantity = 1 };
            await LoadCategoriesAsync(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            ValidateDiscount(model);
            ValidateImageCounts(model, string.IsNullOrWhiteSpace(model.ImageUrl) ? 0 : 1);

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(model);
                return View(model);
            }

            string? imageUrl = model.ImageUrl?.Trim();
            var galleryUrls = new List<string>();

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                try
                {
                    imageUrl = await _images.SaveAsync(model.ImageFile);
                    galleryUrls.Add(imageUrl);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
                    await LoadCategoriesAsync(model);
                    return View(model);
                }
            }

            try
            {
                foreach (var file in model.GalleryFiles.Where(f => f.Length > 0))
                {
                    galleryUrls.Add(await _images.SaveAsync(file));
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(model.GalleryFiles), ex.Message);
                await LoadCategoriesAsync(model);
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name.Trim(),
                CategoryId = model.CategoryId,
                Price = model.Price,
                DiscountPrice = model.DiscountPrice,
                StockQuantity = model.StockQuantity,
                Description = model.Description?.Trim() ?? string.Empty,
                ContentNote = model.ContentNote?.Trim() ?? string.Empty,
                ImageUrl = imageUrl ?? string.Empty
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            await AddProductImagesAsync(product, galleryUrls, model.ContentImageFiles);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm sản phẩm \"{product.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.Include(p => p.Images).AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductFormViewModel
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                StockQuantity = product.StockQuantity,
                Description = product.Description,
                ContentNote = product.ContentNote,
                ImageUrl = product.ImageUrl,
                CurrentImageUrl = product.ImageUrl
            };
            model.ExistingImages = product.Images.OrderBy(i => i.IsContentImage).ThenBy(i => i.SortOrder).ToList();

            await LoadCategoriesAsync(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
        {
            var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            ValidateDiscount(model);
            var existingGalleryCount = product.Images.Count(i => !i.IsContentImage && !model.DeleteImageIds.Contains(i.Id));
            if (!string.IsNullOrWhiteSpace(product.ImageUrl) && !product.Images.Any(i => !i.IsContentImage && i.Url == product.ImageUrl))
            {
                existingGalleryCount++;
            }
            ValidateImageCounts(model, existingGalleryCount);
            model.CurrentImageUrl = product.ImageUrl;
            model.ExistingImages = product.Images.OrderBy(i => i.IsContentImage).ThenBy(i => i.SortOrder).ToList();

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(model);
                return View(model);
            }

            if (model.ImageFile == null && !string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                product.ImageUrl = model.ImageUrl.Trim();
            }

            var deletedImages = product.Images.Where(i => model.DeleteImageIds.Contains(i.Id)).ToList();
            foreach (var image in deletedImages)
            {
                product.Images.Remove(image);
                _context.ProductImages.Remove(image);
            }

            var gallerySort = product.Images.Where(i => !i.IsContentImage).Select(i => i.SortOrder).DefaultIfEmpty(-1).Max() + 1;
            var contentSort = product.Images.Where(i => i.IsContentImage).Select(i => i.SortOrder).DefaultIfEmpty(-1).Max() + 1;
            foreach (var file in model.GalleryFiles.Where(f => f.Length > 0))
            {
                product.Images.Add(new ProductImage { Url = await _images.SaveAsync(file), SortOrder = gallerySort++, IsContentImage = false });
            }
            foreach (var file in model.ContentImageFiles.Where(f => f.Length > 0))
            {
                product.Images.Add(new ProductImage { Url = await _images.SaveAsync(file), SortOrder = contentSort++, IsContentImage = true });
            }
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var newImageUrl = await _images.SaveAsync(model.ImageFile);
                _images.Delete(product.ImageUrl);
                product.ImageUrl = newImageUrl;
                product.Images.Add(new ProductImage { Url = newImageUrl, SortOrder = 0, IsContentImage = false });
            }

            product.Name = model.Name.Trim();
            product.CategoryId = model.CategoryId;
            product.Price = model.Price;
            product.DiscountPrice = model.DiscountPrice;
            product.StockQuantity = model.StockQuantity;
            product.Description = model.Description?.Trim() ?? string.Empty;
            product.ContentNote = model.ContentNote?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();
            foreach (var image in deletedImages)
            {
                _images.Delete(image.Url);
            }

            TempData["Success"] = $"Đã cập nhật sản phẩm \"{product.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        private void ValidateImageCounts(ProductFormViewModel model, int existingGalleryCount)
        {
            var newGalleryCount = model.GalleryFiles.Count(f => f.Length > 0) +
                (model.ImageFile?.Length > 0 ? 1 : 0);
            if (existingGalleryCount + newGalleryCount > 9)
            {
                ModelState.AddModelError(nameof(model.GalleryFiles), "Mỗi sản phẩm được tối đa 9 ảnh chính.");
            }
            if (model.ContentImageFiles.Count(f => f.Length > 0) > 30)
            {
                ModelState.AddModelError(nameof(model.ContentImageFiles), "Mỗi lần chỉ được tải tối đa 30 ảnh ghi chú.");
            }
        }

        private async Task AddProductImagesAsync(Product product, IEnumerable<string> galleryUrls, IEnumerable<IFormFile> contentFiles)
        {
            var sort = 0;
            foreach (var url in galleryUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                product.Images.Add(new ProductImage { Url = url, SortOrder = sort++, IsContentImage = false });
            }
            foreach (var file in contentFiles.Where(f => f.Length > 0))
            {
                product.Images.Add(new ProductImage { Url = await _images.SaveAsync(file), SortOrder = sort++, IsContentImage = true });
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewData["UsedInOrders"] = await _context.OrderDetails.AnyAsync(d => d.ProductId == id);
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            var imageUrl = product.ImageUrl;

            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                _images.Delete(imageUrl);

                TempData["Success"] = $"Đã xoá sản phẩm \"{product.Name}\".";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = $"Không xoá được \"{product.Name}\" vì sản phẩm đã có trong đơn hàng. " +
                                    "Hãy đặt số lượng tồn kho = 0 để ngừng bán thay vì xoá.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void ValidateDiscount(ProductFormViewModel model)
        {
            if (model.DiscountPrice.HasValue && model.DiscountPrice.Value >= model.Price)
            {
                ModelState.AddModelError(nameof(model.DiscountPrice), "Giá khuyến mãi phải nhỏ hơn giá bán.");
            }
        }

        private async Task LoadCategoriesAsync(ProductFormViewModel model)
        {
            model.Categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
