using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface ISimilarContentService
    {
        /// <summary>
        /// Tìm kiếm các bài viết tương tự dựa trên tiêu đề và nội dung
        /// </summary>
        /// <param name="title">Tiêu đề bài viết</param>
        /// <param name="content">Nội dung bài viết</param>
        /// <param name="excludePostId">ID bài viết cần loại trừ (khi edit)</param>
        /// <param name="limit">Số lượng bài viết trả về tối đa</param>
        /// <param name="minSimilarityScore">Điểm tương tự tối thiểu (0-1)</param>
        /// <returns>Danh sách bài viết tương tự kèm điểm số</returns>
        Task<List<SimilarPostResult>> FindSimilarPostsAsync(
            string title, 
            string content, 
            int? excludePostId = null, 
            int limit = 3, 
            double minSimilarityScore = 0.7);

        /// <summary>
        /// Tính toán độ tương tự giữa hai văn bản
        /// </summary>
        /// <param name="text1">Văn bản thứ nhất</param>
        /// <param name="text2">Văn bản thứ hai</param>
        /// <returns>Điểm tương tự từ 0 đến 1</returns>
        double CalculateSimilarity(string text1, string text2);
    }

    public class SimilarPostResult
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string? SubCategoryName { get; set; }
        public int Views { get; set; }
        public int CommentsCount { get; set; }
        public int LikesCount { get; set; }
    }
}