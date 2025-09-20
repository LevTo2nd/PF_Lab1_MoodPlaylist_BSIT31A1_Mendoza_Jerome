# Media Upload and Playback Feature - Implementation Instructions

## Overview
This guide provides step-by-step instructions to update the MoodPlaylistGenerator application to support uploading and playing local audio/video files instead of just YouTube links. If a local file is not found, the application will fall back to playing Rick Astley's "Never Gonna Give You Up" from YouTube.

## Prerequisites
- Visual Studio or Visual Studio Code
- .NET 7.0+ SDK
- Git installed and configured

## Step 1: Create Development Branch

```bash
# Navigate to your project directory
cd D:\loa\MoodPlaylistGenerator

# Create and switch to the new development branch
git checkout -b dev-media

# Verify you're on the correct branch
git branch
```

## Step 2: Update the Song Model

### 2.1 Modify the Song.cs Model
**File:** `Models/Song.cs`

Add new properties to handle both local files and YouTube URLs:

```csharp
using System.ComponentModel.DataAnnotations;

namespace MoodPlaylistGenerator.Models
{
    public class Song
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Artist { get; set; } = string.Empty;
        
        // Keep YouTube URL for backwards compatibility and fallback
        [Url]
        public string? YouTubeUrl { get; set; }
        
        // New properties for local media
        public string? LocalFilePath { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long? FileSizeBytes { get; set; }
        
        // Media type enum
        public MediaType MediaType { get; set; } = MediaType.YouTube;
        
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public User User { get; set; } = null!;
        public List<SongMood> SongMoods { get; set; } = new();
        public List<PlaylistSong> PlaylistSongs { get; set; } = new();
    }
    
    public enum MediaType
    {
        YouTube = 0,
        LocalAudio = 1,
        LocalVideo = 2
    }
}
```

## Step 3: Create Database Migration

### 3.1 Generate Migration
Run the following commands in Package Manager Console or terminal:

```bash
# Add migration for new media fields
dotnet ef migrations add AddLocalMediaSupport

# Update database
dotnet ef database update
```

## Step 4: Update Application Configuration

### 4.1 Update appsettings.json
Add configuration for file uploads:

```json
{
  "ConnectionStrings": {
    // ... existing connection strings
  },
  "MediaUpload": {
    "MaxFileSizeMB": 50,
    "AllowedAudioTypes": [".mp3", ".wav", ".flac", ".aac", ".ogg"],
    "AllowedVideoTypes": [".mp4", ".avi", ".mov", ".mkv", ".webm"],
    "UploadPath": "wwwroot/uploads/media",
    "FallbackYouTubeUrl": "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
  }
}
```

### 4.2 Create Media Upload Service
**Create File:** `Services/IMediaUploadService.cs`

```csharp
namespace MoodPlaylistGenerator.Services
{
    public interface IMediaUploadService
    {
        Task<(string filePath, string fileName, string contentType, long fileSize)> SaveMediaFileAsync(IFormFile file, int userId);
        bool IsValidMediaFile(IFormFile file);
        bool FileExists(string filePath);
        void DeleteFile(string filePath);
        string GetMediaUrl(string filePath);
        string GetFallbackYouTubeUrl();
        MediaType DetermineMediaType(string contentType);
    }
}
```

**Create File:** `Services/MediaUploadService.cs`

```csharp
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
            
            // Create directory if it doesn't exist
            Directory.CreateDirectory(fullUploadPath);
            
            // Create user-specific subdirectory
            var userDirectory = Path.Combine(fullUploadPath, $"user_{userId}");
            Directory.CreateDirectory(userDirectory);
            
            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(userDirectory, uniqueFileName);
            
            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            // Return relative path for database storage
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
```

## Step 5: Update Song Service

### 5.1 Modify SongService.cs
Add new methods and update existing ones:

