using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    public class CommentController : Controller
    {
        private readonly ForumDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IFileUploadService _fileUploadService;

        public CommentController(ForumDbContext context, UserManager<User> userManager, IFileUploadService fileUploadService)
        {
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
        }

        // POST: Comment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(CreateCommentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                
                // Check if user is locked
                if (user?.IsLocked == true)
                    return Json(new { success = false, message = "Tài khoản của bạn đã bị khóa. Không thể bình luận." });
                
                // Handle file uploads
                string? imagePath = null;
                string? videoPath = null;
                
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    imagePath = await _fileUploadService.UploadImageAsync(model.ImageFile);
                }
                
                if (model.VideoFile != null && model.VideoFile.Length > 0)
                {
                    videoPath = await _fileUploadService.UploadVideoAsync(model.VideoFile);
                }
                
                var comment = new Comment
                {
                    Content = model.Content,
                    PostId = model.PostId,
                    UserId = user!.Id,
                    ParentCommentId = model.ParentCommentId,
                    ImagePath = imagePath,
                    VideoPath = videoPath,
                    FontFamily = model.FontFamily,
                    FontColor = model.FontColor,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // Create notification for post author (if not commenting on own post)
                var post = await _context.Posts.FindAsync(model.PostId);
                if (post != null && post.UserId != user.Id)
                {
                    var notification = new Notification
                    {
                        Type = NotificationType.PostReply,
                        Title = $"{user.DisplayName ?? user.UserName} đã bình luận bài viết của bạn",
                        Message = $"\"{post.Title}\"",
                        UserId = post.UserId,
                        PostId = model.PostId,
                        CommentId = comment.Id,
                        RelatedUserId = user.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                // Create notification for parent comment author (if replying)
                if (model.ParentCommentId.HasValue)
                {
                    var parentComment = await _context.Comments.FindAsync(model.ParentCommentId.Value);
                    if (parentComment != null && parentComment.UserId != user.Id)
                    {
                        var notification = new Notification
                        {
                            Type = NotificationType.CommentReply,
                            Title = $"{user.DisplayName ?? user.UserName} đã trả lời bình luận của bạn",
                            Message = $"Trên bài viết: \"{post?.Title}\"",
                            UserId = parentComment.UserId,
                            PostId = model.PostId,
                            CommentId = comment.Id,
                            RelatedUserId = user.Id,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Notifications.Add(notification);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return RedirectToAction("Details", "Post", new { id = model.PostId });
        }

        // GET: Comment/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                
            if (comment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (comment.UserId != user!.Id && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            var viewModel = new EditCommentViewModel
            {
                Id = comment.Id,
                Content = comment.Content,
                PostId = comment.PostId,
                ImagePath = comment.ImagePath,
                VideoPath = comment.VideoPath,
                FontFamily = comment.FontFamily,
                FontColor = comment.FontColor
            };

            return View(viewModel);
        }

        // POST: Comment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, EditCommentViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null || comment.IsDeleted)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (comment.UserId != user!.Id && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                comment.Content = model.Content;
                comment.FontFamily = model.FontFamily;
                comment.FontColor = model.FontColor;
                comment.UpdatedAt = DateTime.UtcNow;
                
                // Handle image upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(comment.ImagePath))
                    {
                        _fileUploadService.DeleteFile(comment.ImagePath);
                    }
                    comment.ImagePath = await _fileUploadService.UploadImageAsync(model.ImageFile);
                }
                else if (model.DeleteImage && !string.IsNullOrEmpty(comment.ImagePath))
                {
                    _fileUploadService.DeleteFile(comment.ImagePath);
                    comment.ImagePath = null;
                }
                
                // Handle video upload
                if (model.VideoFile != null && model.VideoFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(comment.VideoPath))
                    {
                        _fileUploadService.DeleteFile(comment.VideoPath);
                    }
                    comment.VideoPath = await _fileUploadService.UploadVideoAsync(model.VideoFile);
                }
                else if (model.DeleteVideo && !string.IsNullOrEmpty(comment.VideoPath))
                {
                    _fileUploadService.DeleteFile(comment.VideoPath);
                    comment.VideoPath = null;
                }
                
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Post", new { id = comment.PostId });
            }
            
            return View(model);
        }

        // POST: Comment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (comment.UserId != user!.Id && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            comment.IsDeleted = true;
            await _context.SaveChangesAsync();
            
            return RedirectToAction("Details", "Post", new { id = comment.PostId });
        }

        // POST: Comment/Like/5
        [HttpPost]
        [Authorize]
        [Route("Comment/Like/{id}")]
        public async Task<IActionResult> Like(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var comment = await _context.Comments.FindAsync(id);
            
            if (comment == null)
            {
                return NotFound();
            }

            var existingLike = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.CommentId == id && cl.UserId == user!.Id);

            if (existingLike != null)
            {
                _context.CommentLikes.Remove(existingLike);
            }
            else
            {
                _context.CommentLikes.Add(new CommentLike 
                { 
                    CommentId = id, 
                    UserId = user!.Id,
                    CreatedAt = DateTime.UtcNow
                });

                // Tạo notification cho tác giả comment
                if (comment.UserId != user.Id)
                {
                    var post = await _context.Posts.FindAsync(comment.PostId);
                    var notification = new Notification
                    {
                        Type = NotificationType.CommentLike,
                        Title = $"{user.DisplayName ?? user.UserName} đã thích bình luận của bạn",
                        Message = $"Trên bài viết: \"{post?.Title}\"",
                        UserId = comment.UserId,
                        CommentId = id,
                        PostId = comment.PostId,
                        RelatedUserId = user.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                }
            }

            await _context.SaveChangesAsync();
            
            var likeCount = await _context.CommentLikes.CountAsync(cl => cl.CommentId == id);
            return Json(new { success = true, likeCount = likeCount });
        }
    }
}
