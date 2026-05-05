using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class EditCommentViewModel
    {
        public int Id { get; set; }
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public int PostId { get; set; }
        
        // Media files
        public IFormFile? ImageFile { get; set; }
        public IFormFile? VideoFile { get; set; }
        
        // Existing media (for display/deletion)
        public string? ImagePath { get; set; }
        public string? VideoPath { get; set; }
        public bool DeleteImage { get; set; }
        public bool DeleteVideo { get; set; }
        
        // Formatting
        public string FontFamily { get; set; } = "Arial";
        public string FontColor { get; set; } = "#000000";
    }
}
