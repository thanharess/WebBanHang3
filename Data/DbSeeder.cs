using Microsoft.EntityFrameworkCore;
using WebBanHang.Helpers;
using WebBanHang.Models;

namespace WebBanHang.Data
{
    /// <summary>
    /// Nạp dữ liệu mẫu để website có sản phẩm hiển thị ngay sau khi tạo database.
    /// Mỗi phần đều kiểm tra dữ liệu đã tồn tại chưa nên gọi lại nhiều lần vẫn an toàn.
    /// </summary>
    public static class DbSeeder
    {
        /// <summary>
        /// Mật khẩu mặc định của tài khoản quản trị tạo sẵn.
        /// CHỈ DÙNG CHO MÔI TRƯỜNG PHÁT TRIỂN - hãy đổi ngay khi triển khai thật.
        /// </summary>
        public const string DefaultAdminEmail = "admin@golfking.vn";
        public const string DefaultAdminPassword = "Admin@123";

        public const string DefaultStaffEmail = "staff@chtgolf.vn";
        public const string DefaultStaffPassword = "Staff@123";

        public static void Seed(AppDbContext context)
        {
            EnsureChatTable(context);
            SeedCatalog(context);
            SeedAdminUser(context);
            SeedStaffUser(context);
        }

        private static void SeedCatalog(AppDbContext context)
        {
            // Không dừng khi database đã có dữ liệu: chỉ thêm các mẫu chưa tồn tại.
            // Nhờ vậy cập nhật phiên bản ứng dụng vẫn nạp được catalogue mới mà không mất dữ liệu cũ.
            var categories = new[]
            {
                ("Gậy Golf", "Driver, fairway, hybrid, iron, wedge và putter.", "/images/spec-club.svg"),
                ("Bóng Golf", "Bóng golf chính hãng cho luyện tập và thi đấu.", "/images/spec-ball.svg"),
                ("Túi Golf", "Túi gậy, túi caddy và túi phụ kiện.", "/images/spec-bag.svg"),
                ("Găng Tay Golf", "Găng tay êm tay, bám chắc trong mọi cú đánh.", "/images/spec-accessory.svg"),
                ("Giày Golf", "Giày golf nhẹ, chống trượt và thoáng khí.", "/images/spec-apparel.svg"),
                ("Quần Áo Golf", "Trang phục golf lịch sự, co giãn và thoải mái.", "/images/spec-apparel.svg"),
                ("Phụ Kiện Golf", "Tee, marker, khăn, ô và phụ kiện sân golf.", "/images/spec-accessory.svg"),
                ("Thiết Bị Tập Golf", "Thảm tập, lưới tập và thiết bị hỗ trợ kỹ thuật.", "/images/spec-club.svg")
            };

            foreach (var (name, description, imageUrl) in categories)
            {
                var existing = context.Categories.FirstOrDefault(c => c.Name == name);
                if (existing == null)
                {
                    context.Categories.Add(new Category
                    {
                        Name = name,
                        Description = description,
                        ImageUrl = imageUrl,
                        IsActive = true
                    });
                }
                else if (string.IsNullOrWhiteSpace(existing.ImageUrl))
                {
                    existing.ImageUrl = imageUrl;
                }
            }

            context.SaveChanges();

            var categoryIds = context.Categories.ToDictionary(c => c.Name, c => c.Id);
            var products = new[]
            {
                ("Driver TaylorMade Qi10", "Gậy Golf", 14500000m, 13200000m, 8, "Driver trợ lực, phù hợp golfer cần khoảng cách và độ ổn định."),
                ("Bộ Iron Callaway Paradym", "Gậy Golf", 21800000m, 19900000m, 5, "Bộ gậy sắt cân bằng giữa cảm giác bóng và khả năng kiểm soát."),
                ("Putter Odyssey White Hot", "Gậy Golf", 5900000m, (decimal?)null, 12, "Putter mặt gậy mềm, hỗ trợ căn chỉnh chính xác."),
                ("Bóng Titleist Pro V1", "Bóng Golf", 1650000m, 1490000m, 20, "Hộp 12 bóng cao cấp với độ xoáy và cảm giác vượt trội."),
                ("Bóng Callaway Chrome Soft", "Bóng Golf", 1350000m, (decimal?)null, 18, "Bóng mềm, khoảng cách tốt, phù hợp cho nhiều trình độ."),
                ("Túi Gậy TaylorMade Cart Bag", "Túi Golf", 4200000m, 3750000m, 6, "Túi gậy nhiều ngăn, chống thấm nhẹ và tiện mang theo."),
                ("Túi Đựng Giày Golf", "Túi Golf", 650000m, (decimal?)null, 15, "Túi nhỏ gọn, giữ giày sạch sẽ sau mỗi vòng golf."),
                ("Găng Tay FootJoy WeatherSof", "Găng Tay Golf", 450000m, 390000m, 30, "Găng tay bền, thoáng khí, cho độ bám ổn định."),
                ("Găng Tay Titleist Players", "Găng Tay Golf", 690000m, (decimal?)null, 22, "Da cao cấp, ôm tay và tạo cảm giác đánh bóng chân thực."),
                ("Giày Adidas ZG23", "Giày Golf", 3200000m, 2850000m, 10, "Giày golf siêu nhẹ, đế bám tốt trên sân cỏ."),
                ("Áo Polo Golf Nam", "Quần Áo Golf", 890000m, 750000m, 25, "Áo polo co giãn, thấm hút mồ hôi, phong cách thể thao."),
                ("Quần Golf Nữ Co Giãn", "Quần Áo Golf", 990000m, (decimal?)null, 16, "Quần dáng gọn, nhẹ và thoải mái khi vận động."),
                ("Ô Golf Chống UV", "Phụ Kiện Golf", 550000m, 490000m, 14, "Ô cỡ lớn, chống nắng và mưa hiệu quả trên sân."),
                ("Bộ Tee Golf Gỗ", "Phụ Kiện Golf", 120000m, (decimal?)null, 50, "Bộ tee gỗ tiện dụng cho các buổi tập và thi đấu."),
                ("Thảm Tập Putting 3m", "Thiết Bị Tập Golf", 1850000m, 1590000m, 9, "Thảm tập putting tại nhà với vạch căn chỉnh trực quan.")
            };

            foreach (var (name, categoryName, price, discountPrice, stock, description) in products)
            {
                if (!context.Products.Any(p => p.Name == name))
                {
                    context.Products.Add(new Product
                {
                        Name = name,
                        CategoryId = categoryIds[categoryName],
                        Price = price,
                        DiscountPrice = discountPrice,
                        StockQuantity = stock,
                        Description = description,
                        ImageUrl = "/images/product-placeholder.svg"
                    });
                }
                }
            

            context.SaveChanges();
        }

