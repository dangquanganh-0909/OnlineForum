using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ForumDbContext _context;
        private readonly UserManager<User> _userManager;

        public UserController(ForumDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: User/Profile/5
        public async Task<IActionResult> Profile(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                id = currentUser.Id;
            }

            var user = await _context.Users
                .Include(u => u.Posts.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Category)
                .Include(u => u.Posts.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.PostLikes)
                .Include(u => u.Comments.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.Post)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Check if user is locked
            if (user.IsLocked)
            {
                return View("UserLocked", user);
            }

            // Check if current user is following this user
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            bool isFollowing = false;
            
            if (!string.IsNullOrEmpty(currentUserId) && currentUserId != id)
            {
                isFollowing = await _context.UserFollows
                    .AnyAsync(uf => uf.FollowerId == currentUserId && uf.FollowingId == id);
            }

            var viewModel = new UserProfileViewModel
            {
                User = user,
                RecentPosts = user.Posts.OrderByDescending(p => p.CreatedAt).Take(10).ToList(),
                RecentComments = user.Comments.OrderByDescending(c => c.CreatedAt).Take(10).ToList(),
                TotalPosts = user.Posts.Count,
                TotalComments = user.Comments.Count,
                TotalLikes = user.Posts.Sum(p => p.PostLikes.Count),
                Followers = user.Followers.Select(uf => uf.Follower!).ToList(),
                Following = user.Following.Select(uf => uf.Following!).ToList(),
                IsFollowing = isFollowing
            };

            return View(viewModel);
        }

        // GET: User/Edit
        [Authorize]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new EditProfileViewModel
            {
                DisplayName = user.DisplayName ?? user.UserName!,
                Bio = user.Bio,
                Email = user.Email!
            };

            return View(viewModel);
        }

        // POST: User/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(EditProfileViewModel model, IFormFile? avatar)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                user.DisplayName = model.DisplayName;
                user.Bio = model.Bio;
                user.Email = model.Email;

                // Handle avatar upload
                if (avatar != null && avatar.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
                    
                    if (allowedExtensions.Contains(extension) && avatar.Length <= 2 * 1024 * 1024) // 2MB limit
                    {
                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var uploadsPath = Path.Combine("wwwroot", "uploads", "avatars");
                        Directory.CreateDirectory(uploadsPath);
                        
                        var filePath = Path.Combine(uploadsPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await avatar.CopyToAsync(stream);
                        }
                        
                        user.Avatar = $"/uploads/avatars/{fileName}";
                    }
                    else
                    {
                        ModelState.AddModelError("Avatar", "Please upload a valid image file (JPG, PNG, GIF) under 2MB.");
                        return View(model);
                    }
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Profile));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: User/Notifications
        [Authorize]
        public async Task<IActionResult> Notifications(int page = 1)
        {
            const int pageSize = 20;
            var user = await _userManager.GetUserAsync(User);
            
            var query = _context.Notifications
                .Include(n => n.Post)
                .Include(n => n.Comment)
                .Include(n => n.RelatedUser)
                .Where(n => n.UserId == user!.Id)
                .OrderByDescending(n => n.CreatedAt);

            var totalNotifications = await query.CountAsync();
            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy số thông báo chưa đọc
            var unreadCount = await _context.Notifications
                .Where(n => n.UserId == user!.Id && !n.IsRead)
                .CountAsync();

            ViewBag.Notifications = notifications;
            ViewBag.UnreadCount = unreadCount;
            ViewBag.PageNumber = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalNotifications / pageSize);

            return View();
        }

        // POST: User/MarkAllNotificationsRead
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == user!.Id && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST: User/MarkNotificationRead/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user!.Id);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // POST: User/ToggleActive/5 - Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Không cho phép admin tự khóa chính mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == id)
            {
                TempData["Error"] = "Bạn không thể khóa tài khoản của chính mình.";
                return RedirectToAction("Profile", new { id });
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = user.IsActive ? "Đã mở khóa tài khoản thành công." : "Đã khóa tài khoản thành công.";
            return RedirectToAction("Profile", new { id });
        }

        // POST: User/Follow/5
        [HttpPost]
        public async Task<IActionResult> Follow(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Không thể follow chính mình
            if (currentUser.Id == id)
            {
                TempData["Error"] = "Không thể follow chính mình";
                return RedirectToAction("Profile", new { id });
            }

            var targetUser = await _context.Users.FindAsync(id);
            if (targetUser == null)
            {
                return NotFound();
            }

            // Kiểm tra xem đã follow chưa
            var existingFollow = await _context.UserFollows
                .FirstOrDefaultAsync(uf => uf.FollowerId == currentUser.Id && uf.FollowingId == id);

            if (existingFollow != null)
            {
                _context.UserFollows.Remove(existingFollow);
                TempData["Success"] = $"Đã bỏ theo dõi {targetUser.DisplayName ?? targetUser.UserName}";
            }
            else
            {
                _context.UserFollows.Add(new UserFollow 
                { 
                    FollowerId = currentUser.Id, 
                    FollowingId = id 
                });

                // Tạo thông báo
                var notification = new Notification
                {
                    Type = NotificationType.Follow,
                    Title = $"{currentUser.DisplayName ?? currentUser.UserName} đã follow bạn",
                    Message = $"{currentUser.DisplayName ?? currentUser.UserName} vừa bắt đầu follow bạn",
                    UserId = id,
                    RelatedUserId = currentUser.Id,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                TempData["Success"] = $"Đã follow {targetUser.DisplayName ?? targetUser.UserName}";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Profile", new { id });
        }

        // GET: User/Followers/5
        [AllowAnonymous]
        public async Task<IActionResult> Followers(string id)
        {
            var user = await _context.Users
                .Include(u => u.Followers)
                    .ThenInclude(f => f.Follower)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new UserProfileViewModel
            {
                User = user,
                Followers = user.Followers.Select(f => f.Follower!).ToList()
            };

            return View(viewModel);
        }

        // GET: User/Following/5
        [AllowAnonymous]
        public async Task<IActionResult> Following(string id)
        {
            var user = await _context.Users
                .Include(u => u.Following)
                    .ThenInclude(f => f.Following)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new UserProfileViewModel
            {
                User = user,
                Following = user.Following.Select(f => f.Following!).ToList()
            };

            return View(viewModel);
        }

        // GET: User/ManageUsers - Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageUsers(int page = 1, string? search = "")
        {
            const int pageSize = 20;
            
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.UserName!.Contains(search) || 
                                         u.Email!.Contains(search) || 
                                         (u.DisplayName != null && u.DisplayName.Contains(search)));
            }

            query = query.OrderByDescending(u => u.JoinDate);

            var totalUsers = await query.CountAsync();
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy roles cho từng user
            var usersWithRoles = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add(new 
                {
                    User = user,
                    Roles = roles,
                    PostCount = await _context.Posts.CountAsync(p => p.UserId == user.Id && !p.IsDeleted),
                    CommentCount = await _context.Comments.CountAsync(c => c.UserId == user.Id && !c.IsDeleted)
                });
            }

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalUsers / pageSize);
            ViewData["Search"] = search;
            
            return View(usersWithRoles);
        }
    }
}
