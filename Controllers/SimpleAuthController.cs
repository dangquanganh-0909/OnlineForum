using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using System.Security.Claims;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [AllowAnonymous]
    public class SimpleAuthController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<SimpleAuthController> _logger;

        public SimpleAuthController(UserManager<User> userManager, SignInManager<User> signInManager, ILogger<SimpleAuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public IActionResult Login(string? returnUrl = null)
        {
            if (TempData["Success"] != null)
            {
                ViewBag.Success = TempData["Success"];
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null)
        {
            try
            {
                _logger.LogInformation("Simple login attempt for: {Email}", email);

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    ViewBag.Error = "Email và mật khẩu là bắt buộc.";
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }

                // Tìm user bằng email trước
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Login failed - user not found for email: {Email}", email);
                    ViewBag.Error = "Email hoặc mật khẩu không đúng.";
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }

                _logger.LogInformation("Found user: {UserName} for email: {Email}", user.UserName, email);

                // Kiểm tra password trước
                var passwordCheck = await _userManager.CheckPasswordAsync(user, password);
                _logger.LogInformation("Password check result for {Email}: {Result}", email, passwordCheck);

                if (!passwordCheck)
                {
                    _logger.LogWarning("Password check failed for: {Email}", email);
                    ViewBag.Error = "Email hoặc mật khẩu không đúng.";
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }

                // Kiểm tra tài khoản có bị khóa không
                if (user.IsLocked)
                {
                    _logger.LogWarning("Login failed - account is locked for: {Email}", email);
                    ViewBag.Error = $"Tài khoản của bạn đã bị khóa. Lý do: {user.LockedReason ?? "Không rõ"}. Vui lòng liên hệ quản trị viên.";
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }

                // Đăng nhập trực tiếp bằng SignInAsync thay vì PasswordSignInAsync
                await _signInManager.SignInAsync(user, rememberMe);
                _logger.LogInformation("Simple login successful for: {Email}", email);
                
                // Chuyển hướng về returnUrl nếu có, nếu không thì về trang chủ
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Post");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during simple login for: {Email}", email);
                ViewBag.Error = "Đã xảy ra lỗi trong quá trình đăng nhập.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string username, string password, string confirmPassword)
        {
            try
            {
                _logger.LogInformation("Simple registration attempt for: {Email}, URL Length: {Length}", 
                    email, Request.GetDisplayUrl()?.Length ?? 0);

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ViewBag.Error = "Tất cả các trường là bắt buộc.";
                    return View();
                }

                if (password != confirmPassword)
                {
                    ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                    return View();
                }

                // Kiểm tra email đã tồn tại
                var existingUserByEmail = await _userManager.FindByEmailAsync(email);
                if (existingUserByEmail != null)
                {
                    ViewBag.Error = "Email đã tồn tại.";
                    return View();
                }

                // Kiểm tra username đã tồn tại  
                var existingUserByName = await _userManager.FindByNameAsync(username);
                if (existingUserByName != null)
                {
                    ViewBag.Error = "Tên người dùng đã tồn tại.";
                    return View();
                }

                var user = new User
                {
                    UserName = username,
                    Email = email,
                    DisplayName = username,
                    JoinDate = DateTime.UtcNow,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Simple registration successful for: {Email}", email);
                    // Không tự động đăng nhập, chuyển về trang login
                    TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login", "SimpleAuth");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Simple registration failed for: {Email}. Errors: {Errors}", email, errors);
                    ViewBag.Error = $"Đăng ký thất bại: {errors}";
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during simple registration for: {Email}", email);
                ViewBag.Error = "Đã xảy ra lỗi trong quá trình đăng ký.";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                _logger.LogInformation("User logout attempt");
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User logout successful");
                return RedirectToAction("Index", "Welcome");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return RedirectToAction("Index", "Welcome");
            }
        }

        [AllowAnonymous]
        public IActionResult LogoutGet()
        {
            // GET version cho logout để tránh lỗi 414
            return View("LogoutConfirm");
        }

        // Google OAuth handlers
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            try
            {
                var redirectUrl = Url.Action(nameof(GoogleResponse), "SimpleAuth", new { returnUrl });
                var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
                return new ChallengeResult("Google", properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Google login");
                ViewBag.Error = "Tính năng đăng nhập Google chưa được cấu hình. Vui lòng liên hệ quản trị viên.";
                ViewBag.ReturnUrl = returnUrl;
                return View("Login");
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                _logger.LogError("Google login error: {RemoteError}", remoteError);
                ViewBag.Error = $"Lỗi từ Google: {remoteError}";
                return View("Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("Unable to load Google login information");
                ViewBag.Error = "Không thể tải thông tin đăng nhập từ Google.";
                return RedirectToAction(nameof(Login));
            }

            // Sign in with this external provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }

            // If the user does not have an account, then create an account and sign in with the external provider
            var email = info.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
            var name = info.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? string.Empty;

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Google login: Email claim not found");
                ViewBag.Error = "Không thể lấy email từ tài khoản Google của bạn.";
                return RedirectToAction(nameof(Login));
            }

            // Check if user already exists
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Create new user from Google login
                user = new User
                {
                    UserName = email,
                    Email = email,
                    DisplayName = !string.IsNullOrEmpty(name) ? name.Split(' ')[0] : email.Split('@')[0],
                    EmailConfirmed = true,
                    IsActive = true,
                    JoinDate = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Error creating user from Google login: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    ViewBag.Error = "Không thể tạo tài khoản. Vui lòng thử lại.";
                    return RedirectToAction(nameof(Login));
                }

                // Add to Users role by default
                await _userManager.AddToRoleAsync(user, "Users");
            }

            // Link the external login to the user
            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                _logger.LogWarning("Could not add login for provider {LoginProvider}", info.LoginProvider);
            }

            // Sign in the user
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User created and logged in with {Name} provider.", info.LoginProvider);

            return LocalRedirect(returnUrl);
        }

        public IActionResult Lockout()
        {
            return View();
        }
    }
}
