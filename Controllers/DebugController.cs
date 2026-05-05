using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.AspNetCore.Localization;

namespace WebApplication1.Controllers
{
    [AllowAnonymous]
    public class DebugController : Controller
    {
        private readonly ForumDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<DebugController> _logger;

        public DebugController(ForumDbContext context, UserManager<User> userManager, ILogger<DebugController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var debugInfo = new
            {
                DatabaseExists = await _context.Database.CanConnectAsync(),
                UsersTableExists = await _context.Users.AnyAsync(),
                UserCount = await _context.Users.CountAsync(),
                IdentityOptions = new
                {
                    RequireDigit = _userManager.Options.Password.RequireDigit,
                    RequiredLength = _userManager.Options.Password.RequiredLength,
                    RequireNonAlphanumeric = _userManager.Options.Password.RequireNonAlphanumeric,
                    RequireUppercase = _userManager.Options.Password.RequireUppercase,
                    RequireLowercase = _userManager.Options.Password.RequireLowercase
                }
            };

            ViewBag.DebugInfo = debugInfo;
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult TestRegister()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> CheckUser(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { error = "Email required" });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Json(new { found = false, message = "User not found" });
                }

                var passwordCheck = await _userManager.CheckPasswordAsync(user, "test123");
                
                return Json(new { 
                    found = true, 
                    username = user.UserName,
                    email = user.Email,
                    displayName = user.DisplayName,
                    emailConfirmed = user.EmailConfirmed,
                    isActive = user.IsActive,
                    passwordHashExists = !string.IsNullOrEmpty(user.PasswordHash),
                    passwordCheckTest123 = passwordCheck
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> TestLogin(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return Json(new { error = "Email and password required" });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Json(new { step = "find_user", success = false, message = "User not found" });
                }

                var passwordCheck = await _userManager.CheckPasswordAsync(user, password);
                if (!passwordCheck)
                {
                    return Json(new { step = "password_check", success = false, message = "Password incorrect" });
                }

                return Json(new { 
                    step = "complete",
                    success = true, 
                    username = user.UserName,
                    email = user.Email,
                    message = "Login would succeed"
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        public IActionResult CheckCulture()
        {
            var currentCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var currentUICulture = System.Globalization.CultureInfo.CurrentUICulture.Name;
            var cookies = Request.Cookies.Select(c => new { c.Key, c.Value }).ToList();
            
            return Json(new { 
                currentCulture = currentCulture,
                currentUICulture = currentUICulture,
                cookies = cookies,
                headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            });
        }

        [AllowAnonymous]
        public IActionResult UrlTest()
        {
            var info = new
            {
                RequestUrl = Request.GetDisplayUrl(),
                UrlLength = Request.GetDisplayUrl()?.Length ?? 0,
                Method = Request.Method,
                ContentType = Request.ContentType,
                ContentLength = Request.ContentLength,
                Headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            };
            
            return Json(info);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTestUser()
        {
            try
            {
                var testEmail = "test@example.com";
                var testUsername = "testuser";
                var testPassword = "Test123456";

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(testEmail);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Test user already exists" });
                }

                var user = new User
                {
                    UserName = testUsername,
                    Email = testEmail,
                    DisplayName = testUsername,
                    JoinDate = DateTime.UtcNow,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, testPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Test user created successfully");
                    return Json(new { success = true, message = "Test user created successfully" });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create test user: {Errors}", errors);
                    return Json(new { success = false, message = $"Failed to create user: {errors}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception creating test user");
                return Json(new { success = false, message = $"Exception: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestLogin()
        {
            try
            {
                var testEmail = "test@example.com";
                var testPassword = "Test123456";

                var user = await _userManager.FindByEmailAsync(testEmail);
                if (user == null)
                {
                    return Json(new { success = false, message = "Test user not found" });
                }

                var result = await _userManager.CheckPasswordAsync(user, testPassword);
                return Json(new { success = result, message = result ? "Password correct" : "Password incorrect" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception testing login");
                return Json(new { success = false, message = $"Exception: {ex.Message}" });
            }
        }

        public IActionResult TestLocalization()
        {
            var currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            var currentUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            var requestCulture = Request.HttpContext.Features.Get<IRequestCultureFeature>();
            
            var welcome = "Chào Mừng";
            var forumDiscussion = "Diễn Đàn Thảo Luận";
            
            var cookies = string.Join(", ", Request.Cookies.Select(c => $"{c.Key}={c.Value}"));
            
            return View();
        }

        public IActionResult TestLocalizationJson()
        {
            var currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            var currentUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            var requestCulture = Request.HttpContext.Features.Get<IRequestCultureFeature>();
            
            var welcome = "Chào Mừng";
            var forumDiscussion = "Diễn Đàn Thảo Luận";
            
            var cookies = string.Join(", ", Request.Cookies.Select(c => $"{c.Key}={c.Value}"));
            
            return Json(new {
                CurrentCulture = currentCulture.Name,
                CurrentUICulture = currentUICulture.Name,
                RequestCultureName = requestCulture?.RequestCulture.Culture.Name,
                RequestUICultureName = requestCulture?.RequestCulture.UICulture.Name,
                WelcomeText = welcome,
                ForumDiscussionText = forumDiscussion,
                WelcomeResourceNotFound = false,
                ForumResourceNotFound = false,
                Cookies = cookies
            });
        }

        [HttpGet("/debug/check-category-admins")]
        public async Task<IActionResult> CheckCategoryAdmins()
        {
            try
            {
                var categoryAdmins = await _context.CategoryAdmins
                    .Include(ca => ca.User)
                    .Include(ca => ca.SubCategory)
                    .OrderBy(ca => ca.SubCategory.Name)
                    .ToListAsync();

                var adminsData = categoryAdmins.Select(ca => new
                {
                    ca.Id,
                    Email = ca.User.Email,
                    DisplayName = ca.User.DisplayName,
                    SubCategory = ca.SubCategory.Name,
                    AssignedAt = ca.AssignedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                    IsActive = ca.IsActive
                }).ToList();

                return Ok(new
                {
                    success = true,
                    totalCategoryAdmins = categoryAdmins.Count,
                    categoryAdmins = adminsData,
                    message = categoryAdmins.Count > 0 
                        ? $"✅ Tìm thấy {categoryAdmins.Count} tài khoản admin danh mục" 
                        : "❌ Chưa có tài khoản admin danh mục nào được tạo"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpGet("/debug/check-all-users")]
        public async Task<IActionResult> CheckAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.Email.Contains("admin"))
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.DisplayName,
                        u.UserName,
                        u.IsActive,
                        u.JoinDate
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    totalAdminUsers = users.Count,
                    adminUsers = users,
                    message = users.Count > 0 
                        ? $"✅ Tìm thấy {users.Count} tài khoản admin" 
                        : "❌ Chưa có tài khoản admin nào"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }
    }
}
