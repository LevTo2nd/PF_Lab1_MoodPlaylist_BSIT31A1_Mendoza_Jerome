# PF_LAB2: Local Media Upload and Playback Feature
### Programming Fundamentals Laboratory 2 (Continuation of PF_LAB1)

---

## Assignment Overview

**Objective:** Extend your existing MoodPlaylistGenerator application from PF_LAB1 to support local media file uploads and playback functionality. When local media files are not found, the application should gracefully fallback to playing Rick Astley's "Never Gonna Give You Up" from YouTube.

**Duration:** 2-3 weeks  
**Difficulty Level:** Intermediate  
**Prerequisites:** Completion of PF_LAB1 (Basic MoodPlaylistGenerator)

---

## Learning Objectives

By completing this assignment, you will learn:

1. **File Upload Handling** - Working with `IFormFile` and multipart form data
2. **Media File Validation** - Implementing file type and size validation
3. **File System Operations** - Creating directories, saving files, and managing file paths
4. **HTML5 Media Elements** - Using `<audio>` and `<video>` elements for playback
5. **Error Handling & Fallbacks** - Implementing graceful degradation strategies
6. **Database Migrations** - Adding new properties to existing models
7. **Service Layer Architecture** - Creating and injecting custom services
8. **Git Branching** - Working with feature branches in development workflow

---

## Assignment Requirements

### Core Functionality Requirements

#### 1. **Media Upload Support** (40 points)
- [ ] Support uploading audio files (MP3, WAV, FLAC, AAC, OGG)
- [ ] Support uploading video files (MP4, AVI, MOV, MKV, WEBM)
- [ ] Implement file size validation (max 50MB)
- [ ] Implement file type validation using MIME types
- [ ] Store uploaded files in user-specific directories
- [ ] Generate unique filenames to prevent conflicts

#### 2. **Database Schema Updates** (20 points)
- [ ] Add new properties to Song model for local media support
- [ ] Implement `MediaType` enum (YouTube, LocalAudio, LocalVideo)
- [ ] Create and apply Entity Framework migration
- [ ] Maintain backward compatibility with existing YouTube-only songs

#### 3. **Media Playback Interface** (30 points)
- [ ] Implement HTML5 audio player for audio files
- [ ] Implement HTML5 video player for video files
- [ ] Display file information (name, size, type) for uploaded media
- [ ] Maintain existing YouTube video embedding functionality
- [ ] Provide intuitive user interface for both upload options

#### 4. **Rick Roll Fallback System** (10 points)
- [ ] Automatically play Rick Roll when uploaded file is missing/deleted
- [ ] Display appropriate error message when file not found
- [ ] Provide fallback link to Rick Roll video
- [ ] Handle file system errors gracefully

---

## Technical Specifications

### File Structure Requirements

```
Models/
├── Song.cs (Modified)
└── MediaType.cs (New)

Services/
├── IMediaUploadService.cs (New)
├── MediaUploadService.cs (New)
└── SongService.cs (Modified)

Views/Songs/
├── Create.cshtml (Modified)
├── Details.cshtml (Modified)
└── Index.cshtml (Updated for media types)

wwwroot/
└── uploads/
    └── media/
        ├── user_1/
        ├── user_2/
        └── ...
```

### Configuration Requirements

Add to `appsettings.json`:
```json
{
  "MediaUpload": {
    "MaxFileSizeMB": 50,
    "AllowedAudioTypes": [".mp3", ".wav", ".flac", ".aac", ".ogg"],
    "AllowedVideoTypes": [".mp4", ".avi", ".mov", ".mkv", ".webm"],
    "UploadPath": "wwwroot/uploads/media",
    "FallbackYouTubeUrl": "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
  }
}
```

### Database Migration Requirements

New Song model properties:
- `LocalFilePath` (string, nullable)
- `FileName` (string, nullable) 
- `ContentType` (string, nullable)
- `FileSizeBytes` (long, nullable)
- `MediaType` (enum, default: YouTube)

---

## Git Branching Strategy

### **IMPORTANT: Follow this branching workflow exactly**

#### 1. **Create Feature Branch**
```bash
# Start from main/master branch
git checkout main
git pull origin main

# Create and switch to development branch
git checkout -b dev-media
```

#### 2. **Development Workflow**
- Make all changes on the `dev-media` branch
- Commit frequently with descriptive messages
- Use conventional commit format:
  ```bash
  git commit -m "feat: add MediaUploadService interface"
  git commit -m "feat: implement file upload validation"
  git commit -m "feat: add HTML5 media players to details view"
  git commit -m "fix: handle missing file fallback to Rick Roll"
  ```

#### 3. **Final Submission**
```bash
# Push your feature branch
git push origin dev-media

# Create a final commit with all changes
git add .
git commit -m "feat: complete PF_LAB2 media upload and playback functionality

- Add local media file upload support
- Implement HTML5 audio/video players
- Add Rick Roll fallback for missing files
- Update database schema with media properties
- Maintain backward compatibility with YouTube functionality"

git push origin dev-media
```

