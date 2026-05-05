using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ForumDbContext : IdentityDbContext<User>
    {
        public ForumDbContext(DbContextOptions<ForumDbContext> options) : base(options)
        {
        }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<MainCategory> MainCategories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<PostTag> PostTags { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<CommentLike> CommentLikes { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserFollow> UserFollows { get; set; }
        public DbSet<CategoryAdmin> CategoryAdmins { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // ===============================================
            // HỆ THỐNG PHÂN CẤP 3 TẦNG: MainCategory -> SubCategory -> Post
            // ===============================================
            
            // MainCategory configuration
            builder.Entity<MainCategory>(entity =>
            {
                entity.HasKey(mc => mc.Id);
                
                entity.Property(mc => mc.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(mc => mc.Order)
                    .HasDefaultValue(0);
                
                entity.HasIndex(mc => mc.Order)
                    .HasDatabaseName("IX_MainCategory_Order");
            });
            
            // SubCategory configuration và quan hệ với MainCategory (1-nhiều)
            builder.Entity<SubCategory>(entity =>
            {
                entity.HasKey(sc => sc.Id);
                
                entity.Property(sc => sc.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(sc => sc.Description)
                    .HasMaxLength(500);
                
                entity.Property(sc => sc.Icon)
                    .HasMaxLength(200);
                
                // Quan hệ: Một MainCategory có nhiều SubCategory
                entity.HasOne(sc => sc.MainCategory)
                    .WithMany(mc => mc.SubCategories)
                    .HasForeignKey(sc => sc.MainCategoryId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade); // Xóa MainCategory sẽ xóa tất cả SubCategory con
            });
            
            // Post configuration và quan hệ với SubCategory (1-nhiều)
            builder.Entity<Post>(entity =>
            {
                // Quan hệ với SubCategory (Chuyên mục mới - 3 tầng)
                entity.HasOne(p => p.SubCategory)
                    .WithMany(sc => sc.Posts)
                    .HasForeignKey(p => p.SubCategoryId)
                    .OnDelete(DeleteBehavior.SetNull); // Xóa SubCategory sẽ set SubCategoryId = null
                
                // Quan hệ với Category cũ (giữ lại cho tương thích ngược)
                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Posts)
                    .HasForeignKey(p => p.CategoryId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            // ===============================================
            // CÁC QUAN HỆ KHÁC (đã có từ trước)
            // ===============================================
            
            // PostTag many-to-many relationship
            builder.Entity<PostTag>()
                .HasKey(pt => new { pt.PostId, pt.TagId });
                
            builder.Entity<PostTag>()
                .HasOne(pt => pt.Post)
                .WithMany(p => p.PostTags)
                .HasForeignKey(pt => pt.PostId);
                
            builder.Entity<PostTag>()
                .HasOne(pt => pt.Tag)
                .WithMany(t => t.PostTags)
                .HasForeignKey(pt => pt.TagId);
            
            // Comment self-referencing relationship
            builder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Unique constraints for likes
            builder.Entity<PostLike>()
                .HasIndex(pl => new { pl.PostId, pl.UserId })
                .IsUnique();
                
            builder.Entity<CommentLike>()
                .HasIndex(cl => new { cl.CommentId, cl.UserId })
                .IsUnique();
            
            // UserFollow relationship
            builder.Entity<UserFollow>()
                .HasOne(uf => uf.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<UserFollow>()
                .HasOne(uf => uf.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FollowingId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<UserFollow>()
                .HasIndex(uf => new { uf.FollowerId, uf.FollowingId })
                .IsUnique();
            
            // Notification RelatedUser relationship (optional)
            builder.Entity<Notification>()
                .HasOne(n => n.RelatedUser)
                .WithMany()
                .HasForeignKey(n => n.RelatedUserId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // CategoryAdmin relationship
            builder.Entity<CategoryAdmin>()
                .HasOne(ca => ca.User)
                .WithMany(u => u.AdminOf)
                .HasForeignKey(ca => ca.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<CategoryAdmin>()
                .HasOne(ca => ca.SubCategory)
                .WithMany(sc => sc.Admins)
                .HasForeignKey(ca => ca.SubCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<CategoryAdmin>()
                .HasIndex(ca => new { ca.UserId, ca.SubCategoryId })
                .IsUnique();
            
            // Seed default categories
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Công nghệ", Description = "Thảo luận về công nghệ, thiết bị điện tử, AI, blockchain", Color = "#10B981" },
                new Category { Id = 2, Name = "Tài chính", Description = "Đầu tư, kinh doanh, tiền tệ, crypto, chứng khoán", Color = "#059669" },
                new Category { Id = 3, Name = "Đời sống", Description = "Cuộc sống hàng ngày, gia đình, xã hội, văn hóa", Color = "#3B82F6" },
                new Category { Id = 4, Name = "Giải trí", Description = "Phim ảnh, âm nhạc, game, thể thao, sở thích", Color = "#8B5CF6" },
                new Category { Id = 5, Name = "Học tập", Description = "Giáo dục, kỹ năng, khóa học, tài liệu học tập", Color = "#F59E0B" },
                new Category { Id = 6, Name = "Sức khỏe", Description = "Y tế, dinh dưỡng, thể dục, chăm sóc sức khỏe", Color = "#EF4444" },
                new Category { Id = 7, Name = "Du lịch", Description = "Địa điểm du lịch, kinh nghiệm, lịch trình, ẩm thực", Color = "#06B6D4" },
                new Category { Id = 8, Name = "Sáng tạo", Description = "Nghệ thuật, thiết kế, viết lách, nhiếp ảnh, DIY", Color = "#EC4899" }
            );
        }
    }
}
