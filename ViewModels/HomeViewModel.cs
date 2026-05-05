namespace WebApplication1.ViewModels
{
    /// <summary>
    /// ViewModel cho trang chủ - hiển thị cây phân cấp MainCategory -> SubCategory
    /// </summary>
    public class HomeViewModel
    {
        public List<MainCategoryViewModel> MainCategories { get; set; } = new List<MainCategoryViewModel>();
    }

    /// <summary>
    /// ViewModel cho MainCategory với danh sách SubCategory
    /// </summary>
    public class MainCategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsActive { get; set; }
        
        public List<SubCategoryViewModel> SubCategories { get; set; } = new List<SubCategoryViewModel>();
    }

    /// <summary>
    /// ViewModel cho SubCategory với thống kê bài viết
    /// </summary>
    public class SubCategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public int MainCategoryId { get; set; }
        
        // Thống kê
        public int PostCount { get; set; }
        
        // Bài viết mới nhất
        public LatestPostViewModel? LatestPost { get; set; }
    }

    /// <summary>
    /// ViewModel cho bài viết mới nhất trong SubCategory
    /// </summary>
    public class LatestPostViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public int Views { get; set; }
    }
}
