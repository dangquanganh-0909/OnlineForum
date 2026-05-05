using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface IAIContentService
    {
        /// <summary>
        /// Summarize the content of a post
        /// </summary>
        Task<string> SummarizeContentAsync(string content);

        /// <summary>
        /// Check and correct spelling/grammar errors in content
        /// </summary>
        Task<string> CheckAndCorrectSpellingAsync(string content);

        /// <summary>
        /// Check for duplicate posts based on content similarity
        /// </summary>
        Task<(bool isDuplicate, int? similarPostId, double similarityScore)> CheckForDuplicateAsync(Post post, IEnumerable<Post> existingPosts);

        /// <summary>
        /// Calculate similarity score between two texts
        /// </summary>
        double CalculateSimilarityScore(string text1, string text2);
    }
}
