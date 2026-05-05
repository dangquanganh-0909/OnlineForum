using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Tag
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    }
}
