namespace WebApplication1.Models
{
    public class PostTag
    {
        public int PostId { get; set; }
        public int TagId { get; set; }
        
        // Navigation properties
        public virtual Post Post { get; set; } = null!;
        public virtual Tag Tag { get; set; } = null!;
    }
}
