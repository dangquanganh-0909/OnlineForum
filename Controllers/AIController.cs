using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AIController : ControllerBase
    {
        private readonly IAIContentService _aiService;
        private readonly ForumDbContext _context;
        private readonly ILogger<AIController> _logger;

        public AIController(
            IAIContentService aiService,
            ForumDbContext context,
            ILogger<AIController> logger)
        {
            _aiService = aiService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Summarize the content of a post
        /// </summary>
        [HttpPost("summarize")]
        public async Task<IActionResult> Summarize([FromBody] SummarizeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Content))
                    return BadRequest(new { message = "Content is required" });

                var summary = await _aiService.SummarizeContentAsync(request.Content);
                
                return Ok(new { summary });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Summarize: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while summarizing" });
            }
        }

        /// <summary>
        /// Check and correct spelling/grammar in content
        /// </summary>
        [HttpPost("spellcheck")]
        public async Task<IActionResult> SpellCheck([FromBody] SpellCheckRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Content))
                    return BadRequest(new { message = "Content is required" });

                var corrected = await _aiService.CheckAndCorrectSpellingAsync(request.Content);
                
                return Ok(new { corrected });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SpellCheck: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while checking spelling" });
            }
        }

        /// <summary>
        /// Check for duplicate posts
        /// </summary>
        [HttpPost("check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromBody] CheckDuplicateRequest request)
        {
            try
            {
                if (request.PostId <= 0)
                    return BadRequest(new { message = "PostId is required" });

                var post = await _context.Posts.FindAsync(request.PostId);
                if (post == null)
                    return NotFound(new { message = "Post not found" });

                var allPosts = _context.Posts.Where(p => !p.IsDeleted).ToList();
                
                var (isDuplicate, similarPostId, similarityScore) = 
                    await _aiService.CheckForDuplicateAsync(post, allPosts);

                // Update the post with duplicate check results
                post.IsDuplicateChecked = true;
                post.IsPotentialDuplicate = isDuplicate;
                post.SimilarPostId = similarPostId;
                post.DuplicateSimilarityScore = similarityScore;

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    isDuplicate, 
                    similarPostId, 
                    similarityScore = Math.Round(similarityScore * 100, 1) 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CheckDuplicate: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while checking for duplicates" });
            }
        }

        /// <summary>
        /// Process a post with all AI features
        /// </summary>
        [HttpPost("process-post/{id}")]
        public async Task<IActionResult> ProcessPost(int id)
        {
            try
            {
                var post = await _context.Posts.FindAsync(id);
                if (post == null)
                    return NotFound(new { message = "Post not found" });

                // Check if user is the author or has admin rights
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (post.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                // Summarize content
                post.Summary = await _aiService.SummarizeContentAsync(post.Content);
                post.IsSummarized = true;

                // Check and correct spelling
                post.CorrectedContent = await _aiService.CheckAndCorrectSpellingAsync(post.Content);
                post.IsSpellChecked = true;

                // Check for duplicates
                var allPosts = _context.Posts.Where(p => !p.IsDeleted && p.Id != id).ToList();
                var (isDuplicate, similarPostId, similarityScore) = 
                    await _aiService.CheckForDuplicateAsync(post, allPosts);

                post.IsDuplicateChecked = true;
                post.IsPotentialDuplicate = isDuplicate;
                post.SimilarPostId = similarPostId;
                post.DuplicateSimilarityScore = similarityScore;

                post.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "Post processed successfully",
                    summary = post.Summary,
                    corrected = post.CorrectedContent,
                    isDuplicate = isDuplicate,
                    similarPostId = similarPostId,
                    similarityScore = Math.Round(similarityScore * 100, 1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ProcessPost: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while processing the post" });
            }
        }
    }

    // Request DTOs
    public class SummarizeRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class SpellCheckRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class CheckDuplicateRequest
    {
        public int PostId { get; set; }
    }
}
