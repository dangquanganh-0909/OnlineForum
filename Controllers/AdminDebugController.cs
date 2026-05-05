using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [AllowAnonymous]
    public class AdminDebugController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AdminDebugController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> CheckAdminStatus()
        {
            var adminUser = await _userManager.FindByEmailAsync("admin@forum.com");
            
            if (adminUser == null)
            {
                return Content("❌ Admin user not found!");
            }

            var roles = await _userManager.GetRolesAsync(adminUser);
            var passwordValid = await _userManager.CheckPasswordAsync(adminUser, "Admin123!");

            var html = $@"
            <h2>✅ Admin Account Status</h2>
            <table border='1' cellpadding='10'>
                <tr><td>Email</td><td>{adminUser.Email}</td></tr>
                <tr><td>UserName</td><td>{adminUser.UserName}</td></tr>
                <tr><td>Display Name</td><td>{adminUser.DisplayName}</td></tr>
                <tr><td>IsLocked</td><td style='color: {(adminUser.IsLocked ? "red" : "green")}'><strong>{adminUser.IsLocked}</strong></td></tr>
                <tr><td>LockedReason</td><td>{adminUser.LockedReason ?? "N/A"}</td></tr>
                <tr><td>IsActive</td><td>{adminUser.IsActive}</td></tr>
                <tr><td>EmailConfirmed</td><td>{adminUser.EmailConfirmed}</td></tr>
                <tr><td>Roles</td><td>{string.Join(", ", roles)}</td></tr>
                <tr><td>Password (Admin123!)</td><td style='color: {(passwordValid ? "green" : "red")}'><strong>{passwordValid}</strong></td></tr>
            </table>
            
            <h3>⚠️ SOLUTION:</h3>";

            if (adminUser.IsLocked)
            {
                html += @"
                <p><strong>Problem:</strong> Admin account is LOCKED (IsLocked = true)</p>
                <p><strong>Solution:</strong> You need to unlock the account by:</p>
                <ol>
                    <li>Using SQL to update: <code>UPDATE AspNetUsers SET IsLocked = 0 WHERE Email = 'admin@forum.com'</code></li>
                    <li>Or restart the database by deleting forum.db file and running the app again</li>
                </ol>";
            }
            else if (!passwordValid)
            {
                html += @"
                <p><strong>Problem:</strong> Password mismatch! Admin123! is incorrect</p>
                <p><strong>Solution:</strong> Recreate the admin user with correct password</p>";
            }
            else if (!adminUser.EmailConfirmed)
            {
                html += @"
                <p><strong>Problem:</strong> Email not confirmed</p>
                <p><strong>Solution:</strong> This shouldn't prevent login, but ensure EmailConfirmed = true</p>";
            }
            else if (!roles.Contains("Administrator"))
            {
                html += @"
                <p><strong>Problem:</strong> Admin user doesn't have Administrator role</p>
                <p><strong>Solution:</strong> Assign Administrator role to user</p>";
            }
            else
            {
                html += @"
                <p style='color: green'><strong>✅ All checks passed!</strong> Admin account should be able to login.</p>";
            }

            return Content(html, "text/html");
        }

        public async Task<IActionResult> UnlockAdmin()
        {
            var adminUser = await _userManager.FindByEmailAsync("admin@forum.com");
            
            if (adminUser == null)
            {
                return Content("❌ Admin user not found!");
            }

            if (adminUser.IsLocked)
            {
                adminUser.IsLocked = false;
                adminUser.LockedReason = null;
                adminUser.LockedAt = null;
                
                var result = await _userManager.UpdateAsync(adminUser);
                
                if (result.Succeeded)
                {
                    return Content("✅ Admin account unlocked successfully! You can now login with email: admin@forum.com, password: Admin123!");
                }
                else
                {
                    return Content("❌ Error: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            
            return Content("ℹ️ Admin account is not locked.");
        }
    }
}
