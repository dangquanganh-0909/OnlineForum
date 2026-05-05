using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class EditProfileViewModel
    {
        [Required]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;
        
        [Display(Name = "Bio")]
        [StringLength(500)]
        public string? Bio { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
