namespace WebApplication1.Models
{
    public class PostLike
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Post Post { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
