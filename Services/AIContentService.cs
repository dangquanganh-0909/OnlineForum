using WebApplication1.Models;
using System.Text.RegularExpressions;

namespace WebApplication1.Services
{
    /// <summary>
    /// Service for AI-powered content processing
    /// Implements summarization, spell checking, and duplicate detection
    /// </summary>
    public class AIContentService : IAIContentService
    {
        private readonly ILogger<AIContentService> _logger;

        public AIContentService(ILogger<AIContentService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Summarize post content by extracting key sentences
        /// </summary>
        public Task<string> SummarizeContentAsync(string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    return Task.FromResult(string.Empty);

                // Remove HTML tags if any
                var plainText = Regex.Replace(content, "<[^>]*>", "");
                
                // Split into sentences
                var sentences = Regex.Split(plainText, @"(?<=[.!?])\s+")
                    .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 10)
                    .ToList();

                if (sentences.Count == 0)
                    return Task.FromResult(plainText.Length > 200 ? plainText.Substring(0, 200) + "..." : plainText);

                // Select key sentences (first and last, plus some from middle)
                var summary = new List<string>();
                
                if (sentences.Count >= 1)
                    summary.Add(sentences[0]); // First sentence
                
                if (sentences.Count > 2)
                {
                    // Add one from middle
                    var middleIndex = sentences.Count / 2;
                    summary.Add(sentences[middleIndex]);
                }
                
                if (sentences.Count > 1)
                    summary.Add(sentences[sentences.Count - 1]); // Last sentence

                var result = string.Join(" ", summary);
                
                // Limit summary length
                if (result.Length > 500)
                    result = result.Substring(0, 500) + "...";

                _logger.LogInformation($"Content summarized: {content.Length} chars -> {result.Length} chars");
                
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error summarizing content: {ex.Message}");
                return Task.FromResult(string.Empty);
            }
        }

        /// <summary>
        /// Check and correct spelling/grammar errors (basic implementation)
        /// </summary>
        public Task<string> CheckAndCorrectSpellingAsync(string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    return Task.FromResult(string.Empty);

                // This is a simplified implementation
                // In production, you would use a real spelling/grammar API like:
                // - Azure Cognitive Services
                // - Bing Spell Check API
                // - Google Cloud Natural Language
                
                var corrected = content;

                // Basic common Vietnamese spelling corrections
                var corrections = new Dictionary<string, string>
                {
                    { @"\bthế\s+nào\b", "thế nào" },
                    { @"\btại\s+sao\b", "tại sao" },
                    { @"\bvì\s+sao\b", "vì sao" },
                    { @"\bnhư\s+thế\s+nào\b", "như thế nào" },
                    { @"\b([a-z])\1{2,}\b", "$1" } // Remove repeated characters
                };

                foreach (var (pattern, replacement) in corrections)
                {
                    corrected = Regex.Replace(corrected, pattern, replacement, RegexOptions.IgnoreCase);
                }

                _logger.LogInformation("Spell check completed");
                
                return Task.FromResult(corrected);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking spelling: {ex.Message}");
                return Task.FromResult(content);
            }
        }

        /// <summary>
        /// Check for duplicate posts using similarity analysis
        /// </summary>
        public Task<(bool isDuplicate, int? similarPostId, double similarityScore)> CheckForDuplicateAsync(Post post, IEnumerable<Post> existingPosts)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(post.Content) || !existingPosts.Any())
                    return Task.FromResult<(bool, int?, double)>((false, null, 0.0));

                double maxSimilarity = 0.0;
                int? mostSimilarPostId = null;
                const double DUPLICATE_THRESHOLD = 0.75; // 75% similarity = potential duplicate

                foreach (var existingPost in existingPosts.Where(p => p.Id != post.Id && !p.IsDeleted))
                {
                    double similarity = CalculateSimilarityScore(post.Content, existingPost.Content);
                    
                    if (similarity > maxSimilarity)
                    {
                        maxSimilarity = similarity;
                        mostSimilarPostId = existingPost.Id;
                    }
                }

                bool isDuplicate = maxSimilarity >= DUPLICATE_THRESHOLD;
                
                _logger.LogInformation($"Duplicate check completed. Max similarity: {maxSimilarity:P}, Is duplicate: {isDuplicate}");
                
                return Task.FromResult<(bool, int?, double)>((isDuplicate, mostSimilarPostId, maxSimilarity));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking for duplicates: {ex.Message}");
                return Task.FromResult<(bool, int?, double)>((false, null, 0.0));
            }
        }

        /// <summary>
        /// Calculate similarity score between two texts using basic algorithm
        /// Returns a value between 0 and 1
        /// </summary>
        public double CalculateSimilarityScore(string text1, string text2)
        {
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                return 0.0;

            // Normalize texts
            var normalized1 = NormalizeText(text1);
            var normalized2 = NormalizeText(text2);

            // Calculate using word overlap
            var words1 = new HashSet<string>(normalized1.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            var words2 = new HashSet<string>(normalized2.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            if (words1.Count == 0 || words2.Count == 0)
                return 0.0;

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            // Jaccard similarity
            double jaccardSimilarity = (double)intersection / union;

            // Also consider length similarity
            int minLength = Math.Min(normalized1.Length, normalized2.Length);
            int maxLength = Math.Max(normalized1.Length, normalized2.Length);
            double lengthSimilarity = (double)minLength / maxLength;

            // Weighted average (70% word similarity, 30% length similarity)
            double finalScore = (jaccardSimilarity * 0.7) + (lengthSimilarity * 0.3);

            return Math.Min(finalScore, 1.0); // Ensure score is between 0 and 1
        }

        private string NormalizeText(string text)
        {
            // Remove HTML tags
            text = Regex.Replace(text, "<[^>]*>", "");
            
            // Convert to lowercase
            text = text.ToLower();
            
            // Remove extra whitespace
            text = Regex.Replace(text, @"\s+", " ");
            
            // Remove punctuation
            text = Regex.Replace(text, @"[^\w\s]", "");
            
            return text.Trim();
        }
    }
}
