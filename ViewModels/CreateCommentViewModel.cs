using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class CreateCommentViewModel
    {
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public int PostId { get; set; }
        public int? ParentCommentId { get; set; }
        
        // Media files
        public IFormFile? ImageFile { get; set; }
        public IFormFile? VideoFile { get; set; }
        
        // Formatting
        public string FontFamily { get; set; } = "Arial";
        public string FontColor { get; set; } = "#000000";
    }
}
