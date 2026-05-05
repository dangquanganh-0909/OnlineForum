using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    /// <summary>
    /// Cụm danh mục chính - Cấp cao nhất trong hệ thống phân loại
    /// </summary>
    public class MainCategory
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Tên cụm danh mục không được để trống")]
        [StringLength(100, ErrorMessage = "Tên cụm danh mục không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;
        
        [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị phải lớn hơn hoặc bằng 0")]
        public int Order { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        /// <summary>
        /// Danh sách các chuyên mục con thuộc cụm danh mục này
        /// </summary>
        public virtual ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    }
}
