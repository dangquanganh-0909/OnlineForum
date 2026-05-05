using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public enum ReportStatus
    {
        Pending,
        Reviewed,
        Resolved
    }
    
    public enum ReportReason
    {
        [Display(Name = "Nội dung phản cảm")]
        OffensiveContent,
        
        [Display(Name = "Bạo lực")]
        Violence,
        
        [Display(Name = "Nội dung phản động")]
        SubversiveContent,
        
        [Display(Name = "Vi phạm pháp luật")]
        IllegalContent,
        
        [Display(Name = "Spam")]
        Spam,
        
        [Display(Name = "Qu骚 rối")]
        Harassment,
        
        [Display(Name = "Vi phạm bản quyền")]
        Copyright,
        
        [Display(Name = "Khác")]
        Other
    }
    
    public class Report
    {
        public int Id { get; set; }
        
        public ReportReason Reason { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Foreign keys
        public string UserId { get; set; } = string.Empty;
        public int? PostId { get; set; }
        public int? CommentId { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Post? Post { get; set; }
        public virtual Comment? Comment { get; set; }
    }
}
