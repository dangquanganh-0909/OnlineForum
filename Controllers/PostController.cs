using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class PostController : Controller
    {
        private readonly ForumDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IFileUploadService _fileUploadService;
        private readonly ISimilarContentService _similarContentService;

        public PostController(ForumDbContext context, UserManager<User> userManager, IFileUploadService fileUploadService, ISimilarContentService similarContentService)
        {
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
            _similarContentService = similarContentService;
        }

        // GET: Post
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? subCategoryId, int? mainCategoryId, string? search, string? sortBy = "recent", int page = 1)
        {
            const int pageSize = 10;
            
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc.MainCategory)
                .Include(p => p.Category) // Giữ lại để tương thích ngược
                .Include(p => p.PostLikes)
                .Include(p => p.Comments)
                .Where(p => !p.IsDeleted);

            if (subCategoryId.HasValue)
            {
                query = query.Where(p => p.SubCategoryId == subCategoryId.Value);
            }
            else if (mainCategoryId.HasValue)
            {
                query = query.Where(p => p.SubCategory != null && p.SubCategory.MainCategoryId == mainCategoryId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Title.Contains(search) || p.Content.Contains(search));
            }

            // Sắp xếp theo tiêu chí
            switch (sortBy?.ToLower())
            {
                case "views":
                    query = query.OrderByDescending(p => p.Views);
                    break;
                case "interaction":
                    query = query.OrderByDescending(p => p.PostLikes.Count + p.Comments.Count);
                    break;
                case "length":
                    query = query.OrderByDescending(p => p.Content.Length);
                    break;
                case "recent":
                default:
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            var totalPosts = await query.CountAsync();
            var posts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mainCategories = await _context.MainCategories
                .Where(mc => mc.IsActive)
                .Include(mc => mc.SubCategories.Where(sc => sc.IsActive))
                .OrderBy(mc => mc.Order)
                .ToListAsync();

            var subCategories = await _context.SubCategories
                .Where(sc => sc.IsActive)
                .Include(sc => sc.MainCategory)
                .OrderBy(sc => sc.MainCategory.Order).ThenBy(sc => sc.Name)
                .ToListAsync();

            var viewModel = new PostIndexViewModel
            {
                Posts = posts,
                MainCategories = mainCategories,
                SubCategories = subCategories,
                CurrentSubCategoryId = subCategoryId,
                CurrentMainCategoryId = mainCategoryId,
                SearchTerm = search,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize),
                SortBy = sortBy ?? "recent"
            };

            // Nếu có subCategoryId hoặc mainCategoryId -> hiển thị danh sách bài viết
            // Ngược lại -> hiển thị cards categories
            if (subCategoryId.HasValue || mainCategoryId.HasValue)
            {
                return View("PostList", viewModel);
            }

            return View(viewModel);
        }

        // GET: Post/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.PostLikes)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.User)
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.CommentLikes)
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.Replies.Where(r => !r.IsDeleted))
                        .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (post == null)
            {
                return NotFound();
            }

            // Increment view count
            post.Views++;
            await _context.SaveChangesAsync();

            return View(post);
        }

        // GET: Post/Create
        // Supports: /Post/Create?subId=5 (auto-select subcategory)
        [Authorize]
        public async Task<IActionResult> Create(int? subId)
        {
            // Check if user is locked
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsLocked == true)
                return RedirectToAction("UserLocked", "User", new { id = user.Id });

            var model = new CreatePostViewModel();
            
            // If coming from a specific SubCategory
            if (subId.HasValue)
            {
                var subCategory = await _context.SubCategories
                    .Include(sc => sc.MainCategory)
                    .FirstOrDefaultAsync(sc => sc.Id == subId.Value);
                
                if (subCategory != null)
                {
                    model.SubCategoryId = subCategory.Id;
                    model.MainCategoryId = subCategory.MainCategoryId;
                    
                    // For backward compatibility with old Category system
                    // You can map to a default CategoryId if needed
                    var defaultCategory = await _context.Categories.FirstOrDefaultAsync();
                    model.CategoryId = defaultCategory?.Id ?? 1;
                    
                    // Load SubCategories for the selected MainCategory
                    ViewBag.SubCategoryName = subCategory.Name;
                    ViewBag.MainCategoryName = subCategory.MainCategory.Name;
                    ViewBag.SubCategories = new SelectList(
                        await _context.SubCategories
                            .Where(sc => sc.MainCategoryId == subCategory.MainCategoryId && sc.IsActive)
                            .ToListAsync(),
                        "Id", "Name", subCategory.Id
                    );
                }
            }
            
            // Load MainCategories for dropdown
            ViewBag.MainCategories = new SelectList(
                await _context.MainCategories
                    .Where(mc => mc.IsActive)
                    .OrderBy(mc => mc.Order)
                    .ToListAsync(),
                "Id", "Name", model.MainCategoryId
            );
            
            // Load all Categories (for backward compatibility)
            ViewData["CategoryId"] = new SelectList(
                await _context.Categories.Where(c => c.IsActive).ToListAsync(),
                "Id", "Name"
            );
            
            return View(model);
        }

        // POST: Post/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(CreatePostViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                
                // Validate file uploads
                string? imagePath = null;
                string? videoPath = null;
                
                if (model.Image != null)
                {
                    if (!_fileUploadService.IsValidImageFile(model.Image))
                    {
                        ModelState.AddModelError("Image", "File ảnh không hợp lệ. Chỉ chấp nhận jpg, jpeg, png, gif, webp và tối đa 10MB.");
                    }
                    else
                    {
                        imagePath = await _fileUploadService.UploadImageAsync(model.Image);
                        if (imagePath == null)
                        {
                            ModelState.AddModelError("Image", "Lỗi khi upload ảnh. Vui lòng thử lại.");
                        }
                    }
                }
                
                if (model.Video != null)
                {
                    if (!_fileUploadService.IsValidVideoFile(model.Video))
                    {
                        ModelState.AddModelError("Video", "File video không hợp lệ. Chỉ chấp nhận mp4, webm, ogg, mov, avi và tối đa 100MB.");
                    }
                    else
                    {
                        videoPath = await _fileUploadService.UploadVideoAsync(model.Video);
                        if (videoPath == null)
                        {
                            ModelState.AddModelError("Video", "Lỗi khi upload video. Vui lòng thử lại.");
                        }
                    }
                }
                
                if (ModelState.IsValid)
                {
                    var post = new Post
                    {
                        Title = model.Title,
                        Content = model.Content,
                        CategoryId = model.CategoryId,
                        SubCategoryId = model.SubCategoryId, // New 3-tier system
                        UserId = user!.Id,
                        CreatedAt = DateTime.UtcNow,
                        ImagePath = imagePath,
                        VideoPath = videoPath
                    };

                    _context.Add(post);
                    await _context.SaveChangesAsync();

                    // Handle tags
                    if (!string.IsNullOrEmpty(model.Tags))
                    {
                        var tagNames = model.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim().ToLower())
                            .Distinct();

                        foreach (var tagName in tagNames)
                        {
                            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                            if (tag == null)
                            {
                                tag = new Tag { Name = tagName };
                                _context.Tags.Add(tag);
                                await _context.SaveChangesAsync();
                            }

                            _context.PostTags.Add(new PostTag { PostId = post.Id, TagId = tag.Id });
                        }
                        await _context.SaveChangesAsync();
                    }

                    // Tạo notification cho tất cả followers
                    var followers = await _context.UserFollows
                        .Where(uf => uf.FollowingId == user!.Id)
                        .Select(uf => uf.FollowerId)
                        .ToListAsync();

                    foreach (var followerId in followers)
                    {
                        var notification = new Notification
                        {
                            Type = NotificationType.PostReply,
                            Title = $"{user.DisplayName ?? user.UserName} đã đăng bài viết mới",
                            Message = $"\"{post.Title}\"",
                            UserId = followerId,
                            PostId = post.Id,
                            RelatedUserId = user.Id,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Notifications.Add(notification);
                    }
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Details), new { id = post.Id });
                }
            }
            
            ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", model.CategoryId);
            return View(model);
        }

        // GET: Post/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
                
            if (post == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (post.UserId != user!.Id && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            var viewModel = new CreatePostViewModel
            {
                Title = post.Title,
                Content = post.Content,
                CategoryId = post.CategoryId,
                Tags = string.Join(", ", post.PostTags.Select(pt => pt.Tag.Name))
            };

            ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", post.CategoryId);
            return View(viewModel);
        }

        // POST: Post/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, CreatePostViewModel model)
        {
            var post = await _context.Posts
                .Include(p => p.PostTags)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
                
            if (post == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (post.UserId != user!.Id && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                post.Title = model.Title;
                post.Content = model.Content;
                post.CategoryId = model.CategoryId;
                post.UpdatedAt = DateTime.UtcNow;

                // Remove existing tags
                _context.PostTags.RemoveRange(post.PostTags);

                // Add new tags
                if (!string.IsNullOrEmpty(model.Tags))
                {
                    var tagNames = model.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim().ToLower())
                        .Distinct();

                    foreach (var tagName in tagNames)
                    {
                        var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                        if (tag == null)
                        {
                            tag = new Tag { Name = tagName };
                            _context.Tags.Add(tag);
                            await _context.SaveChangesAsync();
                        }

                        _context.PostTags.Add(new PostTag { PostId = post.Id, TagId = tag.Id });
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = post.Id });
            }
            
            ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", model.CategoryId);
            return View(model);
        }

        // POST: Post/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (post.UserId != user!.Id && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            post.IsDeleted = true;
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Post/Like/5
        [HttpPost]
        [Authorize]
        [Route("Post/Like/{id}")]
        public async Task<IActionResult> Like(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var post = await _context.Posts.FindAsync(id);
            
            if (post == null)
            {
                return NotFound();
            }

            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(pl => pl.PostId == id && pl.UserId == user!.Id);

            if (existingLike != null)
            {
                _context.PostLikes.Remove(existingLike);
            }
            else
            {
                _context.PostLikes.Add(new PostLike 
                { 
                    PostId = id, 
                    UserId = user!.Id,
                    CreatedAt = DateTime.UtcNow
                });

                // Tạo notification cho tác giả bài viết
                if (post.UserId != user.Id)
                {
                    var notification = new Notification
                    {
                        Type = NotificationType.PostLike,
                        Title = $"{user.DisplayName ?? user.UserName} đã thích bài viết của bạn",
                        Message = $"\"{post.Title}\"",
                        UserId = post.UserId,
                        PostId = id,
                        RelatedUserId = user.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                }
            }

            await _context.SaveChangesAsync();
            
            var likeCount = await _context.PostLikes.CountAsync(pl => pl.PostId == id);
            return Json(new { success = true, likeCount = likeCount });
        }

        // GET: Post/GetByCategory
        [AllowAnonymous]
        public async Task<IActionResult> GetByCategory(int? categoryId = null, int? subCategoryId = null, int? mainCategoryId = null, string? sortBy = "recent")
        {
            const int pageSize = 10;
            var page = 1;
            
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc.MainCategory)
                .Include(p => p.Category) // Giữ lại cho tương thích ngược
                .Include(p => p.PostLikes)
                .Include(p => p.Comments)
                .Where(p => !p.IsDeleted);

            // Filter theo hệ thống mới (SubCategory)
            if (subCategoryId.HasValue)
            {
                query = query.Where(p => p.SubCategoryId == subCategoryId.Value);
            }
            else if (mainCategoryId.HasValue)
            {
                query = query.Where(p => p.SubCategory != null && p.SubCategory.MainCategoryId == mainCategoryId.Value);
            }
            // Fallback cho hệ thống cũ (Category)
            else if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Sắp xếp theo tiêu chí
            switch (sortBy?.ToLower())
            {
                case "views":
                    query = query.OrderByDescending(p => p.Views);
                    break;
                case "interaction":
                    query = query.OrderByDescending(p => p.PostLikes.Count + p.Comments.Count);
                    break;
                case "length":
                    query = query.OrderByDescending(p => p.Content.Length);
                    break;
                case "recent":
                default:
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            var totalPosts = await query.CountAsync();
            var posts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new PostIndexViewModel
            {
                Posts = posts,
                CurrentSubCategoryId = subCategoryId,
                CurrentMainCategoryId = mainCategoryId,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize),
                SortBy = sortBy ?? "recent"
            };

            return PartialView("_PostList", viewModel);
        }
        
        // API: Get SubCategories by MainCategoryId
        // Used for cascade dropdown in Create Post form
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetSubCategoriesByMain(int mainCategoryId)
        {
            var subCategories = await _context.SubCategories
                .Where(sc => sc.MainCategoryId == mainCategoryId && sc.IsActive)
                .OrderBy(sc => sc.Name)
                .Select(sc => new
                {
                    id = sc.Id,
                    name = sc.Name,
                    icon = sc.Icon,
                    description = sc.Description
                })
                .ToListAsync();
            
            return Json(subCategories);
        }

        // API: Tìm bài viết tương tự để gợi ý tránh trùng lặp
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> GetSimilarPosts([FromBody] SimilarPostRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(request.Content))
            {
                return Json(new { success = false, message = "Vui lòng nhập tiêu đề hoặc nội dung" });
            }

            try
            {
                var similarPosts = await _similarContentService.FindSimilarPostsAsync(
                    request.Title ?? string.Empty,
                    request.Content ?? string.Empty,
                    request.ExcludePostId,
                    3, // Tối đa 3 bài viết
                    0.7 // Độ tương tự tối thiểu 70%
                );

                var response = new
                {
                    success = true,
                    similarPosts = similarPosts.Select(sp => new
                    {
                        postId = sp.PostId,
                        title = sp.Title,
                        content = sp.Content,
                        similarityScore = Math.Round(sp.SimilarityScore * 100, 1),
                        createdAt = sp.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        authorName = sp.AuthorName,
                        subCategoryName = sp.SubCategoryName,
                        views = sp.Views,
                        commentsCount = sp.CommentsCount,
                        likesCount = sp.LikesCount,
                        postUrl = Url.Action("Details", "Post", new { id = sp.PostId })
                    }).ToList()
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi tìm kiếm bài viết tương tự" });
            }
        }
    }

    public class SimilarPostRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public int? ExcludePostId { get; set; }
    }
}
