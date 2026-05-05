using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Post
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public int Views { get; set; } = 0; // Số lượt xem
        
        // Media fields
        public string? ImagePath { get; set; }
        public string? VideoPath { get; set; }
        
        // AI Features fields
        public bool IsSummarized { get; set; } = false;
        public string? Summary { get; set; }
        public bool IsSpellChecked { get; set; } = false;
        public string? CorrectedContent { get; set; }
        public bool IsDuplicateChecked { get; set; } = false;
        public bool IsPotentialDuplicate { get; set; } = false;
        public int? SimilarPostId { get; set; }
        public double DuplicateSimilarityScore { get; set; } = 0.0;
        
        // Foreign keys
        public string UserId { get; set; } = string.Empty;
        public int CategoryId { get; set; } // Giữ lại cho tương thích ngược
        public int? SubCategoryId { get; set; } // Chuyên mục (cấp 2)
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Category Category { get; set; } = null!; // Giữ lại cho tương thích ngược
        public virtual SubCategory? SubCategory { get; set; } // Chuyên mục mới
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
        public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
