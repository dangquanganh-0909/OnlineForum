using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Data;
using WebApplication1.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ForumDbContext _context;

        public HomeController(ILogger<HomeController> logger, ForumDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Trang chủ - Hiển thị cây phân cấp MainCategory -> SubCategory với thống kê và chức năng tìm kiếm
        /// </summary>
        public async Task<IActionResult> Index(int? subCategoryId, int? mainCategoryId, string? search, string? sortBy = "recent")
        {
            var viewModel = new HomeViewModel();


            try
            {
                // Nếu có subCategoryId hoặc có search, lấy danh sách bài viết phù hợp
                if (subCategoryId.HasValue || !string.IsNullOrEmpty(search))
                {
                    IQueryable<Post> postsQuery = _context.Posts
                        .Include(p => p.User)
                        .Include(p => p.SubCategory)
                            .ThenInclude(sc => sc.MainCategory)
                        .Include(p => p.PostLikes)
                        .Include(p => p.Comments)
                        .Where(p => !p.IsDeleted);

                    if (subCategoryId.HasValue)
                    {
                        postsQuery = postsQuery.Where(p => p.SubCategoryId == subCategoryId.Value);
                        var selectedSubCategory = await _context.SubCategories
                            .Include(sc => sc.MainCategory)
                            .FirstOrDefaultAsync(sc => sc.Id == subCategoryId.Value && sc.IsActive);
                        if (selectedSubCategory != null)
                        {
                            ViewData["SelectedSubCategory"] = selectedSubCategory;
                        }
                    }

                    if (!string.IsNullOrEmpty(search))
                    {
                        postsQuery = postsQuery.Where(p => p.Title.Contains(search) || p.Content.Contains(search));
                    }

                    // Apply sorting
                    switch (sortBy?.ToLower())
                    {
                        case "views":
                            postsQuery = postsQuery.OrderByDescending(p => p.Views);
                            break;
                        case "interaction":
                            postsQuery = postsQuery.OrderByDescending(p => p.PostLikes.Count + p.Comments.Count);
                            break;
                        case "length":
                            postsQuery = postsQuery.OrderByDescending(p => p.Content.Length);
                            break;
                        case "recent":
                        default:
                            postsQuery = postsQuery.OrderByDescending(p => p.CreatedAt);
                            break;
                    }

                    var posts = await postsQuery.AsNoTracking().ToListAsync();
                    ViewData["Posts"] = posts;
                }

                // Sử dụng Eager Loading để lấy toàn bộ cây phân cấp
                var mainCategories = await _context.MainCategories
                    .Where(mc => mc.IsActive)
                    .Include(mc => mc.SubCategories.Where(sc => sc.IsActive)) // Include SubCategories
                        .ThenInclude(sc => sc.Posts.Where(p => !p.IsDeleted)) // Include Posts để đếm và lấy latest
                            .ThenInclude(p => p.User) // Include User để lấy tên tác giả
                    .OrderBy(mc => mc.Order)
                    .AsNoTracking() // Tối ưu performance vì chỉ đọc
                    .ToListAsync();

                // Tạo query để lấy SubCategories với filtering
                var subCategoriesQuery = _context.SubCategories
                    .Where(sc => sc.IsActive)
                    .Include(sc => sc.MainCategory)
                    .AsQueryable();

                // Apply filters for search
                if (mainCategoryId.HasValue)
                {
                    subCategoriesQuery = subCategoriesQuery.Where(sc => sc.MainCategoryId == mainCategoryId.Value);
                }

                var subCategories = await subCategoriesQuery
                    .OrderBy(sc => sc.MainCategory.Order)
                    .ThenBy(sc => sc.Name)
                    .AsNoTracking()
                    .ToListAsync();

                // Map sang ViewModel
                viewModel.MainCategories = mainCategories.Select(mc => new MainCategoryViewModel
                {
                    Id = mc.Id,
                    Name = mc.Name,
                    Order = mc.Order,
                    IsActive = mc.IsActive,
                    SubCategories = mc.SubCategories.Select(sc => new SubCategoryViewModel
                    {
                        Id = sc.Id,
                        Name = sc.Name,
                        Description = sc.Description,
                        Icon = sc.Icon,
                        MainCategoryId = sc.MainCategoryId,
                        PostCount = sc.Posts.Count(p => !p.IsDeleted),
                        LatestPost = sc.Posts
                            .Where(p => !p.IsDeleted)
                            .OrderByDescending(p => p.CreatedAt)
                            .Select(p => new LatestPostViewModel
                            {
                                Id = p.Id,
                                Title = p.Title,
                                CreatedAt = p.CreatedAt,
                                AuthorName = p.User.DisplayName ?? p.User.UserName,
                                Views = p.Views
                            })
                            .FirstOrDefault()
                    }).ToList()
                }).ToList();

                // Lưu tham số tìm kiếm vào ViewData để sử dụng trong View
                ViewData["CurrentMainCategoryId"] = mainCategoryId;
                ViewData["CurrentSubCategoryId"] = subCategoryId;
                ViewData["SearchTerm"] = search;
                ViewData["SortBy"] = sortBy;
                ViewData["SubCategories"] = subCategories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load categories from database (likely due to pending migrations). Using Mock Data.");
                
                // FALLBACK: Use Mock Data if database fails
                viewModel.MainCategories = GetMockData();
                ViewData["IsMockData"] = true; // Flag to show alert in View
            }

            return View(viewModel);
        }

        private List<MainCategoryViewModel> GetMockData()
        {
            return new List<MainCategoryViewModel>
            {
                new MainCategoryViewModel { 
                    Id = 1, Name = "Công nghệ (Demo Data)", Order = 1,
                    SubCategories = new List<SubCategoryViewModel> {
                        new SubCategoryViewModel { 
                            Id = 1, Name = "Lập trình", Icon = "💻", Description = "Thảo luận về code & bug", 
                            PostCount = 125,
                            LatestPost = new LatestPostViewModel { Id = 1, Title = "Học ASP.NET Core MVC thế nào?", AuthorName = "DevPro", CreatedAt = DateTime.Now.AddHours(-2), Views = 500 }
                        },
                        new SubCategoryViewModel { 
                            Id = 2, Name = "Phần cứng", Icon = "🖥️", Description = "Review PC, Laptop, Gear", 
                            PostCount = 42,
                            LatestPost = new LatestPostViewModel { Id = 2, Title = "Review RTX 5090 mới nhất", AuthorName = "TechReviewer", CreatedAt = DateTime.Now.AddDays(-1), Views = 1200 }
                        }
                    }
                },
                new MainCategoryViewModel { 
                    Id = 2, Name = "Kinh doanh (Demo Data)", Order = 2,
                    SubCategories = new List<SubCategoryViewModel> {
                        new SubCategoryViewModel { 
                            Id = 3, Name = "Chứng khoán", Icon = "📈", Description = "Bàn luận thị trường", 
                            PostCount = 88,
                            LatestPost = new LatestPostViewModel { Id = 3, Title = "VnIndex vượt mốc lịch sử", AuthorName = "Broker", CreatedAt = DateTime.Now.AddMinutes(-30), Views = 800 }
                        }
                    }
                }
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
