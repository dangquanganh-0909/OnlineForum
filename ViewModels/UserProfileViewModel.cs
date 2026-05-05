using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class UserProfileViewModel
    {
        public User User { get; set; } = null!;
        public List<Post> RecentPosts { get; set; } = new List<Post>();
        public List<Comment> RecentComments { get; set; } = new List<Comment>();
        public int TotalPosts { get; set; }
        public int TotalComments { get; set; }
        public int TotalLikes { get; set; }
        
        // Follow-related properties
        public List<User>? Followers { get; set; }
        public List<User>? Following { get; set; }
        public bool IsFollowing { get; set; }
    }
}