```csharp
// Add these methods to your existing SongService class

public async Task<Song> CreateSongWithMediaAsync(string title, string artist, int userId, List<int> moodIds, IFormFile? mediaFile = null, string? youtubeUrl = null)
{
    var song = new Song
    {
        Title = title,
        Artist = artist,
        UserId = userId,
        CreatedAt = DateTime.UtcNow
    };
    
    // Handle media file upload
    if (mediaFile != null && mediaFile.Length > 0)
    {
        var mediaUploadService = // Inject this service
        var (filePath, fileName, contentType, fileSize) = await mediaUploadService.SaveMediaFileAsync(mediaFile, userId);
        
        song.LocalFilePath = filePath;
        song.FileName = fileName;
        song.ContentType = contentType;
        song.FileSizeBytes = fileSize;
        song.MediaType = mediaUploadService.DetermineMediaType(contentType);
    }
    else if (!string.IsNullOrEmpty(youtubeUrl))
    {
        song.YouTubeUrl = youtubeUrl;
        song.MediaType = MediaType.YouTube;
    }
    
    _context.Songs.Add(song);
    await _context.SaveChangesAsync();
    
    // Add mood associations
    if (moodIds.Any())
    {
        foreach (var moodId in moodIds)
        {
            _context.SongMoods.Add(new SongMood
            {
                SongId = song.Id,
                MoodId = moodId
            });
        }
        await _context.SaveChangesAsync();
    }
    
    return await GetSongByIdAsync(song.Id, userId) ?? song;
}

public string GetPlayableUrl(Song song, IMediaUploadService mediaUploadService)
{
    // Check if it's a local media file
    if (song.MediaType != MediaType.YouTube && !string.IsNullOrEmpty(song.LocalFilePath))
    {
        // Check if file exists
        if (mediaUploadService.FileExists(song.LocalFilePath))
        {
            return mediaUploadService.GetMediaUrl(song.LocalFilePath);
        }
    }
    
    // Fallback to YouTube URL or Rick Roll
    if (!string.IsNullOrEmpty(song.YouTubeUrl))
    {
        return song.YouTubeUrl;
    }
    
    return mediaUploadService.GetFallbackYouTubeUrl();
}
```

## Step 6: Update View Models

### 6.1 Create/Update View Models
**Create File:** `ViewModels/CreateSongViewModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace MoodPlaylistGenerator.ViewModels
{
    public class CreateSongViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Artist is required")]
        public string Artist { get; set; } = string.Empty;
        
        [Display(Name = "Upload Media File")]
        public IFormFile? MediaFile { get; set; }
        
        [Display(Name = "YouTube URL (optional)")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? YouTubeUrl { get; set; }
        
        [Display(Name = "Select Moods")]
        public List<int> SelectedMoodIds { get; set; } = new();
        
        public List<Mood> AvailableMoods { get; set; } = new();
    }
}
```

## Step 7: Update Controllers

### 7.1 Update SongsController.cs
Modify the Create and Edit actions:

```csharp
// Add constructor parameter for MediaUploadService
private readonly IMediaUploadService _mediaUploadService;

public SongsController(SongService songService, IMediaUploadService mediaUploadService)
{
    _songService = songService;
    _mediaUploadService = mediaUploadService;
}

[HttpPost]
public async Task<IActionResult> Create(CreateSongViewModel model)
{
    if (!ModelState.IsValid)
    {
        model.AvailableMoods = await _songService.GetAllMoodsAsync();
        return View(model);
    }
    
    // Validate that either media file or YouTube URL is provided
    if (model.MediaFile == null && string.IsNullOrEmpty(model.YouTubeUrl))
    {
        ModelState.AddModelError("", "Please either upload a media file or provide a YouTube URL.");
        model.AvailableMoods = await _songService.GetAllMoodsAsync();
        return View(model);
    }
    
    // Validate media file if provided
    if (model.MediaFile != null && !_mediaUploadService.IsValidMediaFile(model.MediaFile))
    {
        ModelState.AddModelError("MediaFile", "Invalid media file. Please upload a valid audio or video file.");
        model.AvailableMoods = await _songService.GetAllMoodsAsync();
        return View(model);
    }
    
    var userId = GetCurrentUserId();
    
    try
    {
        await _songService.CreateSongWithMediaAsync(
            model.Title, 
            model.Artist, 
            userId, 
            model.SelectedMoodIds, 
            model.MediaFile, 
            model.YouTubeUrl);
        
        TempData["SuccessMessage"] = "Song added successfully!";
        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        ModelState.AddModelError("", "An error occurred while adding the song: " + ex.Message);
        model.AvailableMoods = await _songService.GetAllMoodsAsync();
        return View(model);
    }
}
```

## Step 8: Update Views

### 8.1 Update Create.cshtml
**File:** `Views/Songs/Create.cshtml`

