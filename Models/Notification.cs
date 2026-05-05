using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public enum NotificationType
    {
        PostReply,
        CommentReply,
        PostLike,
        CommentLike,
        Mention,
        Follow,
        PostReport,
        UserLocked
    }
    
    public class Notification
    {
        public int Id { get; set; }
        
        public NotificationType Type { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Message { get; set; }
        
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Foreign keys
        public string UserId { get; set; } = string.Empty;
        public int? PostId { get; set; }
        public int? CommentId { get; set; }
        public string? RelatedUserId { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Post? Post { get; set; }
        public virtual Comment? Comment { get; set; }
        public virtual User? RelatedUser { get; set; }
    }
}
