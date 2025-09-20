# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Common Development Commands

### Building and Running
```bash
# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run the application (development)
dotnet run

# Run with specific profile
dotnet run --launch-profile https
```

### Database Operations
```bash
# Apply existing migrations
dotnet ef database update

# Create new migration
dotnet ef migrations add <MigrationName>

# Remove last migration
dotnet ef migrations remove

# Generate SQL script from migrations
dotnet ef migrations script
```

### Testing and API Endpoints
The application includes a TestController with API endpoints for backend testing:
```bash
# Test endpoints (when app is running)
curl http://localhost:5215/api/test/status
curl http://localhost:5215/api/test/moods
curl -X POST http://localhost:5215/api/test/test-user
curl -X POST http://localhost:5215/api/test/test-song
curl -X POST http://localhost:5215/api/test/test-playlist
curl http://localhost:5215/api/test/database-info
```

### Publishing
```bash
# Publish the application (self-contained for Windows)
dotnet publish -c Release -r win-x64 --self-contained

# Publish as a framework-dependent app
dotnet publish -c Release
```

## Architecture Overview

### Core Framework
- **ASP.NET Core MVC** (.NET 9.0) with Razor Views
- **Entity Framework Core** with SQLite database
- **Cookie-based authentication** with BCrypt password hashing
- **Bootstrap 5** for responsive UI

### Application Structure

**MVC Pattern with Service Layer:**
- Controllers handle HTTP requests and return views/JSON
- Services contain business logic and data operations
- Models represent domain entities and database schema
- ViewModels handle view-specific data transfer

**Key Services:**
- `AuthService`: User registration, login, password reset
- `SongService`: CRUD operations for songs and mood associations
- `PlaylistService`: Random playlist generation and management

**Database Architecture:**
- User-isolated data (all operations scoped to authenticated user)
- Many-to-many relationships via junction tables (`SongMood`, `PlaylistSong`)
- 6 predefined moods with colors and descriptions (seeded data)
- SQLite database file: `MoodPlaylist.db`

### Domain Models
- **User**: Authentication, songs, and playlists ownership
- **Song**: YouTube URLs with title/artist, linked to multiple moods
- **Mood**: Predefined categories (Happy, Sad, Relaxed, Energetic, Romantic, Focus)
- **Playlist**: Generated collections of songs based on mood selection
- **SongMood/PlaylistSong**: Junction tables for many-to-many relationships

### Key Features
- **Mood-based song tagging**: Songs can have multiple mood associations
- **Random playlist generation**: Shuffles songs by mood with customizable count (1-50)
- **User data isolation**: All operations filtered by authenticated user ID
- **Session management**: 30-day sliding expiration with cookie authentication

### Development Notes
- Uses implicit usings and nullable reference types enabled
- SQLite connection string defaults to `Data Source=MoodPlaylist.db`
- Application URLs: `https://localhost:7065` (HTTPS) or `http://localhost:5215` (HTTP)
- Built for rapid development (3-hour constraint mentioned in README)

### Database Schema Patterns
Junction tables use composite primary keys:
```csharp
// SongMood: Links songs to multiple moods
HasKey(sm => new { sm.SongId, sm.MoodId })

// PlaylistSong: Links playlists to songs with ordering
HasKey(ps => new { ps.PlaylistId, ps.SongId })
```

User scoping enforced in services:
```csharp
// All queries filtered by authenticated user
.Where(entity => entity.UserId == userId)
```

### Important Code Patterns

**Service Layer Pattern:**
All business logic is encapsulated in services rather than controllers:
```csharp
// Controller pattern
public async Task<IActionResult> GeneratePlaylist(int moodId, int count, string name)
{
    var userId = User.GetUserId();
    var playlist = await _playlistService.GeneratePlaylistAsync(userId, moodId, count, name);
    return RedirectToAction("Details", new { id = playlist.Id });
}
```

**View Model Pattern:**
All data transfer between controllers and views uses ViewModels:
```csharp
// Transforming domain models to view models
var viewModel = new SongDetailsViewModel
{
    Song = song,
    SelectedMoodIds = song.SongMoods.Select(sm => sm.MoodId).ToList(),
    AllMoods = await _context.Moods.ToListAsync()
};
```

**Transaction Pattern:**
Entity Framework operations are grouped in transactions when needed:
```csharp
// Using transactions for multi-step operations
using var transaction = await _context.Database.BeginTransactionAsync();
try {
    // Multiple database operations
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch {
    await transaction.RollbackAsync();
    throw;
}
```

This architecture supports the core workflow: Users add YouTube songs → tag with moods → generate randomized playlists by mood selection.
