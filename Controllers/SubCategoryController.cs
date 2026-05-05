using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    public class SubCategoryController : Controller
    {
        /// <summary>
        /// Redirect old SubCategory/Details/{id} routes to Home with subCategoryId parameter
        /// This maintains backward compatibility for any existing bookmarks or links
        /// </summary>
        public IActionResult Details(int id)
        {
            return RedirectToAction("Index", "Home", new { subCategoryId = id });
        }

        /// <summary>
        /// Redirect any other SubCategory routes to Home
        /// </summary>
        public IActionResult Index(int? id)
        {
            if (id.HasValue)
            {
                return RedirectToAction("Index", "Home", new { subCategoryId = id });
            }
            return RedirectToAction("Index", "Home");
        }
    }
}