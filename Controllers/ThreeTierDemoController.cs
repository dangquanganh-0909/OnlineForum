using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Controller demo cho hệ thống phân cấp 3 tầng
    /// MainCategory -> SubCategory -> Post
    /// </summary>
    public class ThreeTierDemoController : Controller
    {
        private readonly ForumDbContext _context;
        private readonly ILogger<ThreeTierDemoController> _logger;

        public ThreeTierDemoController(ForumDbContext context, ILogger<ThreeTierDemoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ================================================
        // MAIN CATEGORIES
        // ================================================

        /// <summary>
        /// GET: /ThreeTierDemo/MainCategories
        /// Hiển thị tất cả cụm danh mục
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MainCategories()
        {
            var mainCategories = await _context.MainCategories
                .Include(mc => mc.SubCategories) // Eager loading
                .OrderBy(mc => mc.Order)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = mainCategories.Select(mc => new
                {
                    mc.Id,
                    mc.Name,
                    mc.Order,
                    mc.IsActive,
                    SubCategoryCount = mc.SubCategories.Count,
                    SubCategories = mc.SubCategories.Select(sc => new
                    {
                        sc.Id,
                        sc.Name,
                        sc.Icon
                    })
                })
            });
        }

        /// <summary>
        /// GET: /ThreeTierDemo/GetMainCategory/1
        /// Lấy chi tiết một cụm danh mục
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMainCategory(int id)
        {
            var mainCategory = await _context.MainCategories
                .Include(mc => mc.SubCategories)
                    .ThenInclude(sc => sc.Posts)
                .FirstOrDefaultAsync(mc => mc.Id == id);

            if (mainCategory == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy cụm danh mục" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    mainCategory.Id,
                    mainCategory.Name,
                    mainCategory.Order,
                    SubCategories = mainCategory.SubCategories.Select(sc => new
                    {
                        sc.Id,
                        sc.Name,
                        sc.Description,
                        sc.Icon,
                        PostCount = sc.Posts.Count
                    })
                }
            });
        }

        // ================================================
        // SUB CATEGORIES
        // ================================================

        /// <summary>
        /// GET: /ThreeTierDemo/SubCategories?mainCategoryId=1
        /// Lấy danh sách chuyên mục theo cụm danh mục
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SubCategories(int? mainCategoryId)
        {
            var query = _context.SubCategories
                .Include(sc => sc.MainCategory)
                .AsQueryable();

            if (mainCategoryId.HasValue)
            {
                query = query.Where(sc => sc.MainCategoryId == mainCategoryId.Value);
            }

            var subCategories = await query.ToListAsync();

            return Ok(new
            {
                success = true,
                data = subCategories.Select(sc => new
                {
                    sc.Id,
                    sc.Name,
                    sc.Description,
                    sc.Icon,
                    sc.MainCategoryId,
                    MainCategoryName = sc.MainCategory.Name
                })
            });
        }

        /// <summary>
        /// GET: /ThreeTierDemo/GetSubCategory/1
        /// Lấy chi tiết một chuyên mục
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            var subCategory = await _context.SubCategories
                .Include(sc => sc.MainCategory)
                .Include(sc => sc.Posts)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(sc => sc.Id == id);

            if (subCategory == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy chuyên mục" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    subCategory.Id,
                    subCategory.Name,
                    subCategory.Description,
                    subCategory.Icon,
                    MainCategory = new
                    {
                        subCategory.MainCategory.Id,
                        subCategory.MainCategory.Name,
                        subCategory.MainCategory.Order
                    },
                    Posts = subCategory.Posts.Select(p => new
                    {
                        p.Id,
                        p.Title,
                        p.Views,
                        p.CreatedAt,
                        Author = p.User.UserName
                    })
                }
            });
        }

        // ================================================
        // POSTS
        // ================================================

        /// <summary>
        /// GET: /ThreeTierDemo/Posts?subCategoryId=1
        /// Lấy danh sách bài viết theo chuyên mục
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Posts(int? subCategoryId, int page = 1, int pageSize = 10)
        {
            var query = _context.Posts
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc.MainCategory)
                .Include(p => p.User)
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            if (subCategoryId.HasValue)
            {
                query = query.Where(p => p.SubCategoryId == subCategoryId.Value);
            }

            var totalCount = await query.CountAsync();
            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                },
                data = posts.Select(p => new
                {
                    p.Id,
                    p.Title,
                    ContentPreview = p.Content.Length > 200 ? p.Content.Substring(0, 200) + "..." : p.Content,
                    p.Views,
                    p.CreatedAt,
                    Author = p.User.UserName,
                    SubCategory = p.SubCategory != null ? new
                    {
                        p.SubCategory.Id,
                        p.SubCategory.Name,
                        p.SubCategory.Icon,
                        MainCategoryName = p.SubCategory.MainCategory?.Name
                    } : null
                })
            });
        }

        /// <summary>
        /// GET: /ThreeTierDemo/GetFullHierarchy/5
        /// Lấy toàn bộ cây phân cấp từ Post -> SubCategory -> MainCategory
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFullHierarchy(int postId)
        {
            var post = await _context.Posts
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc.MainCategory)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy bài viết" });
            }

            return Ok(new
            {
                success = true,
                hierarchy = new
                {
                    MainCategory = post.SubCategory?.MainCategory != null ? new
                    {
                        post.SubCategory.MainCategory.Id,
                        post.SubCategory.MainCategory.Name,
                        post.SubCategory.MainCategory.Order
                    } : null,
                    SubCategory = post.SubCategory != null ? new
                    {
                        post.SubCategory.Id,
                        post.SubCategory.Name,
                        post.SubCategory.Icon,
                        post.SubCategory.Description
                    } : null,
                    Post = new
                    {
                        post.Id,
                        post.Title,
                        post.Content,
                        post.Views,
                        post.CreatedAt,
                        Author = post.User.UserName
                    }
                }
            });
        }

        // ================================================
        // STATISTICS
        // ================================================

        /// <summary>
        /// GET: /ThreeTierDemo/Statistics
        /// Thống kê toàn bộ hệ thống 3 tầng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            var stats = await _context.MainCategories
                .Include(mc => mc.SubCategories)
                    .ThenInclude(sc => sc.Posts)
                .Select(mc => new
                {
                    MainCategoryId = mc.Id,
                    MainCategoryName = mc.Name,
                    Order = mc.Order,
                    SubCategoryCount = mc.SubCategories.Count,
                    TotalPosts = mc.SubCategories.Sum(sc => sc.Posts.Count),
                    TotalViews = mc.SubCategories.SelectMany(sc => sc.Posts).Sum(p => p.Views),
                    SubCategories = mc.SubCategories.Select(sc => new
                    {
                        SubCategoryId = sc.Id,
                        SubCategoryName = sc.Name,
                        Icon = sc.Icon,
                        PostCount = sc.Posts.Count,
                        TotalViews = sc.Posts.Sum(p => p.Views)
                    }).OrderByDescending(sc => sc.PostCount)
                })
                .OrderBy(mc => mc.Order)
                .ToListAsync();

            var totalStats = new
            {
                TotalMainCategories = await _context.MainCategories.CountAsync(),
                TotalSubCategories = await _context.SubCategories.CountAsync(),
                TotalPosts = await _context.Posts.Where(p => !p.IsDeleted && p.SubCategoryId != null).CountAsync(),
                TotalViews = await _context.Posts.Where(p => !p.IsDeleted && p.SubCategoryId != null).SumAsync(p => p.Views)
            };

            return Ok(new
            {
                success = true,
                summary = totalStats,
                details = stats
            });
        }

        // ================================================
        // BREADCRUMB
        // ================================================

        /// <summary>
        /// GET: /ThreeTierDemo/Breadcrumb/5
        /// Lấy breadcrumb navigation từ Post
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Breadcrumb(int postId)
        {
            var post = await _context.Posts
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc.MainCategory)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy bài viết" });
            }

            var breadcrumb = new List<object>
            {
                new { text = "Trang chủ", url = "/" }
            };

            if (post.SubCategory?.MainCategory != null)
            {
                breadcrumb.Add(new
                {
                    text = post.SubCategory.MainCategory.Name,
                    url = $"/MainCategory/{post.SubCategory.MainCategory.Id}"
                });
            }

            if (post.SubCategory != null)
            {
                breadcrumb.Add(new
                {
                    text = post.SubCategory.Name,
                    url = $"/SubCategory/{post.SubCategory.Id}"
                });
            }

            breadcrumb.Add(new
            {
                text = post.Title,
                url = $"/Post/{post.Id}",
                active = true
            });

            return Ok(new
            {
                success = true,
                breadcrumb
            });
        }
    }
}