```html
@model MoodPlaylistGenerator.ViewModels.CreateSongViewModel
@{
    ViewData["Title"] = "Add New Song";
}

<div class="row justify-content-center">
    <div class="col-md-8">
        <div class="card">
            <div class="card-header">
                <h3>🎵 Add New Song</h3>
            </div>
            <div class="card-body">
                <form asp-action="Create" method="post" enctype="multipart/form-data">
                    <div asp-validation-summary="All" class="alert alert-danger" role="alert"></div>
                    
                    <div class="mb-3">
                        <label asp-for="Title" class="form-label">Song Title *</label>
                        <input asp-for="Title" class="form-control" placeholder="Enter song title">
                        <span asp-validation-for="Title" class="text-danger"></span>
                    </div>
                    
                    <div class="mb-3">
                        <label asp-for="Artist" class="form-label">Artist *</label>
                        <input asp-for="Artist" class="form-control" placeholder="Enter artist name">
                        <span asp-validation-for="Artist" class="text-danger"></span>
                    </div>
                    
                    <!-- Media Upload Section -->
                    <div class="card mb-3">
                        <div class="card-header">
                            <h5>Choose Media Source</h5>
                        </div>
                        <div class="card-body">
                            <div class="row">
                                <div class="col-md-6">
                                    <h6>🎵 Upload Local Media File</h6>
                                    <div class="mb-3">
                                        <label asp-for="MediaFile" class="form-label">Select Audio/Video File</label>
                                        <input asp-for="MediaFile" class="form-control" type="file" 
                                               accept="audio/*,video/*" id="mediaFileInput">
                                        <span asp-validation-for="MediaFile" class="text-danger"></span>
                                        <div class="form-text">
                                            Supported formats: MP3, WAV, FLAC, AAC, OGG, MP4, AVI, MOV, MKV, WEBM<br>
                                            Maximum file size: 50MB
                                        </div>
                                    </div>
                                </div>
                                
                                <div class="col-md-6">
                                    <h6>🎬 YouTube URL (Alternative)</h6>
                                    <div class="mb-3">
                                        <label asp-for="YouTubeUrl" class="form-label">YouTube URL</label>
                                        <input asp-for="YouTubeUrl" class="form-control" placeholder="https://www.youtube.com/watch?v=...">
                                        <span asp-validation-for="YouTubeUrl" class="text-danger"></span>
                                        <div class="form-text">
                                            If no local file is uploaded, this URL will be used
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="mb-3">
                        <label class="form-label">Moods</label>
                        <div class="row">
                            @foreach (var mood in Model.AvailableMoods)
                            {
                                <div class="col-md-4 mb-2">
                                    <div class="form-check">
                                        <input class="form-check-input" type="checkbox" 
                                               name="SelectedMoodIds" value="@mood.Id" 
                                               id="mood_@mood.Id">
                                        <label class="form-check-label" for="mood_@mood.Id">
                                            <span class="badge rounded-pill" style="background-color: @mood.Color; color: white;">
                                                @mood.Name
                                            </span>
                                        </label>
                                    </div>
                                </div>
                            }
                        </div>
                    </div>
                    
                    <div class="d-flex justify-content-between">
                        <a asp-action="Index" class="btn btn-secondary">
                            <i class="fas fa-arrow-left"></i> Back to Songs
                        </a>
                        <button type="submit" class="btn btn-primary">
                            <i class="fas fa-plus"></i> Add Song
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <script>
        // JavaScript to handle file selection feedback
        document.getElementById('mediaFileInput').addEventListener('change', function(e) {
            if (e.target.files.length > 0) {
                const file = e.target.files[0];
                const fileName = file.name;
                const fileSize = (file.size / (1024 * 1024)).toFixed(2);
                console.log(`Selected file: ${fileName} (${fileSize} MB)`);
            }
        });
    </script>
}
```

### 8.2 Update Details.cshtml
**File:** `Views/Songs/Details.cshtml`

Add media player section:

```html
<!-- Replace the existing YouTube embed section with this -->
<div class="col-md-4">
    @if (Model.Song.MediaType == MediaType.LocalAudio || Model.Song.MediaType == MediaType.LocalVideo)
    {
        <div class="card">
            <div class="card-header">
                <h5>🎵 Media Player</h5>
            </div>
            <div class="card-body p-0">
                @{
                    var mediaUrl = ViewBag.MediaUrl as string ?? "";
                    var fallbackUrl = ViewBag.FallbackUrl as string ?? "";
                }
                
                @if (Model.Song.MediaType == MediaType.LocalAudio)
                {
                    <audio controls class="w-100" style="height: 40px;">
                        <source src="@mediaUrl" type="@Model.Song.ContentType">
                        <p>Your browser doesn't support HTML5 audio. 
                           <a href="@fallbackUrl" target="_blank">Play Rick Roll instead!</a>
                        </p>
                    </audio>
                }
                else if (Model.Song.MediaType == MediaType.LocalVideo)
                {
                    <video controls class="w-100" style="max-height: 300px;">
                        <source src="@mediaUrl" type="@Model.Song.ContentType">
                        <p>Your browser doesn't support HTML5 video. 
                           <a href="@fallbackUrl" target="_blank">Play Rick Roll instead!</a>
                        </p>
                    </video>
                }
                
                @if (!ViewBag.FileExists)
                {
                    <div class="alert alert-warning m-2">
                        <strong>File not found!</strong> The original media file is missing.
                        <br>
                        <a href="@fallbackUrl" target="_blank" class="btn btn-sm btn-outline-primary mt-2">
                            🎬 Play Rick Roll instead
                        </a>
                    </div>
                }
            </div>
        </div>
    }
    else if (!string.IsNullOrEmpty(Model.YouTubeVideoId))
    {
        <!-- Keep existing YouTube embed code -->
        <div class="card">
            <div class="card-header">
                <h5>🎬 Preview</h5>
            </div>
            <div class="card-body p-0">
                <div class="ratio ratio-16x9">
                    <iframe src="https://www.youtube.com/embed/@Model.YouTubeVideoId" 
                            title="YouTube video player" 
                            frameborder="0" 
                            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" 
                            allowfullscreen>
                    </iframe>
                </div>
            </div>
        </div>
    }
    
    <!-- File Information -->
    @if (Model.Song.MediaType != MediaType.YouTube)
    {
        <div class="card mt-3">
            <div class="card-header">
                <h5>📁 File Information</h5>
            </div>
            <div class="card-body">
                <p><strong>File Name:</strong> @Model.Song.FileName</p>
                <p><strong>Type:</strong> @Model.Song.ContentType</p>
                <p><strong>Size:</strong> @((Model.Song.FileSizeBytes ?? 0) / (1024.0 * 1024.0):F2) MB</p>
                <p><strong>Media Type:</strong> @Model.Song.MediaType</p>
            </div>
        </div>
    }
</div>
```

