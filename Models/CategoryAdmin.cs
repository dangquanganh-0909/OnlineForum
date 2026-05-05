using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    /// <summary>
    /// Quản trị viên danh mục - quản lý người dùng và bài viết trong danh mục
    /// </summary>
    public class CategoryAdmin
    {
        public int Id { get; set; }

        // Foreign Keys
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int SubCategoryId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual SubCategory SubCategory { get; set; } = null!;
    }
}
