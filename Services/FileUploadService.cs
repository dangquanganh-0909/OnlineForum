namespace WebApplication1.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;
        
        // Kích thước file tối đa (10MB cho ảnh, 100MB cho video)
        private const long MaxImageSize = 10 * 1024 * 1024; // 10MB
        private const long MaxVideoSize = 100 * 1024 * 1024; // 100MB
        
        // Các định dạng file được phép
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly string[] _allowedVideoExtensions = { ".mp4", ".webm", ".ogg", ".mov", ".avi" };

        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string?> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            if (!IsValidImageFile(file))
                return null;

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "images");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/uploads/images/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image file");
                return null;
            }
        }

        public async Task<string?> UploadVideoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            if (!IsValidVideoFile(file))
                return null;

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "videos");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/uploads/videos/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading video file");
                return null;
            }
        }

        public bool DeleteFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > MaxImageSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedImageExtensions.Contains(extension);
        }

        public bool IsValidVideoFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > MaxVideoSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedVideoExtensions.Contains(extension);
        }
    }
}