## Step 9: Register Services in Program.cs

### 9.1 Update Program.cs
Add the new service registration:

```csharp
// Add this line where other services are registered
builder.Services.AddScoped<IMediaUploadService, MediaUploadService>();
```

## Step 10: Create Upload Directory Structure

### 10.1 Create Directories
Create the following directory structure in your project:

```
wwwroot/
└── uploads/
    └── media/
        └── user_1/
        └── user_2/
        └── (etc.)
```

You can create this programmatically or manually create the initial directories.

## Step 11: Update .gitignore (Important!)

### 11.1 Add Upload Directory to .gitignore
Add this to your `.gitignore` file:

```
# Uploaded media files
wwwroot/uploads/
```

## Step 12: Testing Instructions

### 12.1 Test the Implementation

1. **Build and Run the Application**
   ```bash
   dotnet build
   dotnet run
   ```

2. **Test Media Upload**
   - Navigate to the "Add New Song" page
   - Try uploading different audio/video formats
   - Verify file size limits are enforced
   - Test that files are properly saved and playable

3. **Test Fallback Functionality**
   - Upload a song with a media file
   - Manually delete the file from the upload directory
   - Verify that accessing the song details shows the Rick Roll fallback

4. **Test YouTube Fallback**
   - Create a song without uploading a file (YouTube URL only)
   - Verify YouTube embedding still works

## Step 13: Security Considerations

### 13.1 Important Security Notes
- **File Validation**: The current implementation validates file extensions and MIME types
- **File Size Limits**: Enforced to prevent abuse
- **User Isolation**: Files are stored in user-specific directories
- **Path Security**: All file paths are properly sanitized

### 13.2 Additional Security Recommendations
- Consider adding virus scanning for uploaded files
- Implement rate limiting for uploads
- Add user storage quotas
- Consider serving uploaded files through a separate domain/subdomain

## Step 14: Commit Your Changes

### 14.1 Commit to dev-media Branch

```bash
# Stage all changes
git add .

# Commit changes
git commit -m "Add local media upload and playback functionality

- Add MediaType enum and new fields to Song model
- Create MediaUploadService for file handling
- Update SongService to support local media files
- Modify views to support file uploads and media playback
- Add fallback to Rick Roll when files are missing
- Implement file validation and security measures"

# Push the branch
git push origin dev-media
```

## Step 15: Next Steps

1. **Test Thoroughly**: Test all scenarios including edge cases
2. **Performance Testing**: Test with large files and multiple concurrent uploads
3. **User Experience**: Consider adding progress bars for file uploads
4. **Mobile Support**: Test media playback on mobile devices
5. **Backup Strategy**: Implement backup for uploaded media files

## Troubleshooting

### Common Issues

1. **Files Not Playing**: Check file permissions and MIME type configuration
2. **Upload Failures**: Verify directory permissions and file size limits
3. **Missing Rick Roll**: Check the fallback URL in configuration
4. **Database Errors**: Ensure migration was applied successfully

### Support Commands

```bash
# Reset database if needed
dotnet ef database drop
dotnet ef database update

# Check current branch
git status
git branch

# View uploaded files
dir wwwroot\uploads\media /s
```

## Conclusion

This implementation provides a robust media upload and playback system that:
- Supports multiple audio and video formats
- Provides graceful fallback to Rick Roll when files are missing
- Maintains backward compatibility with existing YouTube functionality
- Includes proper security measures and file validation
- Organizes files by user for better management

The `dev-media` branch contains all the necessary changes to transform your YouTube-only playlist generator into a comprehensive local media management system.
