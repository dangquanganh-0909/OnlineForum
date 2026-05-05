using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;
using System.Security.Claims;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ForumDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ReportController> _logger;

        public ReportController(ForumDbContext context, UserManager<User> userManager, ILogger<ReportController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // POST: Report/Post/5
        [HttpPost]
        public async Task<IActionResult> Post(int id, ReportReason reason, string? description)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                // Check if post exists
                var post = await _context.Posts
                    .Include(p => p.SubCategory)
                    .FirstOrDefaultAsync(p => p.Id == id);
                
                if (post == null)
                    return NotFound("Bài viết không tồn tại");

                // Check if user already reported this post
                var existingReport = await _context.Reports
                    .FirstOrDefaultAsync(r => r.UserId == user.Id && r.PostId == id);

                if (existingReport != null)
                    return Json(new { success = false, message = "Bạn đã report bài viết này rồi" });

                var report = new Report
                {
                    PostId = id,
                    UserId = user.Id,
                    Reason = reason,
                    Description = description,
                    Status = ReportStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                // Send notification to category admins
                await SendReportNotificationToAdmins(id);

                return Json(new { success = true, message = "Report đã được gửi. Cảm ơn bạn đã giúp cộng đồng an toàn hơn!" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi report bài viết: {ex.Message}");
                return Json(new { success = false, message = "Lỗi khi xử lý report" });
            }
        }

        // POST: Report/Comment/5
        [HttpPost]
        public async Task<IActionResult> Comment(int id, ReportReason reason, string? description)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var comment = await _context.Comments.FindAsync(id);
                if (comment == null)
                    return NotFound("Bình luận không tồn tại");

                // Check if user already reported this comment
                var existingReport = await _context.Reports
                    .FirstOrDefaultAsync(r => r.UserId == user.Id && r.CommentId == id);

                if (existingReport != null)
                    return Json(new { success = false, message = "Bạn đã report bình luận này rồi" });

                var report = new Report
                {
                    CommentId = id,
                    UserId = user.Id,
                    Reason = reason,
                    Description = description,
                    Status = ReportStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Report đã được gửi. Cảm ơn bạn đã giúp cộng đồng an toàn hơn!" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi report bình luận: {ex.Message}");
                return Json(new { success = false, message = "Lỗi khi xử lý report" });
            }
        }

        // Helper method to send notifications to category admins
        private async Task SendReportNotificationToAdmins(int postId)
        {
            try
            {
                var post = await _context.Posts
                    .Include(p => p.SubCategory)
                    .FirstOrDefaultAsync(p => p.Id == postId);

                if (post == null || post.SubCategory == null)
                    return;

                var admins = await _context.CategoryAdmins
                    .Where(ca => ca.SubCategoryId == post.SubCategoryId && ca.IsActive)
                    .Select(ca => ca.UserId)
                    .ToListAsync();

                foreach (var adminId in admins)
                {
                    var notification = new Notification
                    {
                        UserId = adminId,
                        Type = NotificationType.PostReport,
                        Title = $"Bài viết bị report: {post.Title}",
                        Message = $"Một bài viết trong danh mục '{post.SubCategory.Name}' đã được report.",
                        PostId = postId,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi gửi notification: {ex.Message}");
            }
        }

        // GET: Report/ManageReports (for category admins)
        public async Task<IActionResult> ManageReports()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Check if user is a category admin
            var adminOf = await _context.CategoryAdmins
                .Where(ca => ca.UserId == userId && ca.IsActive)
                .Select(ca => ca.SubCategoryId)
                .ToListAsync();

            if (adminOf.Count == 0)
                return Forbid();

            var reports = await _context.Reports
                .Include(r => r.User)
                .Include(r => r.Post)
                .ThenInclude(p => p!.SubCategory)
                .Where(r => r.PostId.HasValue && adminOf.Contains(r.Post!.SubCategoryId ?? 0))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reports);
        }

        // GET: Report/Details/5 (for category admins)
        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var report = await _context.Reports
                .Include(r => r.User)
                .Include(r => r.Post)
                .ThenInclude(p => p!.SubCategory)
                .Include(r => r.Post)
                .ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
                return NotFound();

            // Check if user is admin of the category containing this post
            if (report.PostId.HasValue)
            {
                var subCategoryId = report.Post?.SubCategoryId;
                var isAdmin = await _context.CategoryAdmins
                    .AnyAsync(ca => ca.UserId == userId && ca.SubCategoryId == subCategoryId && ca.IsActive);

                if (!isAdmin)
                    return Forbid();

                report.Status = ReportStatus.Reviewed;
                _context.Reports.Update(report);
                await _context.SaveChangesAsync();
            }

            return View(report);
        }

        // GET: Report/Manage (Admin/Moderator only)
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> Manage()
        {
            var reports = await _context.Reports
                .Include(r => r.User)
                .Include(r => r.Post)
                    .ThenInclude(p => p!.User)
                .Include(r => r.Comment)
                    .ThenInclude(c => c!.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reports);
        }

        // POST: Report/UpdateStatus/5
        [HttpPost]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> UpdateStatus(int id, ReportStatus status)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null)
            {
                return NotFound();
            }

            report.Status = status;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: Report/ResolveReport/5 (for category admins)
        [HttpPost]
        public async Task<IActionResult> ResolveReport(int id, string? action)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var report = await _context.Reports
                .Include(r => r.Post)
                .ThenInclude(p => p!.SubCategory)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
                return NotFound();

            // Check if user is admin of the category
            var subCatId = report.Post?.SubCategoryId;
            var isAdmin = await _context.CategoryAdmins
                .AnyAsync(ca => ca.UserId == userId && ca.SubCategoryId == subCatId && ca.IsActive);

            if (!isAdmin)
                return Forbid();

            report.Status = ReportStatus.Resolved;
            _context.Reports.Update(report);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Report đã được xử lý" });
        }
    }
}

