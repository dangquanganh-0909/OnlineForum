namespace WebApplication1.Services
{
    public interface IFileUploadService
    {
        Task<string?> UploadImageAsync(IFormFile file);
        Task<string?> UploadVideoAsync(IFormFile file);
        bool DeleteFile(string filePath);
        bool IsValidImageFile(IFormFile file);
        bool IsValidVideoFile(IFormFile file);
    }
}