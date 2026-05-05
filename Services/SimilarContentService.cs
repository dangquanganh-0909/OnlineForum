using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class SimilarContentService : ISimilarContentService
    {
        private readonly ForumDbContext _context;

        public SimilarContentService(ForumDbContext context)
        {
            _context = context;
        }

        public async Task<List<SimilarPostResult>> FindSimilarPostsAsync(
            string title, 
            string content, 
            int? excludePostId = null, 
            int limit = 3, 
            double minSimilarityScore = 0.7)
        {
            // Kết hợp tiêu đề và nội dung để so sánh
            var inputText = $"{title} {content}".ToLower();
            
            // Lấy tất cả bài viết không bị xóa
            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.SubCategory)
                .Include(p => p.Comments)
                .Include(p => p.PostLikes)
                .Where(p => !p.IsDeleted && p.Id != excludePostId)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    p.CreatedAt,
                    p.Views,
                    AuthorName = p.User.DisplayName ?? p.User.UserName,
                    SubCategoryName = p.SubCategory != null ? p.SubCategory.Name : null,
                    CommentsCount = p.Comments.Count,
                    LikesCount = p.PostLikes.Count
                })
                .ToListAsync();

            var similarPosts = new List<SimilarPostResult>();

            foreach (var post in posts)
            {
                var postText = $"{post.Title} {post.Content}".ToLower();
                var similarity = CalculateSimilarity(inputText, postText);

                if (similarity >= minSimilarityScore)
                {
                    similarPosts.Add(new SimilarPostResult
                    {
                        PostId = post.Id,
                        Title = post.Title,
                        Content = TruncateContent(post.Content, 150),
                        SimilarityScore = similarity,
                        CreatedAt = post.CreatedAt,
                        AuthorName = post.AuthorName ?? "Anonymous",
                        SubCategoryName = post.SubCategoryName,
                        Views = post.Views,
                        CommentsCount = post.CommentsCount,
                        LikesCount = post.LikesCount
                    });
                }
            }

            return similarPosts
                .OrderByDescending(x => x.SimilarityScore)
                .Take(limit)
                .ToList();
        }

        public double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0;

            // Chuẩn hóa văn bản
            var normalizedText1 = NormalizeText(text1);
            var normalizedText2 = NormalizeText(text2);

            // Sử dụng kết hợp nhiều phương pháp để tính độ tương tự
            var jaccardSimilarity = CalculateJaccardSimilarity(normalizedText1, normalizedText2);
            var cosineSimilarity = CalculateCosineSimilarity(normalizedText1, normalizedText2);
            var levenshteinSimilarity = CalculateLevenshteinSimilarity(normalizedText1, normalizedText2);

            // Trọng số kết hợp
            return (jaccardSimilarity * 0.3) + (cosineSimilarity * 0.5) + (levenshteinSimilarity * 0.2);
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Chuyển về chữ thường
            text = text.ToLower();

            // Loại bỏ HTML tags
            text = Regex.Replace(text, @"<[^>]+>", "");

            // Loại bỏ dấu câu và ký tự đặc biệt
            text = Regex.Replace(text, @"[^\w\sÀ-ỹ]", " ");

            // Loại bỏ khoảng trắng thừa
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }

        private double CalculateJaccardSimilarity(string text1, string text2)
        {
            var words1 = text1.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var words2 = text2.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            if (words1.Count == 0 && words2.Count == 0)
                return 1.0;

            if (words1.Count == 0 || words2.Count == 0)
                return 0.0;

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return (double)intersection / union;
        }

        private double CalculateCosineSimilarity(string text1, string text2)
        {
            var words1 = text1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var words2 = text2.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var allWords = words1.Concat(words2).Distinct().ToArray();
            
            if (allWords.Length == 0)
                return 1.0;

            var vector1 = new double[allWords.Length];
            var vector2 = new double[allWords.Length];

            for (int i = 0; i < allWords.Length; i++)
            {
                vector1[i] = words1.Count(w => w == allWords[i]);
                vector2[i] = words2.Count(w => w == allWords[i]);
            }

            var dotProduct = vector1.Zip(vector2, (a, b) => a * b).Sum();
            var magnitude1 = Math.Sqrt(vector1.Sum(x => x * x));
            var magnitude2 = Math.Sqrt(vector2.Sum(x => x * x));

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0.0;

            return dotProduct / (magnitude1 * magnitude2);
        }

        private double CalculateLevenshteinSimilarity(string text1, string text2)
        {
            var maxLength = Math.Max(text1.Length, text2.Length);
            if (maxLength == 0)
                return 1.0;

            var distance = CalculateLevenshteinDistance(text1, text2);
            return 1.0 - ((double)distance / maxLength);
        }

        private int CalculateLevenshteinDistance(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1))
                return text2?.Length ?? 0;

            if (string.IsNullOrEmpty(text2))
                return text1.Length;

            var distance = new int[text1.Length + 1, text2.Length + 1];

            for (int i = 0; i <= text1.Length; i++)
                distance[i, 0] = i;

            for (int j = 0; j <= text2.Length; j++)
                distance[0, j] = j;

            for (int i = 1; i <= text1.Length; i++)
            {
                for (int j = 1; j <= text2.Length; j++)
                {
                    var cost = text1[i - 1] == text2[j - 1] ? 0 : 1;
                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost
                    );
                }
            }

            return distance[text1.Length, text2.Length];
        }

        private string TruncateContent(string content, int maxLength)
        {
            if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
                return content ?? string.Empty;

            return content.Substring(0, maxLength) + "...";
        }
    }
}