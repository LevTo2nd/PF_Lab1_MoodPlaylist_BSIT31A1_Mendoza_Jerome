using Microsoft.AspNetCore.Http;
using MoodPlaylistGenerator.Models;

namespace MoodPlaylistGenerator.Services
{
    public class MediaUploadService : IMediaUploadService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        
        public MediaUploadService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public async Task<(string filePath, string fileName, string contentType, long fileSize)> SaveMediaFileAsync(IFormFile file, int userId)
        {
            if (!IsValidMediaFile(file))
                throw new InvalidOperationException("Invalid media file type");

            var uploadPath = _configuration["MediaUpload:UploadPath"] ?? "wwwroot/uploads/media";
            var fullUploadPath = Path.Combine(_environment.ContentRootPath, uploadPath);
            Directory.CreateDirectory(fullUploadPath);
            var userDirectory = Path.Combine(fullUploadPath, $"user_{userId}");
            Directory.CreateDirectory(userDirectory);
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(userDirectory, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var relativePath = Path.Combine(uploadPath, $"user_{userId}", uniqueFileName);
            return (relativePath, uniqueFileName, file.ContentType, file.Length);
        }

        public bool IsValidMediaFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;
            var maxSizeMB = _configuration.GetValue<int>("MediaUpload:MaxFileSizeMB", 50);
            if (file.Length > maxSizeMB * 1024 * 1024)
                return false;
            var allowedAudio = _configuration.GetSection("MediaUpload:AllowedAudioTypes").Get<string[]>() ?? Array.Empty<string>();
            var allowedVideo = _configuration.GetSection("MediaUpload:AllowedVideoTypes").Get<string[]>() ?? Array.Empty<string>();
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedAudio.Contains(fileExtension) || allowedVideo.Contains(fileExtension);
        }

        public bool FileExists(string filePath)
        {
            var fullPath = Path.Combine(_environment.ContentRootPath, filePath);
            return File.Exists(fullPath);
        }

        public void DeleteFile(string filePath)
        {
            var fullPath = Path.Combine(_environment.ContentRootPath, filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        public string GetMediaUrl(string filePath)
        {
            return "/" + filePath.Replace("\\", "/").Replace("wwwroot/", "");
        }

        public string GetFallbackYouTubeUrl()
        {
            return _configuration["MediaUpload:FallbackYouTubeUrl"] ?? "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        }

        public MediaType DetermineMediaType(string contentType)
        {
            if (contentType.StartsWith("audio/"))
                return MediaType.LocalAudio;
            else if (contentType.StartsWith("video/"))
                return MediaType.LocalVideo;
            else
                return MediaType.YouTube;
        }
    }
}
