using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    /// <summary>
    /// Seed data cho hệ thống phân cấp 3 tầng MainCategory -> SubCategory -> Post
    /// </summary>
    public static class ThreeTierCategorySeed
    {
        public static void SeedData(ForumDbContext context)
        {
            // Kiểm tra xem đã có dữ liệu chưa
            if (context.MainCategories.Any())
            {
                return; // Đã có dữ liệu rồi, không seed nữa
            }

            // ============================================
            // 1. SEED MAIN CATEGORIES (Cụm danh mục)
            // ============================================
            var mainCategories = new List<MainCategory>
            {
                new MainCategory { Id = 1, Name = "Công nghệ", Order = 1, IsActive = true },
                new MainCategory { Id = 2, Name = "Kinh doanh", Order = 2, IsActive = true },
                new MainCategory { Id = 3, Name = "Giải trí", Order = 3, IsActive = true },
                new MainCategory { Id = 4, Name = "Giáo dục", Order = 4, IsActive = true },
                new MainCategory { Id = 5, Name = "Sức khỏe", Order = 5, IsActive = true },
                new MainCategory { Id = 6, Name = "Du lịch", Order = 6, IsActive = true },
            };

            context.MainCategories.AddRange(mainCategories);
            context.SaveChanges();

            // ============================================
            // 2. SEED SUB CATEGORIES (Chuyên mục)
            // ============================================
            var subCategories = new List<SubCategory>
            {
                // Công nghệ (MainCategoryId = 1)
                new SubCategory
                {
                    Id = 1,
                    Name = "Lập trình",
                    Description = "Thảo luận về ngôn ngữ lập trình, framework, best practices",
                    Icon = "💻",
                    MainCategoryId = 1,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 2,
                    Name = "AI & Machine Learning",
                    Description = "Trí tuệ nhân tạo, học máy, deep learning, neural networks",
                    Icon = "🤖",
                    MainCategoryId = 1,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 3,
                    Name = "Blockchain & Crypto",
                    Description = "Công nghệ blockchain, cryptocurrency, Web3, NFT",
                    Icon = "⛓️",
                    MainCategoryId = 1,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 4,
                    Name = "Thiết bị điện tử",
                    Description = "Smartphone, laptop, PC, gaming gear, đánh giá thiết bị",
                    Icon = "📱",
                    MainCategoryId = 1,
                    IsActive = true
                },

                // Kinh doanh (MainCategoryId = 2)
                new SubCategory
                {
                    Id = 5,
                    Name = "Khởi nghiệp",
                    Description = "Startup, ý tưởng kinh doanh, gọi vốn, scaling",
                    Icon = "🚀",
                    MainCategoryId = 2,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 6,
                    Name = "Marketing & Branding",
                    Description = "Digital marketing, SEO, content marketing, xây dựng thương hiệu",
                    Icon = "📈",
                    MainCategoryId = 2,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 7,
                    Name = "Đầu tư & Tài chính",
                    Description = "Chứng khoán, bất động sản, quỹ đầu tư, tài chính cá nhân",
                    Icon = "💰",
                    MainCategoryId = 2,
                    IsActive = true
                },

                // Giải trí (MainCategoryId = 3)
                new SubCategory
                {
                    Id = 8,
                    Name = "Phim ảnh",
                    Description = "Review phim, trailer, tin tức điện ảnh, Netflix, Disney+",
                    Icon = "🎬",
                    MainCategoryId = 3,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 9,
                    Name = "Âm nhạc",
                    Description = "Album mới, concert, nhạc cụ, học nhạc, Spotify playlists",
                    Icon = "🎵",
                    MainCategoryId = 3,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 10,
                    Name = "Gaming",
                    Description = "Game mới, review game, esports, streaming, console vs PC",
                    Icon = "🎮",
                    MainCategoryId = 3,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 11,
                    Name = "Thể thao",
                    Description = "Bóng đá, bóng rổ, tennis, tin tức thể thao, highlight",
                    Icon = "⚽",
                    MainCategoryId = 3,
                    IsActive = true
                },

                // Giáo dục (MainCategoryId = 4)
                new SubCategory
                {
                    Id = 12,
                    Name = "Ngoại ngữ",
                    Description = "Học tiếng Anh, tiếng Trung, IELTS, TOEIC, tips học ngoại ngữ",
                    Icon = "🗣️",
                    MainCategoryId = 4,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 13,
                    Name = "Kỹ năng mềm",
                    Description = "Giao tiếp, lãnh đạo, quản lý thời gian, tư duy phản biện",
                    Icon = "💡",
                    MainCategoryId = 4,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 14,
                    Name = "Khóa học Online",
                    Description = "Coursera, Udemy, Edx, review khóa học, học free",
                    Icon = "📚",
                    MainCategoryId = 4,
                    IsActive = true
                },

                // Sức khỏe (MainCategoryId = 5)
                new SubCategory
                {
                    Id = 15,
                    Name = "Thể dục & Fitness",
                    Description = "Gym, yoga, cardio, kế hoạch tập luyện, tăng cơ giảm mỡ",
                    Icon = "💪",
                    MainCategoryId = 5,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 16,
                    Name = "Dinh dưỡng",
                    Description = "Chế độ ăn uống, vitamin, thực phẩm chức năng, ăn sạch",
                    Icon = "🥗",
                    MainCategoryId = 5,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 17,
                    Name = "Sức khỏe tinh thần",
                    Description = "Mental health, meditation, stress management, mindfulness",
                    Icon = "🧘",
                    MainCategoryId = 5,
                    IsActive = true
                },

                // Du lịch (MainCategoryId = 6)
                new SubCategory
                {
                    Id = 18,
                    Name = "Du lịch trong nước",
                    Description = "Địa điểm du lịch Việt Nam, kinh nghiệm, review khách sạn",
                    Icon = "🇻🇳",
                    MainCategoryId = 6,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 19,
                    Name = "Du lịch quốc tế",
                    Description = "Du lịch châu Á, châu Âu, Mỹ, visa, lịch trình, budget travel",
                    Icon = "✈️",
                    MainCategoryId = 6,
                    IsActive = true
                },
                new SubCategory
                {
                    Id = 20,
                    Name = "Ẩm thực",
                    Description = "Nhà hàng ngon, món ăn đặc sản, food review, công thức nấu ăn",
                    Icon = "🍜",
                    MainCategoryId = 6,
                    IsActive = true
                },
            };

            context.SubCategories.AddRange(subCategories);
            context.SaveChanges();

            Console.WriteLine("✅ Đã seed thành công hệ thống phân cấp 3 tầng!");
            Console.WriteLine($"   - {mainCategories.Count} MainCategories");
            Console.WriteLine($"   - {subCategories.Count} SubCategories");
        }
    }
}