        private static void SeedAdminUser(AppDbContext context)
        {
            if (context.Users.Any(u => u.Email == DefaultAdminEmail))
            {
                return;
            }

            context.Users.Add(new User
            {
                FullName = "Quản trị viên",
                Email = DefaultAdminEmail,
                PasswordHash = PasswordHasher.Hash(DefaultAdminPassword),
                PhoneNumber = "0900000000",
                Address = "Hà Nội",
                Role = UserRole.Admin,
                IsActive = true
            });

            context.SaveChanges();
        }
        
        private static void SeedStaffUser(AppDbContext context)
        {
            if (context.Users.Any(u => u.Email == DefaultStaffEmail))
            {
                return;
            }

            context.Users.Add(new User
            {
                FullName = "Nhân viên tư vấn",
                Email = DefaultStaffEmail,
                PasswordHash = PasswordHasher.Hash(DefaultStaffPassword),
                PhoneNumber = "0900000001",
                Address = "",
                Role = UserRole.Staff,
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            context.SaveChanges();
        }

        private static void EnsureChatTable(AppDbContext context)
        {
            // Phòng khi migration chưa áp dụng đủ snapshot
            context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatMessages](
        [Id] int NOT NULL IDENTITY(1,1),
        [SessionKey] nvarchar(64) NOT NULL,
        [GuestName] nvarchar(100) NOT NULL,
        [Content] nvarchar(1000) NOT NULL,
        [IsFromStaff] bit NOT NULL,
        [IsReadByStaff] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_ChatMessages_SessionKey] ON [dbo].[ChatMessages]([SessionKey]);
END");
        }

    }
}
