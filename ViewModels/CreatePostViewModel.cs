using System.ComponentModel.DataAnnotations;
using WebApplication1.Services;

namespace WebApplication1.ViewModels
{
    public class CreatePostViewModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        
        [Display(Name = "Cụm Danh Mục")]
        public int? MainCategoryId { get; set; }
        
        [Display(Name = "Chuyên Mục")]
        public int? SubCategoryId { get; set; }
        
        [Display(Name = "Tags (comma separated)")]
        public string? Tags { get; set; }
        
        [Display(Name = "Hình ảnh")]
        public IFormFile? Image { get; set; }
        
        [Display(Name = "Video")]
        public IFormFile? Video { get; set; }

        // Thêm thuộc tính để hiển thị gợi ý bài viết tương tự
        public List<SimilarPostResult>? SimilarPosts { get; set; }
        public bool ShowSimilarPosts { get; set; } = false;
    }
}
