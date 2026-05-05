using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class PostIndexViewModel
    {
        public IEnumerable<Post> Posts { get; set; } = new List<Post>();
        public IEnumerable<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
        public IEnumerable<MainCategory> MainCategories { get; set; } = new List<MainCategory>();
        public int? CurrentSubCategoryId { get; set; }
        public int? CurrentMainCategoryId { get; set; }
        public string? SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SortBy { get; set; } = "recent";
    }
}
