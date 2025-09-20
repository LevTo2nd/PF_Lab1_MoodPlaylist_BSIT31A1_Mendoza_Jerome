# Step-by-Step Guide: Making MoodPlaylistGenerator Scalable

## Overview of the New Structure
```
MoodPlaylistGenerator/
├── MoodPlaylistGenerator.sln (existing)
├── MoodPlaylistGenerator/              (Main Web MVC Project)
├── MoodPlaylistGenerator.Data/         (Entities + DbContext)
└── MoodPlaylistGenerator.Services/     (Service Interfaces + Implementations)
```

**Two IAuthService Implementations:**
1. **SQLiteAuthService** - Uses Entity Framework with SQLite (Code-First)
2. **InMemoryAuthService** - Uses List<User> for learning/testing

---

## Step 1: Create the Data Project

Create folder `MoodPlaylistGenerator.Data` and create file `MoodPlaylistGenerator.Data.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.9" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.9">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

---

## Step 2: Create the Services Project

Create folder `MoodPlaylistGenerator.Services` and create file `MoodPlaylistGenerator.Services.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MoodPlaylistGenerator.Data\MoodPlaylistGenerator.Data.csproj" />
  </ItemGroup>

</Project>
```

---

## Step 3: Create Folder Structure

In Data project, create folder: `Entities`
In Services project, create folders: `Interfaces` and `Implementations`

---

## Step 4: Copy Models to Data Project

Copy all files from `Models/` folder to `MoodPlaylistGenerator.Data/Entities/` and change namespace in each file from:
```csharp
namespace MoodPlaylistGenerator.Models
```
to:
```csharp
namespace MoodPlaylistGenerator.Data.Entities
```

---

## Step 5: Copy DbContext to Data Project

Copy `Data/ApplicationDbContext.cs` to `MoodPlaylistGenerator.Data/ApplicationDbContext.cs` and update:

```csharp
using Microsoft.EntityFrameworkCore;
using MoodPlaylistGenerator.Data.Entities;

namespace MoodPlaylistGenerator.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<Mood> Moods { get; set; }
        public DbSet<SongMood> SongMoods { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistSong> PlaylistSongs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure SongMood junction table
            modelBuilder.Entity<SongMood>()
                .HasKey(sm => new { sm.SongId, sm.MoodId });
            
            modelBuilder.Entity<SongMood>()
                .HasOne(sm => sm.Song)
                .WithMany(s => s.SongMoods)
                .HasForeignKey(sm => sm.SongId);
            
            modelBuilder.Entity<SongMood>()
                .HasOne(sm => sm.Mood)
                .WithMany(m => m.SongMoods)
                .HasForeignKey(sm => sm.MoodId);

            // Configure PlaylistSong junction table
            modelBuilder.Entity<PlaylistSong>()
                .HasKey(ps => new { ps.PlaylistId, ps.SongId });
            
            modelBuilder.Entity<PlaylistSong>()
                .HasOne(ps => ps.Playlist)
                .WithMany(p => p.PlaylistSongs)
                .HasForeignKey(ps => ps.PlaylistId);
            
            modelBuilder.Entity<PlaylistSong>()
                .HasOne(ps => ps.Song)
                .WithMany(s => s.PlaylistSongs)
                .HasForeignKey(ps => ps.SongId);

            // Seed data for moods
            modelBuilder.Entity<Mood>().HasData(
                new Mood { Id = 1, Name = "Happy", Color = "#FFD700", Description = "Upbeat and energetic songs" },
                new Mood { Id = 2, Name = "Sad", Color = "#4169E1", Description = "Melancholic and emotional songs" },
                new Mood { Id = 3, Name = "Relaxed", Color = "#98FB98", Description = "Calm and soothing songs" },
                new Mood { Id = 4, Name = "Energetic", Color = "#FF6347", Description = "High-energy and motivating songs" },
                new Mood { Id = 5, Name = "Romantic", Color = "#FF69B4", Description = "Love songs and romantic ballads" },
                new Mood { Id = 6, Name = "Focus", Color = "#9370DB", Description = "Music for concentration and work" }
            );
        }
    }
}
```

---

## Step 6: Create Service Interface

Create file `MoodPlaylistGenerator.Services/Interfaces/IAuthService.cs`:

```csharp
using MoodPlaylistGenerator.Data.Entities;

namespace MoodPlaylistGenerator.Services.Interfaces
{
    public interface IAuthService
    {
        Task<bool> InitiatePasswordResetAsync(string email);
        Task<User?> LoginAsync(string emailOrUsername, string password);
        Task<User?> RegisterAsync(string email, string username, string password);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }
}
```

---

## Step 7: Create SQLite Implementation

Create file `MoodPlaylistGenerator.Services/Implementations/SQLiteAuthService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using MoodPlaylistGenerator.Data;
using MoodPlaylistGenerator.Data.Entities;
using MoodPlaylistGenerator.Services.Interfaces;
using BCrypt.Net;

