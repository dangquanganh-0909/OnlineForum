using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    [AllowAnonymous]
    public class AuthTestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult DirectRegister()
        {
            return Redirect("/Identity/Account/Register");
        }

        public IActionResult DirectLogin()
        {
            return Redirect("/Identity/Account/Login");
        }
    }
}