**⚠️ DO NOT MERGE TO MAIN** - Submit the `dev-media` branch for evaluation

---

## Implementation Guidelines

### Phase 1: Model and Database Updates (Week 1)
1. Update Song model with media properties
2. Create MediaType enum
3. Generate and apply EF migration
4. Test database schema changes

### Phase 2: Service Layer Development (Week 1-2)
1. Create IMediaUploadService interface
2. Implement MediaUploadService class
3. Update SongService with media methods
4. Register services in Program.cs

### Phase 3: UI Implementation (Week 2)
1. Update Create view with file upload
2. Modify Details view with media players
3. Test file upload and validation
4. Implement Rick Roll fallback UI

### Phase 4: Testing and Refinement (Week 2-3)
1. Test all media formats
2. Test file validation edge cases
3. Test Rick Roll fallback scenarios
4. Optimize user experience

---

## Testing Requirements

### Manual Testing Checklist

#### File Upload Tests
- [ ] Upload valid audio file (MP3, WAV, etc.)
- [ ] Upload valid video file (MP4, AVI, etc.)
- [ ] Attempt invalid file type (should be rejected)
- [ ] Attempt oversized file (should be rejected)
- [ ] Upload file with special characters in name

#### Playback Tests
- [ ] Play uploaded audio file in browser
- [ ] Play uploaded video file in browser
- [ ] Verify YouTube videos still work
- [ ] Test on different browsers (Chrome, Firefox, Edge)

#### Fallback Tests
- [ ] Delete uploaded file and verify Rick Roll fallback
- [ ] Create song without any media (should require either file or URL)
- [ ] Test network connectivity issues (optional)

#### Database Tests
- [ ] Verify songs save with correct MediaType
- [ ] Verify file paths are stored correctly
- [ ] Verify existing songs still work after migration

---

## Submission Requirements

### 1. **Code Submission**
- Submit the `dev-media` branch via Git repository
- Include all source code modifications
- Include database migration files
- Include updated configuration files

### 2. **Documentation**
- Complete this assignment file with checkboxes marked
- Add inline code comments for complex logic
- Document any additional features implemented

### 3. **Demo Video** (Optional but Recommended)
- 2-3 minute video showing:
  - File upload process
  - Media playback functionality
  - Rick Roll fallback demonstration
  - Different media types working

---

## Grading Rubric

| Component | Points | Criteria |
|-----------|--------|----------|
| **Media Upload Implementation** | 40 | File validation, storage, error handling |
| **Database Schema** | 20 | Model updates, migrations, data integrity |
| **Media Playback UI** | 30 | HTML5 players, user experience, responsive design |
| **Rick Roll Fallback** | 10 | Proper fallback implementation, error messaging |
| **Code Quality** | 10 | Code organization, comments, best practices |
| **Git Usage** | 10 | Proper branching, commit messages, repository structure |
| **Testing & Documentation** | 10 | Completed testing checklist, clear documentation |
| **Bonus Features** | +5 | Creative enhancements, additional file formats, etc. |

**Total: 130 points (100 + 30 bonus)**

---

## Common Pitfalls to Avoid

❌ **Don't Do This:**
- Merge `dev-media` branch to main before submission
- Skip file validation (security risk)
- Hard-code file paths
- Forget to handle file system errors
- Break existing YouTube functionality

✅ **Best Practices:**
- Use proper exception handling
- Validate files on both client and server side
- Use configuration for file paths and limits
- Test Rick Roll fallback thoroughly
- Maintain clean commit history

---

## Resources and References

### Documentation
- [ASP.NET Core File Upload](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads)
- [HTML5 Media Elements](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/video)
- [Entity Framework Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

### Rick Roll Reference
- **Fallback URL:** `https://www.youtube.com/watch?v=dQw4w9WgXcQ`
- **Song:** "Never Gonna Give You Up" by Rick Astley (1987)

---

## Support and Help

### Getting Help
1. **Review PF_LAB1** - Ensure your foundation is solid
2. **Read Documentation** - Check ASP.NET Core documentation
3. **Debug Step by Step** - Use breakpoints to understand flow
4. **Test Incrementally** - Don't implement everything at once

### Office Hours
- **When:** [To be announced by instructor]
- **Where:** [To be announced by instructor]
- **What to Bring:** Specific error messages, code snippets, questions

---

## Submission Deadline

**Due Date:** [TO BE FILLED BY INSTRUCTOR]  
**Late Submission:** -10 points per day  
**Extension Policy:** Contact instructor 48 hours before deadline

---

## Academic Integrity

This is an **individual assignment**. You may:
- ✅ Discuss general concepts with classmates
- ✅ Use online documentation and tutorials
- ✅ Ask questions during office hours

You may **NOT**:
- ❌ Copy code from classmates
- ❌ Share your complete solution
- ❌ Use AI to write the entire implementation

---

**Good luck with PF_LAB2! 🎵🎬**

*Remember: The goal is to learn file handling, media processing, and error recovery patterns. Take your time to understand each component rather than rushing to completion.*
