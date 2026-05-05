using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created (only if not exists)
            await context.Database.EnsureCreatedAsync();

            // Update categories with new data
            await UpdateCategoriesAsync(context);

            // Create roles
            string[] roles = { "Administrator", "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create admin user if it doesn't exist
            var adminEmail = "admin@forum.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = "admin",
                    Email = adminEmail,
                    DisplayName = "Administrator",
                    Bio = "Forum Administrator",
                    JoinDate = DateTime.UtcNow,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                }
            }

            // Seed sample data if no posts exist
            if (!context.Posts.Any())
            {
                await SeedSampleDataAsync(context, adminUser);
            }
        }

        private static async Task SeedSampleDataAsync(ForumDbContext context, User adminUser)
        {
            // Get all subcategories (sử dụng hệ thống 3-tier)
            var programmingSubCategory = await context.SubCategories.FirstOrDefaultAsync(sc => sc.Name == "Lập trình");
            var investmentSubCategory = await context.SubCategories.FirstOrDefaultAsync(sc => sc.Name == "Đầu tư");
            var lifeStyleSubCategory = await context.SubCategories.FirstOrDefaultAsync(sc => sc.Name == "Cuộc sống");
            var entertainmentSubCategory = await context.SubCategories.FirstOrDefaultAsync(sc => sc.Name == "Giải trí");
            var educationSubCategory = await context.SubCategories.FirstOrDefaultAsync(sc => sc.Name == "Giáo dục");
            var healthSubCategory = await context.SubCategories.FirstOrDefaultAsync(sc => sc.Name == "Sức khỏe");
            var travelSubCategory = await context.SubCategories.FirstOrDefaultAsync(sc => sc.Name == "Du lịch");
            var foodSubCategory = await context.SubCategories.FirstOrDefaultAsync(sc => sc.Name == "Ẩm thực");

            // Fallback to getting first subcategory from each main category if specific ones don't exist
            var subCategories = await context.SubCategories.ToListAsync();
            if (subCategories.Count == 0)
            {
                Console.WriteLine("❌ Không tìm thấy SubCategories nào. Vui lòng chắc chắn ThreeTierCategorySeed đã chạy.");
                return;
            }

            // Use available subcategories or default to first ones
            programmingSubCategory ??= subCategories.FirstOrDefault(sc => sc.MainCategoryId == 1) ?? subCategories[0];
            investmentSubCategory ??= subCategories.FirstOrDefault(sc => sc.MainCategoryId == 2) ?? subCategories[1 % subCategories.Count];
            lifeStyleSubCategory ??= subCategories.Skip(2 % subCategories.Count).FirstOrDefault() ?? subCategories[0];
            entertainmentSubCategory ??= subCategories.FirstOrDefault(sc => sc.MainCategoryId == 3) ?? subCategories[3 % subCategories.Count];
            educationSubCategory ??= subCategories.FirstOrDefault(sc => sc.MainCategoryId == 4) ?? subCategories[4 % subCategories.Count];
            healthSubCategory ??= subCategories.FirstOrDefault(sc => sc.MainCategoryId == 5) ?? subCategories[5 % subCategories.Count];
            travelSubCategory ??= subCategories.FirstOrDefault(sc => sc.MainCategoryId == 6) ?? subCategories[6 % subCategories.Count];
            foodSubCategory ??= subCategories.Skip(1).FirstOrDefault() ?? subCategories[0];

            // Create sample posts for each category
            var posts = new[]
            {
                // Công nghệ posts
                new Post
                {
                    Title = "Xu hướng công nghệ 2025",
                    Content = "Năm 2025 đang mang đến nhiều xu hướng công nghệ thú vị:\n\n1. **Trí tuệ nhân tạo**: AI ngày càng phát triển mạnh mẽ\n2. **Blockchain**: Ứng dụng rộng rãi trong nhiều lĩnh vực\n3. **IoT**: Internet of Things kết nối mọi thứ\n4. **5G/6G**: Tốc độ internet siêu nhanh\n\nBạn nghĩ xu hướng nào sẽ tác động mạnh nhất? Hãy chia sẻ ý kiến của bạn!",
                    UserId = adminUser.Id,
                    SubCategoryId = programmingSubCategory.Id,
                    CategoryId = programmingSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    Views = 42
                },
                new Post
                {
                    Title = "AI sẽ thay đổi ngành công nghệ như thế nào?",
                    Content = "Trí tuệ nhân tạo đang phát triển với tốc độ chóng mặt. Hãy thảo luận về những cách mà AI sẽ tác động đến công việc của chúng ta và xã hội trong tương lai.\n\n• Tự động hóa công việc\n• Cải thiện hiệu suất\n• Tạo ra công việc mới\n• Những thách thức về đạo đức",
                    UserId = adminUser.Id,
                    SubCategoryId = programmingSubCategory.Id,
                    CategoryId = programmingSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    Views = 28
                },
                new Post
                {
                    Title = "Hướng dẫn bắt đầu với Python",
                    Content = "Python là một trong những ngôn ngữ lập trình phổ biến nhất. Bài viết này sẽ hướng dẫn bạn từng bước bắt đầu học Python.\n\nTừ cài đặt môi trường, học cú pháp cơ bản, cho đến xây dựng các ứng dụng thực tế. Đây là bài viết dành cho những ai mới bắt đầu.",
                    UserId = adminUser.Id,
                    SubCategoryId = programmingSubCategory.Id,
                    CategoryId = programmingSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Views = 35
                },
                new Post
                {
                    Title = "CloudComputing - tương lai của IT",
                    Content = "Cloud Computing đang trở thành yêu cầu bắt buộc cho các doanh nghiệp hiện đại. AWS, Azure, Google Cloud - hãy cùng thảo luận về các nền tảng và ứng dụng của chúng.",
                    UserId = adminUser.Id,
                    SubCategoryId = programmingSubCategory.Id,
                    CategoryId = programmingSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Views = 19
                },

                // Tài chính posts
                new Post
                {
                    Title = "Cách quản lý tài chính cá nhân hiệu quả",
                    Content = "Quản lý tài chính cá nhân là kỹ năng quan trọng mà mọi người cần học. Hãy cùng tìm hiểu các chiến lược tiết kiệm, đầu tư và lập kế hoạch tài chính.\n\n1. Lập ngân sách hàng tháng\n2. Xây dựng quỹ khẩn cấp\n3. Đầu tư thông minh\n4. Kiểm soát nợ",
                    UserId = adminUser.Id,
                    SubCategoryId = investmentSubCategory.Id,
                    CategoryId = investmentSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-6),
                    Views = 52
                },
                new Post
                {
                    Title = "Những điều cần biết về đầu tư chứng khoán",
                    Content = "Chứng khoán là một trong những cách để tạo ra thụ động thu nhập. Nhưng trước khi bắt đầu, bạn cần hiểu rõ các nguyên tắc cơ bản.\n\n• Phân tích cơ bản\n• Phân tích kỹ thuật\n• Quản lý rủi ro\n• Tâm lý nhà đầu tư",
                    UserId = adminUser.Id,
                    SubCategoryId = investmentSubCategory.Id,
                    CategoryId = investmentSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    Views = 38
                },
                new Post
                {
                    Title = "Cryptocurrency - Tương lai của tiền tệ?",
                    Content = "Bitcoin, Ethereum và hàng nghìn loại tiền mã hóa khác đang thay đổi cách chúng ta nhìn nhận về tiền. Hãy cùng thảo luận về lợi ích, rủi ro và triển vọng của crypto.",
                    UserId = adminUser.Id,
                    SubCategoryId = investmentSubCategory.Id,
                    CategoryId = investmentSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Views = 67
                },
                new Post
                {
                    Title = "Kinh doanh nhỏ - Bắt đầu từ đâu?",
                    Content = "Có ý định khởi nghiệp? Bài viết này sẽ giúp bạn hiểu về các bước cần thiết để bắt đầu một doanh nghiệp nhỏ thành công.",
                    UserId = adminUser.Id,
                    SubCategoryId = investmentSubCategory.Id,
                    CategoryId = investmentSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Views = 23
                },

                // Đời sống posts
                new Post
                {
                    Title = "Chào mừng đến với diễn đàn!",
                    Content = "Chào mừng bạn đến với diễn đàn thảo luận của chúng tôi! Đây là nơi bạn có thể chia sẻ ý tưởng, đặt câu hỏi và kết nối với các thành viên khác trong cộng đồng.\n\nHãy tự do khám phá các danh mục khác nhau và bắt đầu tham gia thảo luận. Đừng quên đọc quy tắc cộng đồng để đảm bảo trải nghiệm tích cực cho mọi người.",
                    UserId = adminUser.Id,
                    SubCategoryId = lifeStyleSubCategory.Id,
                    CategoryId = lifeStyleSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    Views = 25
                },
                new Post
                {
                    Title = "Cân bằng giữa cuộc sống và công việc",
                    Content = "Work-life balance là một chủ đề được nhiều người quan tâm. Làm sao để cân bằng giữa sự nghiệp và gia đình? Hãy cùng chia sẻ kinh nghiệm của mình.",
                    UserId = adminUser.Id,
                    SubCategoryId = lifeStyleSubCategory.Id,
                    CategoryId = lifeStyleSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    Views = 41
                },
                new Post
                {
                    Title = "Xây dựng mối quan hệ gia đình tốt",
                    Content = "Gia đình là nơi chúng ta tìm thấy sự ấm áp và hỗ trợ. Hãy chia sẻ cách bạn xây dựng mối quan hệ mạnh mẽ với gia đình.",
                    UserId = adminUser.Id,
                    SubCategoryId = lifeStyleSubCategory.Id,
                    CategoryId = lifeStyleSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    Views = 29
                },
                new Post
                {
                    Title = "Tình nguyện và giúp đỡ cộng đồng",
                    Content = "Tình nguyện viên là những người muốn đóng góp cho cộng đồng. Bạn đã từng làm tình nguyện? Hãy chia sẻ những trải nghiệm của mình.",
                    UserId = adminUser.Id,
                    SubCategoryId = lifeStyleSubCategory.Id,
                    CategoryId = lifeStyleSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Views = 16
                },

                // Giải trí posts
                new Post
                {
                    Title = "Những bộ phim hay nhất của năm 2025",
                    Content = "Năm 2025 mang đến nhiều bộ phim tuyệt vời. Hãy chia sẻ những bộ phim yêu thích của bạn và tại sao bạn thích chúng.\n\n• Thể loại hành động\n• Thể loại tình cảm\n• Thể loại khoa học viễn tưởng\n• Thể loại hài hước",
                    UserId = adminUser.Id,
                    SubCategoryId = entertainmentSubCategory.Id,
                    CategoryId = entertainmentSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    Views = 58
                },
                new Post
                {
                    Title = "Game hay đáng chơi trên PC",
                    Content = "Bạn là một game thủ? Hãy chia sẻ những game hay trên PC mà bạn đang chơi. Từ AAA đến indie games, tất cả đều được chào đón.",
                    UserId = adminUser.Id,
                    SubCategoryId = entertainmentSubCategory.Id,
                    CategoryId = entertainmentSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    Views = 72
                },
                new Post
                {
                    Title = "Nhạc hay trong tháng này",
                    Content = "Có những bài nhạc nào hay mà bạn nghe trong tháng này không? Hãy giới thiệu những bài nhạc, nghệ sĩ mà bạn yêu thích.",
                    UserId = adminUser.Id,
                    SubCategoryId = entertainmentSubCategory.Id,
                    CategoryId = entertainmentSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Views = 34
                },
                new Post
                {
                    Title = "Thể thao - Đam mê của bạn",
                    Content = "Bóng đá, bóng rổ, cầu lông hay bất kỳ môn thể thao nào - hãy chia sẻ đam mê thể thao của bạn.",
                    UserId = adminUser.Id,
                    SubCategoryId = entertainmentSubCategory.Id,
                    CategoryId = entertainmentSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Views = 45
                },

                // Học tập posts
                new Post
                {
                    Title = "Kỹ năng học tập hiệu quả",
                    Content = "Để học tập hiệu quả, hãy áp dụng những phương pháp sau:\n\n• Lên kế hoạch học tập rõ ràng\n• Tạo môi trường học tập tích cực\n• Sử dụng kỹ thuật Pomodoro\n• Làm bài tập thường xuyên\n• Tham gia nhóm học tập\n\nCùng nhau xây dựng cộng đồng học tập tuyệt vời!",
                    UserId = adminUser.Id,
                    SubCategoryId = educationSubCategory.Id,
                    CategoryId = educationSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    Views = 18
                },
                new Post
                {
                    Title = "Khóa học online miễn phí chất lượng cao",
                    Content = "Bạn muốn học thêm kỹ năng mới? Có nhiều khóa học online miễn phí với chất lượng cao. Hãy chia sẻ những khóa học mà bạn đã hoặc đang học.",
                    UserId = adminUser.Id,
                    SubCategoryId = educationSubCategory.Id,
                    CategoryId = educationSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Views = 33
                },
                new Post
                {
                    Title = "Cách chuẩn bị thi đại học hiệu quả",
                    Content = "Thi đại học là một cột mốc quan trọng. Hãy chia sẻ những kinh nghiệm và lời khuyên để chuẩn bị tốt cho kỳ thi.",
                    UserId = adminUser.Id,
                    SubCategoryId = educationSubCategory.Id,
                    CategoryId = educationSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Views = 26
                },
                new Post
                {
                    Title = "Tự học lập trình - Hành trình của tôi",
                    Content = "Tự học lập trình không phải là điều khó. Hãy cùng chia sẻ những kinh nghiệm và tài liệu học lập trình tự động.",
                    UserId = adminUser.Id,
                    SubCategoryId = educationSubCategory.Id,
                    CategoryId = educationSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-0.5),
                    Views = 15
                },

                // Sức khỏe posts
                new Post
                {
                    Title = "Tập thể dục ở nhà - Không cần phòng gym",
                    Content = "Không có thời gian hoặc tiền để tập gym? Bạn có thể tập thể dục ở nhà một cách hiệu quả. Hãy cùng chia sẻ các bài tập và kinh nghiệm tập luyện.",
                    UserId = adminUser.Id,
                    SubCategoryId = healthSubCategory.Id,
                    CategoryId = healthSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-6),
                    Views = 48
                },
                new Post
                {
                    Title = "Chế độ ăn uống lành mạnh",
                    Content = "Ăn uống là nền tảng của sức khỏe. Hãy chia sẻ các công thức nấu ăn lành mạnh và lời khuyên về dinh dưỡng.",
                    UserId = adminUser.Id,
                    SubCategoryId = healthSubCategory.Id,
                    CategoryId = healthSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    Views = 37
                },
                new Post
                {
                    Title = "Yoga - Khoá học và kinh nghiệm",
                    Content = "Yoga không chỉ tốt cho cơ thể mà còn tốt cho tâm trí. Hãy chia sẻ kinh nghiệm tập yoga của bạn.",
                    UserId = adminUser.Id,
                    SubCategoryId = healthSubCategory.Id,
                    CategoryId = healthSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Views = 25
                },
                new Post
                {
                    Title = "Cách cải thiện giấc ngủ",
                    Content = "Giấc ngủ tốt là chìa khóa để có sức khỏe tốt. Hãy chia sẻ những cách cải thiện giấc ngủ của bạn.",
                    UserId = adminUser.Id,
                    SubCategoryId = healthSubCategory.Id,
                    CategoryId = healthSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Views = 31
                },

                // Du lịch posts
                new Post
                {
                    Title = "Du lịch Hà Nội - Địa điểm không nên bỏ qua",
                    Content = "Hà Nội là thủ đô ngàn năm văn hóa. Hãy chia sẻ những địa điểm du lịch yêu thích của bạn ở Hà Nội.\n\n• Phố cổ Hà Nội\n• Hồ Hoàn Kiếm\n• Văn Miếu Quốc Tử Giám\n• Mausoleum Hồ Chí Minh",
                    UserId = adminUser.Id,
                    SubCategoryId = travelSubCategory.Id,
                    CategoryId = travelSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    Views = 44
                },
                new Post
                {
                    Title = "Du lịch Đà Nẵng mùa hè",
                    Content = "Đà Nẵng với những bãi biển đẹp là điểm đến lý tưởng mùa hè. Hãy chia sẻ kinh nghiệm và lời khuyên du lịch Đà Nẵng.",
                    UserId = adminUser.Id,
                    SubCategoryId = travelSubCategory.Id,
                    CategoryId = travelSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    Views = 39
                },
                new Post
                {
                    Title = "Ẩm thực nước ngoài - Khám phá thế giới qua ăn uống",
                    Content = "Du lịch không chỉ là khám phá địa điểm mà còn là tìm hiểu ẩm thực địa phương. Hãy chia sẻ những quán ăn ngon mà bạn đã thử.",
                    UserId = adminUser.Id,
                    SubCategoryId = foodSubCategory.Id,
                    CategoryId = foodSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Views = 28
                },
                new Post
                {
                    Title = "Du lịch bằng cách sống như người bản địa",
                    Content = "Thay vì cách du lịch truyền thống, hãy cùng thảo luận về cách sống như người bản địa khi du lịch.",
                    UserId = adminUser.Id,
                    SubCategoryId = travelSubCategory.Id,
                    CategoryId = travelSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-0.5),
                    Views = 12
                },

                // Sáng tạo posts
                new Post
                {
                    Title = "Nhiếp ảnh - Bắt đầu với điện thoại",
                    Content = "Bạn không cần máy ảnh chuyên nghiệp để bắt đầu nhiếp ảnh. Hãy chia sẻ những mẹo và kinh nghiệm chụp ảnh bằng điện thoại.",
                    UserId = adminUser.Id,
                    SubCategoryId = entertainmentSubCategory.Id,
                    CategoryId = entertainmentSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-6),
                    Views = 36
                },
                new Post
                {
                    Title = "Viết lách - Cách bắt đầu viết",
                    Content = "Bạn có ước mơ viết một cuốn sách? Hãy chia sẻ kinh nghiệm viết và các mẹo giúp bạn vượt qua khối chứng viết.",
                    UserId = adminUser.Id,
                    SubCategoryId = entertainmentSubCategory.Id,
                    CategoryId = entertainmentSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    Views = 22
                },
                new Post
                {
                    Title = "Thiết kế đồ họa - Công cụ và kỹ năng cần thiết",
                    Content = "Thiết kế đồ họa là một nghệ thuật. Hãy chia sẻ công cụ bạn sử dụng và các kỹ năng cần thiết để học thiết kế.",
                    UserId = adminUser.Id,
                    SubCategoryId = entertainmentSubCategory.Id,
                    CategoryId = entertainmentSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Views = 29
                },
                new Post
                {
                    Title = "DIY - Những dự án tạo đồ vật hữu ích",
                    Content = "DIY (Do It Yourself) là cách tuyệt vời để sáng tạo. Hãy chia sẻ những dự án DIY mà bạn đã hoàn thành.",
                    UserId = adminUser.Id,
                    SubCategoryId = entertainmentSubCategory.Id,
                    CategoryId = entertainmentSubCategory.MainCategoryId,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Views = 40
                }
            };

            context.Posts.AddRange(posts);
            await context.SaveChangesAsync();

            // Create sample tags
            var tags = new[]
            {
                // Công nghệ tags
                new Tag { Name = "công-nghệ" },
                new Tag { Name = "ai" },
                new Tag { Name = "blockchain" },
                new Tag { Name = "python" },
                new Tag { Name = "cloud-computing" },
                new Tag { Name = "lập-trình" },
                // Tài chính tags
                new Tag { Name = "tài-chính" },
                new Tag { Name = "đầu-tư" },
                new Tag { Name = "chứng-khoán" },
                new Tag { Name = "crypto" },
                new Tag { Name = "kinh-doanh" },
                new Tag { Name = "tiết-kiệm" },
                // Đời sống tags
                new Tag { Name = "đời-sống" },
                new Tag { Name = "gia-đình" },
                new Tag { Name = "work-life-balance" },
                new Tag { Name = "cộng-đồng" },
                // Giải trí tags
                new Tag { Name = "giải-trí" },
                new Tag { Name = "phim-ảnh" },
                new Tag { Name = "game" },
                new Tag { Name = "nhạc" },
                new Tag { Name = "thể-thao" },
                // Học tập tags
                new Tag { Name = "học-tập" },
                new Tag { Name = "kỹ-năng" },
                new Tag { Name = "khóa-học" },
                new Tag { Name = "phương-pháp" },
                // Sức khỏe tags
                new Tag { Name = "sức-khỏe" },
                new Tag { Name = "tập-thể-dục" },
                new Tag { Name = "dinh-dưỡng" },
                new Tag { Name = "yoga" },
                new Tag { Name = "giấc-ngủ" },
                // Du lịch tags
                new Tag { Name = "du-lịch" },
                new Tag { Name = "hà-nội" },
                new Tag { Name = "đà-nẵng" },
                new Tag { Name = "ẩm-thực" },
                // Sáng tạo tags
                new Tag { Name = "sáng-tạo" },
                new Tag { Name = "nhiếp-ảnh" },
                new Tag { Name = "viết-lách" },
                new Tag { Name = "thiết-kế" },
                new Tag { Name = "diy" }
            };

            context.Tags.AddRange(tags);
            await context.SaveChangesAsync();

            // Associate tags with posts
            var postTags = new List<PostTag>();
            
            // Công nghệ posts (0-3)
            postTags.AddRange(new[] {
                new PostTag { PostId = posts[0].Id, TagId = tags[0].Id }, // công-nghệ
                new PostTag { PostId = posts[1].Id, TagId = tags[0].Id }, // công-nghệ
                new PostTag { PostId = posts[1].Id, TagId = tags[1].Id }, // ai
                new PostTag { PostId = posts[2].Id, TagId = tags[4].Id }, // python
                new PostTag { PostId = posts[3].Id, TagId = tags[5].Id }  // cloud-computing
            });
            
            // Tài chính posts (4-7)
            postTags.AddRange(new[] {
                new PostTag { PostId = posts[4].Id, TagId = tags[6].Id }, // tài-chính
                new PostTag { PostId = posts[5].Id, TagId = tags[7].Id }, // đầu-tư
                new PostTag { PostId = posts[5].Id, TagId = tags[8].Id }, // chứng-khoán
                new PostTag { PostId = posts[6].Id, TagId = tags[9].Id }, // crypto
                new PostTag { PostId = posts[7].Id, TagId = tags[10].Id } // kinh-doanh
            });
            
            // Đời sống posts (8-11)
            postTags.AddRange(new[] {
                new PostTag { PostId = posts[8].Id, TagId = tags[12].Id }, // đời-sống
                new PostTag { PostId = posts[9].Id, TagId = tags[14].Id }, // work-life-balance
                new PostTag { PostId = posts[10].Id, TagId = tags[13].Id }, // gia-đình
                new PostTag { PostId = posts[11].Id, TagId = tags[15].Id } // cộng-đồng
            });
            
            // Giải trí posts (12-15)
            postTags.AddRange(new[] {
                new PostTag { PostId = posts[12].Id, TagId = tags[17].Id }, // phim-ảnh
                new PostTag { PostId = posts[13].Id, TagId = tags[18].Id }, // game
                new PostTag { PostId = posts[14].Id, TagId = tags[19].Id }, // nhạc
                new PostTag { PostId = posts[15].Id, TagId = tags[20].Id } // thể-thao
            });
            
            // Học tập posts (16-19)
            postTags.AddRange(new[] {
                new PostTag { PostId = posts[16].Id, TagId = tags[22].Id }, // học-tập
                new PostTag { PostId = posts[17].Id, TagId = tags[24].Id }, // khóa-học
                new PostTag { PostId = posts[18].Id, TagId = tags[23].Id }, // kỹ-năng
                new PostTag { PostId = posts[19].Id, TagId = tags[5].Id }  // lập-trình
            });
            
            // Sức khỏe posts (20-23)
            postTags.AddRange(new[] {
                new PostTag { PostId = posts[20].Id, TagId = tags[26].Id }, // tập-thể-dục
                new PostTag { PostId = posts[21].Id, TagId = tags[27].Id }, // dinh-dưỡng
                new PostTag { PostId = posts[22].Id, TagId = tags[28].Id }, // yoga
                new PostTag { PostId = posts[23].Id, TagId = tags[29].Id } // giấc-ngủ
            });
            
            // Du lịch posts (24-27)
            postTags.AddRange(new[] {
                new PostTag { PostId = posts[24].Id, TagId = tags[31].Id }, // hà-nội
                new PostTag { PostId = posts[25].Id, TagId = tags[32].Id }, // đà-nẵng
                new PostTag { PostId = posts[26].Id, TagId = tags[33].Id }, // ẩm-thực
                new PostTag { PostId = posts[27].Id, TagId = tags[30].Id } // du-lịch
            });
            
            // Sáng tạo posts (28-31)
            postTags.AddRange(new[] {
                new PostTag { PostId = posts[28].Id, TagId = tags[35].Id }, // nhiếp-ảnh
                new PostTag { PostId = posts[29].Id, TagId = tags[36].Id }, // viết-lách
                new PostTag { PostId = posts[30].Id, TagId = tags[37].Id }, // thiết-kế
                new PostTag { PostId = posts[31].Id, TagId = tags[38].Id } // diy
            });

            context.PostTags.AddRange(postTags);
            await context.SaveChangesAsync();

            // Create sample comments
            var comments = new[]
            {
                new Comment
                {
                    Content = "Bài viết rất hay! Công nghệ thực sự đang thay đổi nhanh chóng.",
                    PostId = posts[0].Id,
                    UserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new Comment
                {
                    Content = "AI sẽ thay đổi hoàn toàn cách chúng ta làm việc. Bài viết rất hữu ích!",
                    PostId = posts[1].Id,
                    UserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Comment
                {
                    Content = "Python là ngôn ngữ tuyệt vời để bắt đầu lập trình!",
                    PostId = posts[2].Id,
                    UserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Comment
                {
                    Content = "Cloud computing đang trở thành tương lai của IT.",
                    PostId = posts[3].Id,
                    UserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-0.5)
                },
                new Comment
                {
                    Content = "Quản lý tài chính cá nhân là rất quan trọng. Bài viết này giúp tôi rất nhiều!",
                    PostId = posts[4].Id,
                    UserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            };

            context.Comments.AddRange(comments);
            await context.SaveChangesAsync();
        }

        private static async Task UpdateCategoriesAsync(ForumDbContext context)
        {
            // Danh sách categories mới
            var newCategories = new List<Category>
            {
                new Category { Id = 1, Name = "Công nghệ", Description = "Thảo luận về công nghệ, thiết bị điện tử, AI, blockchain", Color = "#10B981" },
                new Category { Id = 2, Name = "Tài chính", Description = "Đầu tư, kinh doanh, tiền tệ, crypto, chứng khoán", Color = "#059669" },
                new Category { Id = 3, Name = "Đời sống", Description = "Cuộc sống hàng ngày, gia đình, xã hội, văn hóa", Color = "#3B82F6" },
                new Category { Id = 4, Name = "Giải trí", Description = "Phim ảnh, âm nhạc, game, thể thao, sở thích", Color = "#8B5CF6" },
                new Category { Id = 5, Name = "Học tập", Description = "Giáo dục, kỹ năng, khóa học, tài liệu học tập", Color = "#F59E0B" },
                new Category { Id = 6, Name = "Sức khỏe", Description = "Y tế, dinh dưỡng, thể dục, chăm sóc sức khỏe", Color = "#EF4444" },
                new Category { Id = 7, Name = "Du lịch", Description = "Địa điểm du lịch, kinh nghiệm, lịch trình, ẩm thực", Color = "#06B6D4" },
                new Category { Id = 8, Name = "Sáng tạo", Description = "Nghệ thuật, thiết kế, viết lách, nhiếp ảnh, DIY", Color = "#EC4899" }
            };

            foreach (var newCategory in newCategories)
            {
                var existingCategory = await context.Categories.FindAsync(newCategory.Id);
                if (existingCategory != null)
                {
                    // Cập nhật category hiện có
                    existingCategory.Name = newCategory.Name;
                    existingCategory.Description = newCategory.Description;
                    existingCategory.Color = newCategory.Color;
                    context.Categories.Update(existingCategory);
                }
                else
                {
                    // Thêm category mới
                    context.Categories.Add(newCategory);
                }
            }

            // Xóa categories cũ không cần thiết (nếu có ID > 8)
            var oldCategories = await context.Categories.Where(c => c.Id > 8).ToListAsync();
            if (oldCategories.Any())
            {
                context.Categories.RemoveRange(oldCategories);
            }

            await context.SaveChangesAsync();
        }

        public static async Task SeedCategoryAdminsAsync(ForumDbContext context, UserManager<User> userManager, User adminUser)
        {
            Console.WriteLine("🔄 Bắt đầu seeding CategoryAdmins cho hệ thống phân cấp mới...");

            // Lấy danh sách tất cả SubCategories (không giới hạn)
            var subCategories = await context.SubCategories.OrderBy(sc => sc.Id).ToListAsync();

            Console.WriteLine($"📚 Tìm thấy {subCategories.Count} SubCategories");

            if (!subCategories.Any())
            {
                Console.WriteLine("❌ Không tìm thấy SubCategories nào");
                return;
            }

            // Kiểm tra xem đã có CategoryAdmins riêng cho mỗi danh mục chưa
            var existingCategoryAdmins = await context.CategoryAdmins
                .Include(ca => ca.User)
                .ToListAsync();
            
            if (existingCategoryAdmins.Count > 0)
            {
                var hasDedicatedAdmins = existingCategoryAdmins.Any(ca => ca.User != null && ca.User.Email != "admin@forum.com");

                if (hasDedicatedAdmins && existingCategoryAdmins.Count == subCategories.Count)
                {
                    Console.WriteLine("✅ CategoryAdmins cho tất cả danh mục đã tồn tại, bỏ qua seeding");
                    return;
                }
            }

            // Nếu có CategoryAdmins cũ, xóa chúng
            if (existingCategoryAdmins.Count > 0)
            {
                Console.WriteLine($"🗑️  Xóa {existingCategoryAdmins.Count} CategoryAdmin cũ...");
                context.CategoryAdmins.RemoveRange(existingCategoryAdmins);
                await context.SaveChangesAsync();
            }

            // Tạo admin users riêng cho từng danh mục
            var categoryAdmins = new List<CategoryAdmin>();

            foreach (var subCategory in subCategories)
            {
                // Tạo email cho admin của danh mục này
                // Chuyển đổi tên danh mục thành slug (loại bỏ ký tự đặc biệt, dấu, v.v.)
                var adminEmail = ConvertToSlug(subCategory.Name) + "-admin@forum.com";
                var adminPassword = "Admin@123456"; // Mật khẩu mặc định cho admin danh mục

                Console.WriteLine($"👤 Xử lý danh mục: {subCategory.Name}");
                Console.WriteLine($"   📧 Email: {adminEmail}");

                // Kiểm tra xem user này đã tồn tại chưa
                var categoryAdminUser = await userManager.FindByEmailAsync(adminEmail);

                if (categoryAdminUser == null)
                {
                    // Tạo user admin mới cho danh mục
                    categoryAdminUser = new User
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        DisplayName = "Admin - " + subCategory.Name,
                        EmailConfirmed = true,
                        IsActive = true,
                        JoinDate = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(categoryAdminUser, adminPassword);

                    if (result.Succeeded)
                    {
                        Console.WriteLine($"   ✅ Tạo user mới");
                        // Gán role Admin cho user này
                        await userManager.AddToRoleAsync(categoryAdminUser, "Admin");
                        Console.WriteLine($"   ✅ Gán role Admin");
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Lỗi tạo user");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"      - {error.Description}");
                        }
                        continue;
                    }
                }
                else
                {
                    Console.WriteLine($"   ℹ️  User đã tồn tại");
                }

                // Tạo CategoryAdmin assignment
                var categoryAdmin = new CategoryAdmin
                {
                    UserId = categoryAdminUser.Id,
                    SubCategoryId = subCategory.Id,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                };

                categoryAdmins.Add(categoryAdmin);
                Console.WriteLine($"   ✅ Gán admin cho danh mục này\n");
            }

            if (categoryAdmins.Any())
            {
                context.CategoryAdmins.AddRange(categoryAdmins);
                await context.SaveChangesAsync();
                Console.WriteLine($"\n✅ Lưu {categoryAdmins.Count} CategoryAdmin vào database");
                Console.WriteLine("✅ Mỗi danh mục đã có 1 admin riêng!\n");
            }
        }

        // Hàm chuyển đổi tên danh mục thành slug (ví dụ: "AI & Machine Learning" -> "ai-machine-learning")
        private static string ConvertToSlug(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Chuyển thành chữ thường
            text = text.ToLower();

            // Loại bỏ dấu tiếng Việt
            string[] vietnamese = new string[]
            {
                "ả", "ã", "ạ", "ă", "ằ", "ắ", "ặ", "ẳ", "ẵ", "á", "à", "ạ", "ả", "ã",
                "đ",
                "è", "é", "ẹ", "ẻ", "ẽ", "ê", "ề", "ế", "ệ", "ể", "ễ",
                "ì", "í", "ị", "ỉ", "ĩ",
                "ò", "ó", "ọ", "ỏ", "õ", "ô", "ồ", "ố", "ộ", "ổ", "ỗ", "ơ", "ờ", "ớ", "ợ", "ở", "ỡ",
                "ù", "ú", "ụ", "ủ", "ũ", "ư", "ừ", "ứ", "ự", "ử", "ữ",
                "ỳ", "ý", "ỵ", "ỷ", "ỹ"
            };
            string[] latin = new string[]
            {
                "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
                "d",
                "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e",
                "i", "i", "i", "i", "i",
                "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o",
                "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "u",
                "y", "y", "y", "y", "y"
            };

            for (int i = 0; i < vietnamese.Length; i++)
            {
                text = text.Replace(vietnamese[i], latin[i]);
            }

            // Loại bỏ các ký tự không phải chữ, số, dấu gạch ngang hoặc dấu gạch chân
            var chars = text.ToCharArray();
            var result = new StringBuilder();

            foreach (var c in chars)
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_')
                {
                    result.Append(c);
                }
                else if (c == ' ')
                {
                    result.Append('-');
                }
            }

            // Loại bỏ các dấu gạch ngang liên tiếp
            var slug = result.ToString();
            while (slug.Contains("--"))
            {
                slug = slug.Replace("--", "-");
            }

            // Loại bỏ dấu gạch ngang ở đầu và cuối
            slug = slug.Trim('-');

            return slug;
        }
    }
}

