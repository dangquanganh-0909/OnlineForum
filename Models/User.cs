using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public class User : IdentityUser
    {
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool IsLocked { get; set; } = false; // Trạng thái khóa tài khoản
        public string? LockedReason { get; set; } // Lý do khóa
        public DateTime? LockedAt { get; set; } // Thời gian khóa
        public string? LockedByAdminId { get; set; } // Admin khóa tài khoản
        
        // Navigation properties
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
        public virtual ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<CategoryAdmin> AdminOf { get; set; } = new List<CategoryAdmin>();
        
        // Follow relationships
        public virtual ICollection<UserFollow> Followers { get; set; } = new List<UserFollow>(); // Những người follow
        public virtual ICollection<UserFollow> Following { get; set; } = new List<UserFollow>(); // Những người được follow
    }
}
