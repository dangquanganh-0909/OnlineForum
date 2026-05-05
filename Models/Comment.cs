using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Comment
    {
        public int Id { get; set; }
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        // Media properties
        public string? ImagePath { get; set; }
        public string? VideoPath { get; set; }
        
        // Formatting properties
        public string FontFamily { get; set; } = "Arial";
        public string FontColor { get; set; } = "#000000";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        
        // Foreign keys
        public int PostId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
        
        // Navigation properties
        public virtual Post Post { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual Comment? ParentComment { get; set; }
        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
        public virtual ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
