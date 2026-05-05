using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [AllowAnonymous]
    public class TestController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<TestController> _logger;

        public TestController(UserManager<User> userManager, SignInManager<User> signInManager, ILogger<TestController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TestRegister(string email, string username, string password)
        {
            try
            {
                var user = new User
                {
                    UserName = username,
                    Email = email,
                    DisplayName = username,
                    JoinDate = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Test user created successfully: {Email}", email);
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return Json(new { success = true, message = "Đăng ký thành công!" });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Test registration failed: {Errors}", errors);
                    return Json(new { success = false, message = $"Lỗi đăng ký: {errors}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during test registration");
                return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestLogin(string email, string password)
        {
            try
            {
                // Find user by email first
                var user = await _userManager.FindByEmailAsync(email);
                
                if (user == null)
                {
                    _logger.LogWarning("Test login failed: User not found with email {Email}", email);
                    return Json(new { success = false, message = "Email hoặc mật khẩu không đúng." });
                }

                // Sign in using the username
                var result = await _signInManager.PasswordSignInAsync(user.UserName, password, false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Test login successful: {Email}", email);
                    return Json(new { success = true, message = "Đăng nhập thành công!" });
                }
                else
                {
                    _logger.LogWarning("Test login failed: {Email}", email);
                    return Json(new { success = false, message = "Email hoặc mật khẩu không đúng." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during test login");
                return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
    }
}
