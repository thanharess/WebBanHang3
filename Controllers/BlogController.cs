using Microsoft.AspNetCore.Mvc;

namespace WebBanHang.Controllers
{
    public class BlogController : Controller
    {
        private static readonly List<BlogPost> Posts = new()
        {
            new BlogPost
            {
                Id = 1,
                Title = "Bí quyết chọn gậy golf phù hợp cho người mới",
                Summary = "Hướng dẫn chọn gậy driver, iron và putter phù hợp với chiều cao, swing speed và trình độ của bạn.",
                Content = @"<p>Chọn gậy golf đúng giúp bạn đánh bóng ổn định hơn ngay từ những vòng đầu.</p>
<p><strong>1. Driver:</strong> Người mới nên chọn loft 10.5°–12° và shaft flex Regular để dễ bay bóng.</p>
<p><strong>2. Iron:</strong> Bộ cavity-back (game improvement) giúp bóng bay cao và thẳng hơn blade.</p>
<p><strong>3. Putter:</strong> Thử nhiều kiểu grip và alignment line trên putting green trước khi mua.</p>
<p>Đến CH&T GOLF để được đo fitting và tư vấn trực tiếp.</p>",
                ImageUrl = "/images/spec-club.svg",
                PublishedAt = new DateTime(2026, 9, 15),
                Author = "CH&T GOLF"
            },
            new BlogPost
            {
                Id = 2,
                Title = "Bí quyết chọn gậy golf phù hợp cho người mới",
                Summary = "Hướng dẫn chọn gậy driver, iron và putter phù hợp với chiều cao, swing speed và trình độ của bạn.",
                Content = @"<p>Chọn gậy golf đúng giúp bạn đánh bóng ổn định hơn ngay từ những vòng đầu.</p>
<p><strong>1. Driver:</strong> Người mới nên chọn loft 10.5°–12° và shaft flex Regular để dễ bay bóng.</p>
<p><strong>2. Iron:</strong> Bộ cavity-back (game improvement) giúp bóng bay cao và thẳng hơn blade.</p>
<p><strong>3. Putter:</strong> Thử nhiều kiểu grip và alignment line trên putting green trước khi mua.</p>
<p>Đến CH&T GOLF để được đo fitting và tư vấn trực tiếp.</p>",
                ImageUrl = "/images/spec-club.svg",
                PublishedAt = new DateTime(2026, 9, 20),
                Author = "CH&T GOLF"
            },
            new BlogPost
            {
                Id = 3,
                Title = "Bí quyết chọn gậy golf phù hợp cho người mới",
                Summary = "Hướng dẫn chọn gậy driver, iron và putter phù hợp với chiều cao, swing speed và trình độ của bạn.",
                Content = @"<p>Chọn gậy golf đúng giúp bạn đánh bóng ổn định hơn ngay từ những vòng đầu.</p>
<p><strong>1. Driver:</strong> Người mới nên chọn loft 10.5°–12° và shaft flex Regular để dễ bay bóng.</p>
<p><strong>2. Iron:</strong> Bộ cavity-back (game improvement) giúp bóng bay cao và thẳng hơn blade.</p>
<p><strong>3. Putter:</strong> Thử nhiều kiểu grip và alignment line trên putting green trước khi mua.</p>
<p>Đến CH&T GOLF để được đo fitting và tư vấn trực tiếp.</p>",
                ImageUrl = "/images/spec-club.svg",
                PublishedAt = new DateTime(2026, 9, 28),
                Author = "CH&T GOLF"
            }
        };

        public IActionResult Index()
        {
            ViewData["Title"] = "Blog Golf";
            return View(Posts.OrderByDescending(p => p.PublishedAt).ToList());
        }

        public IActionResult Details(int id)
        {
            var post = Posts.FirstOrDefault(p => p.Id == id);
            if (post == null) return NotFound();
            ViewData["Title"] = post.Title;
            return View(post);
        }
    }

    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Summary { get; set; } = "";
        public string Content { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public DateTime PublishedAt { get; set; }
        public string Author { get; set; } = "";
    }
}
