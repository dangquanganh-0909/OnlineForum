using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    [AllowAnonymous]
    public class WelcomeController : Controller
    {
        public IActionResult Index()
        {
            // If user is already authenticated, redirect to home
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Post");
            }
            
            return View();
        }

        public IActionResult Test()
        {
            return View();
        }

        public IActionResult Register()
        {
            return RedirectToAction("Register", "SimpleAuth");
        }

        public IActionResult Login()
        {
            return RedirectToAction("Login", "SimpleAuth");
        }
    }
}