namespace MoodPlaylistGenerator.Services.Implementations
{
    /// <summary>
    /// SQLite implementation of IAuthService using Entity Framework Code-First approach.
    /// This implementation persists user data to a SQLite database.
    /// </summary>
    public class SQLiteAuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public SQLiteAuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> RegisterAsync(string email, string username, string password)
        {
            // Check if user exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email || u.Username == username);

            if (existingUser != null)
                return null;

            // Hash password
            var passwordHash = BCrypt.HashPassword(password);

            var user = new User
            {
                Email = email,
                Username = username,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User?> LoginAsync(string emailOrUsername, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == emailOrUsername || u.Username == emailOrUsername);

            if (user == null || !BCrypt.Verify(password, user.PasswordHash))
                return null;

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> InitiatePasswordResetAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return false;

            // Generate reset token
            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();

            // In a real app, send email here
            Console.WriteLine($"Password reset token for {email}: {user.ResetToken}");
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
                return false;

            user.PasswordHash = BCrypt.HashPassword(newPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
```

---

## Step 8: Create In-Memory Implementation

Create file `MoodPlaylistGenerator.Services/Implementations/InMemoryAuthService.cs`:

```csharp
using MoodPlaylistGenerator.Data.Entities;
using MoodPlaylistGenerator.Services.Interfaces;
using BCrypt.Net;

namespace MoodPlaylistGenerator.Services.Implementations
{
    /// <summary>
    /// In-memory implementation of IAuthService for learning and testing purposes.
    /// This implementation uses a simple List to store users instead of a database.
    /// Perfect for students to understand the service pattern without database complexity.
    /// </summary>
    public class InMemoryAuthService : IAuthService
    {
        private readonly List<User> _users;
        private int _nextUserId = 1;

        public InMemoryAuthService()
        {
            _users = new List<User>();
            
            // Seed with some default users for testing
            // Password for both users is "password123"
            _users.Add(new User
            {
                Id = _nextUserId++,
                Email = "admin@test.com",
                Username = "admin",
                PasswordHash = BCrypt.HashPassword("password123"),
                CreatedAt = DateTime.UtcNow
            });
            
            _users.Add(new User
            {
                Id = _nextUserId++,
                Email = "user@test.com",
                Username = "testuser", 
                PasswordHash = BCrypt.HashPassword("password123"),
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task<User?> RegisterAsync(string email, string username, string password)
        {
            // Simulate async operation
            await Task.Delay(1);
            
            // Check if user already exists
            var existingUser = _users.FirstOrDefault(u => u.Email == email || u.Username == username);
            if (existingUser != null)
                return null; // User already exists

            // Create new user with hashed password
            var user = new User
            {
                Id = _nextUserId++,
                Email = email,
                Username = username,
                PasswordHash = BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            // Add to our in-memory list
            _users.Add(user);
            return user;
        }

        public async Task<User?> LoginAsync(string emailOrUsername, string password)
        {
            // Simulate async operation
            await Task.Delay(1);
            
            // Find user by email or username
            var user = _users.FirstOrDefault(u => 
                u.Email == emailOrUsername || u.Username == emailOrUsername);

            // Verify password
            if (user == null || !BCrypt.Verify(password, user.PasswordHash))
                return null;

            // Update last login time
            user.LastLogin = DateTime.UtcNow;
            return user;
        }

        public async Task<bool> InitiatePasswordResetAsync(string email)
        {
            // Simulate async operation
            await Task.Delay(1);
            
            var user = _users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return false;

            // Generate reset token (in real app, this would be sent via email)
            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            // For demonstration, log the token (in real app, send email)
            Console.WriteLine($"Password reset token for {email}: {user.ResetToken}");
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            // Simulate async operation
            await Task.Delay(1);
            
            // Find user with valid reset token
            var user = _users.FirstOrDefault(u => 
                u.ResetToken == token && 
                u.ResetTokenExpiry != null && 
                u.ResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
                return false;

            // Update password and clear reset token
            user.PasswordHash = BCrypt.HashPassword(newPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            return true;
        }

        /// <summary>
        /// Helper method for debugging - shows all users in memory
        /// </summary>
        public List<User> GetAllUsers() => _users.ToList();

        /// <summary>
        /// Helper method to clear all users (useful for testing)
        /// </summary>
        public void ClearAllUsers() => _users.Clear();
    }
}
```

---

## Step 9: Update Main Project

Edit `MoodPlaylistGenerator.csproj` - remove the old packages and add project references:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\MoodPlaylistGenerator.Data\MoodPlaylistGenerator.Data.csproj" />
    <ProjectReference Include="..\MoodPlaylistGenerator.Services\MoodPlaylistGenerator.Services.csproj" />
  </ItemGroup>

</Project>
```

---

## Step 10: Update Program.cs

Replace your entire `Program.cs` with:

```csharp
using Microsoft.EntityFrameworkCore;
using MoodPlaylistGenerator.Data;
using MoodPlaylistGenerator.Services.Interfaces;
using MoodPlaylistGenerator.Services.Implementations;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add SQLite database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=MoodPlaylist.db"));

// Add services
// OPTION 1: Use SQLite implementation (Code-First with Entity Framework)
builder.Services.AddScoped<IAuthService, SQLiteAuthService>();

// OPTION 2: Use In-Memory implementation (List-based for learning/testing)
// Uncomment the line below and comment out the line above to switch
// builder.Services.AddSingleton<IAuthService, InMemoryAuthService>();

builder.Services.AddScoped<SongService>();
builder.Services.AddScoped<PlaylistService>();

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
```

---

## Step 11: Update Controllers

In all your controllers, update the using statements:

**Remove:**
```csharp
using MoodPlaylistGenerator.Models;
using MoodPlaylistGenerator.Services;
```

**Add:**
```csharp
using MoodPlaylistGenerator.Data.Entities;
using MoodPlaylistGenerator.Services.Interfaces;
```

---

## Step 12: Update ViewModels

In all ViewModel files, change namespace references from:
```csharp
using MoodPlaylistGenerator.Models;
```
to:
```csharp
using MoodPlaylistGenerator.Data.Entities;
```

---

## Step 13: Update Remaining Services

In `SongService.cs` and `PlaylistService.cs`, update using statements:
```csharp
using MoodPlaylistGenerator.Data.Entities;
using MoodPlaylistGenerator.Data;
```

---

## Step 14: Setup Migrations (CLI Commands)

```powershell
cd D:\loa\MoodPlaylistGenerator

# Add initial migration to Data project
dotnet ef migrations add InitialCreate --project MoodPlaylistGenerator.Data --startup-project MoodPlaylistGenerator

# Apply the migration to create/update database
dotnet ef database update --project MoodPlaylistGenerator.Data --startup-project MoodPlaylistGenerator
```

---

## Step 15: Clean Up (After Testing)

Delete these folders from main project:
- `Models/` folder
- `Data/` folder
- `Services/` folder

---

## Testing Both Implementations

### SQLite Implementation (Default):
Use the default Program.cs configuration

### In-Memory Implementation:
Change Program.cs service registration:
```csharp
// Comment out SQLite line
// builder.Services.AddScoped<IAuthService, SQLiteAuthService>();

// Uncomment In-Memory line
builder.Services.AddSingleton<IAuthService, InMemoryAuthService>();
```

Default test credentials for In-Memory:
- Username: `admin`, Password: `password123`
- Username: `testuser`, Password: `password123`

---

## Benefits of This Architecture

1. **Two Clear Learning Paths:**
   - **InMemoryAuthService**: Students learn patterns without database complexity
   - **SQLiteAuthService**: Students learn Entity Framework Code-First approach

2. **Easy Switching:** Change implementation with one line in Program.cs

3. **Separation of Concerns:** Clear boundaries between layers

4. **Testability:** Both implementations are easily testable

5. **Scalability:** Easy to add more implementations (MySQL, PostgreSQL, etc.)

## For Students - Learning Progression

1. **Start with InMemoryAuthService** - Understand service patterns
2. **Study the interface** - Same contract, different implementations  
3. **Switch to SQLiteAuthService** - Learn database integration and Code-First migrations
4. **Compare implementations** - See how the same interface can work with different data sources

This provides the perfect educational progression from simple in-memory operations to full database integration with Entity Framework Code-First approach!

---

## Troubleshooting

### Common Issues:

1. **Build Errors**: Make sure all namespaces are updated correctly
2. **Migration Errors**: Ensure you're running from the correct directory
3. **Service Registration Errors**: Check that all using statements are correct in Program.cs
4. **Database Connection Issues**: Verify the connection string in Program.cs

### Tips:

- Build frequently to catch namespace issues early
- Test with InMemoryAuthService first to verify the architecture works
- Switch to SQLiteAuthService only after confirming everything compiles
- Keep a backup of your original project before starting

---

*This guide creates a scalable, educational architecture perfect for both learning and production use!*
