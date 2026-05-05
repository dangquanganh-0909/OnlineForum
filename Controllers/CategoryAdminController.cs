using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Security.Claims;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class CategoryAdminController : Controller
    {
        private readonly ForumDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<CategoryAdminController> _logger;

        public CategoryAdminController(ForumDbContext context, UserManager<User> userManager, ILogger<CategoryAdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: CategoryAdmin/Index
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Index()
        {
            var admins = await _context.CategoryAdmins
                .Include(ca => ca.User)
                .Include(ca => ca.SubCategory)
                .ThenInclude(sc => sc.MainCategory)
                .OrderBy(ca => ca.SubCategory.MainCategory.Name)
                .ThenBy(ca => ca.SubCategory.Name)
                .ToListAsync();

            return View(admins);
        }

        // GET: CategoryAdmin/Create
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create()
        {
            ViewData["UserId"] = new SelectList(
                await _userManager.Users.Where(u => u.IsActive && !u.IsLocked).ToListAsync(),
                "Id",
                "DisplayName"
            );

            ViewData["SubCategoryId"] = new SelectList(
                await _context.SubCategories
                    .Include(sc => sc.MainCategory)
                    .OrderBy(sc => sc.MainCategory.Name)
                    .ThenBy(sc => sc.Name)
                    .ToListAsync(),
                "Id",
                "Name"
            );

            return View();
        }

        // POST: CategoryAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(CategoryAdmin model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check if user is already admin of this category
                    var existing = await _context.CategoryAdmins
                        .FirstOrDefaultAsync(ca => ca.UserId == model.UserId && ca.SubCategoryId == model.SubCategoryId);

                    if (existing != null)
                    {
                        ModelState.AddModelError("", "Người dùng này đã là admin của danh mục này rồi");
                    }
                    else
                    {
                        model.AssignedAt = DateTime.UtcNow;
                        model.IsActive = true;
                        
                        _context.CategoryAdmins.Add(model);
                        await _context.SaveChangesAsync();

                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tạo category admin: {ex.Message}");
                ModelState.AddModelError("", "Lỗi khi tạo category admin");
            }

            ViewData["UserId"] = new SelectList(
                await _userManager.Users.Where(u => u.IsActive && !u.IsLocked).ToListAsync(),
                "Id",
                "DisplayName",
                model.UserId
            );

            ViewData["SubCategoryId"] = new SelectList(
                await _context.SubCategories
                    .Include(sc => sc.MainCategory)
                    .OrderBy(sc => sc.MainCategory.Name)
                    .ThenBy(sc => sc.Name)
                    .ToListAsync(),
                "Id",
                "Name",
                model.SubCategoryId
            );

            return View(model);
        }

        // POST: CategoryAdmin/Delete/5
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int id)
        {
            var admin = await _context.CategoryAdmins.FindAsync(id);
            if (admin == null)
                return NotFound();

            _context.CategoryAdmins.Remove(admin);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Admin đã được xóa" });
        }

        // POST: CategoryAdmin/Deactivate/5
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var admin = await _context.CategoryAdmins.FindAsync(id);
            if (admin == null)
                return NotFound();

            admin.IsActive = false;
            _context.CategoryAdmins.Update(admin);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Admin đã được deactivate" });
        }

        // GET: CategoryAdmin/LockUser
        public async Task<IActionResult> LockUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var adminOf = await _context.CategoryAdmins
                .Where(ca => ca.UserId == userId && ca.IsActive)
                .Select(ca => ca.SubCategoryId)
                .ToListAsync();

            if (adminOf.Count == 0)
                return Forbid();

            // Get users who have posts in admin's categories
            var users = await _context.Posts
                .Where(p => adminOf.Contains(p.SubCategoryId ?? 0))
                .Select(p => p.User)
                .Distinct()
                .OrderBy(u => u.DisplayName)
                .ToListAsync();

            return View(users);
        }

        // POST: CategoryAdmin/LockUserAccount
        [HttpPost]
        [Authorize(Roles = "Administrator,Admin")]
        public async Task<IActionResult> LockUserAccount(string userId, string? reason)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(currentUserId))
                    return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound();

                // Check if current user is Administrator
                var isAdministrator = User.IsInRole("Administrator");

                // If not Administrator, check if target user is an admin
                if (!isAdministrator)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    if (userRoles.Contains("Admin") || userRoles.Contains("Administrator"))
                    {
                        return Forbid("Bạn không có quyền khóa tài khoản của admin");
                    }
                }

                user.IsLocked = true;
                user.LockedReason = reason;
                user.LockedAt = DateTime.UtcNow;
                user.LockedByAdminId = currentUserId;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    return BadRequest("Không thể khóa tài khoản");

                // Create notification for locked user
                var notification = new Notification
                {
                    UserId = userId,
                    Type = NotificationType.UserLocked,
                    Title = "Tài khoản của bạn đã bị khóa",
                    Message = $"Lý do: {reason}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Tài khoản đã được khóa" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi khóa tài khoản: {ex.Message}");
                return BadRequest("Lỗi khi khóa tài khoản");
            }
        }

        // POST: CategoryAdmin/UnlockUserAccount
        [HttpPost]
        [Authorize(Roles = "Administrator,Admin")]
        public async Task<IActionResult> UnlockUserAccount(string userId)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(currentUserId))
                    return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound();

                // Check if current user is Administrator
                var isAdministrator = User.IsInRole("Administrator");

                // If not Administrator, check if target user is an admin
                if (!isAdministrator)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    if (userRoles.Contains("Admin") || userRoles.Contains("Administrator"))
                    {
                        return Forbid("Bạn không có quyền mở khóa tài khoản của admin");
                    }
                }

                user.IsLocked = false;
                user.LockedReason = null;
                user.LockedAt = null;
                user.LockedByAdminId = null;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    return BadRequest("Không thể mở khóa tài khoản");

                // Create notification for unlocked user
                var notification = new Notification
                {
                    UserId = userId,
                    Type = NotificationType.UserLocked,
                    Title = "Tài khoản của bạn đã được mở khóa",
                    Message = "Bạn có thể tiếp tục sử dụng dịch vụ",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Tài khoản đã được mở khóa" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi mở khóa tài khoản: {ex.Message}");
                return BadRequest("Lỗi khi mở khóa tài khoản");
            }
        }

        // GET: CategoryAdmin/LockedUsers
        [Authorize(Roles = "Administrator,Admin")]
        public async Task<IActionResult> LockedUsers()
        {
            var lockedUsers = await _userManager.Users
                .Where(u => u.IsLocked)
                .OrderByDescending(u => u.LockedAt)
                .ToListAsync();

            return View(lockedUsers);
        }

        // GET: CategoryAdmin/ManageUsers
        [Authorize(Roles = "Administrator,Admin")]
        public async Task<IActionResult> ManageUsers()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdministrator = User.IsInRole("Administrator");

            IQueryable<User> usersQuery = _userManager.Users;

            if (!isAdministrator)
            {
                // For category admins, show only regular users in their categories
                // Exclude Administrator and Admin users
                var adminRoleId = (await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator"))?.Id;
                var categoryAdminRoleId = (await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin"))?.Id;

                var adminUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == adminRoleId || ur.RoleId == categoryAdminRoleId)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                // Get category IDs for this admin
                var categoryAdmins = await _context.CategoryAdmins
                    .Where(ca => ca.UserId == currentUserId && ca.IsActive)
                    .Select(ca => ca.SubCategoryId)
                    .ToListAsync();

                usersQuery = usersQuery
                    .Where(u => !adminUserIds.Contains(u.Id)) // Exclude all admins
                    .Where(u => 
                        _context.Posts.Any(p => 
                            _context.SubCategories.Any(sc => sc.Id == p.CategoryId && categoryAdmins.Contains(sc.Id) && p.UserId == u.Id)
                        ) ||
                        _context.Comments.Any(c => 
                            _context.Posts.Any(p => 
                                _context.SubCategories.Any(sc => sc.Id == p.CategoryId && categoryAdmins.Contains(sc.Id) && p.Id == c.PostId && c.UserId == u.Id)
                            )
                        )
                    );
            }

            var users = await usersQuery
                .OrderBy(u => u.DisplayName)
                .ToListAsync();

            return View(users);
        }
    }
}
