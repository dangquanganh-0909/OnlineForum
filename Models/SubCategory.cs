using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    /// <summary>
    /// Chuyên mục - Cấp 2 trong hệ thống phân loại, thuộc về MainCategory
    /// </summary>
    public class SubCategory
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Tên chuyên mục không được để trống")]
        [StringLength(100, ErrorMessage = "Tên chuyên mục không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }
        
        [StringLength(200, ErrorMessage = "Icon không được vượt quá 200 ký tự")]
        public string? Icon { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        // Foreign Key
        [Required(ErrorMessage = "Cụm danh mục chính không được để trống")]
        public int MainCategoryId { get; set; }
        
        // Navigation properties
        /// <summary>
        /// Cụm danh mục chính mà chuyên mục này thuộc về
        /// </summary>
        public virtual MainCategory MainCategory { get; set; } = null!;
        
        /// <summary>
        /// Danh sách các bài viết thuộc chuyên mục này
        /// </summary>
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        
        /// <summary>
        /// Danh sách các quản trị viên của chuyên mục này
        /// </summary>
        public virtual ICollection<CategoryAdmin> Admins { get; set; } = new List<CategoryAdmin>();
    }
}
