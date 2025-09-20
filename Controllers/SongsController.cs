using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MoodPlaylistGenerator.Services;
using MoodPlaylistGenerator.ViewModels;

namespace MoodPlaylistGenerator.Controllers
{
    [Authorize]
    public class SongsController : Controller
    {
        private readonly SongService _songService;
        private readonly IMediaUploadService _mediaUploadService;

        public SongsController(SongService songService, IMediaUploadService mediaUploadService)
        {
            _songService = songService;
            _mediaUploadService = mediaUploadService;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }

        public async Task<IActionResult> Index(int? moodId, string? search)
        {
            var userId = GetCurrentUserId();
            var songs = await _songService.GetUserSongsAsync(userId);
            var moods = await _songService.GetAllMoodsAsync();

            // Filter by mood if selected
            if (moodId.HasValue)
            {
                songs = await _songService.GetSongsByMoodAsync(moodId.Value, userId);
            }

            // Filter by search term
            if (!string.IsNullOrEmpty(search))
            {
                songs = songs.Where(s => 
                    s.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.Artist.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var viewModel = new SongListViewModel
            {
                Songs = songs,
                Moods = moods,
                SelectedMoodId = moodId,
                SearchTerm = search ?? ""
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            var song = await _songService.GetSongByIdAsync(id, userId);
            
            if (song == null)
                return NotFound();

            var viewModel = new SongDetailViewModel
            {
                Song = song,
                YouTubeVideoId = _songService.ExtractYouTubeVideoId(song.YouTubeUrl),
                AssignedMoods = song.SongMoods.Select(sm => sm.Mood).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateSongViewModel
            {
                AvailableMoods = await _songService.GetAllMoodsAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateSongViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableMoods = await _songService.GetAllMoodsAsync();
                return View(model);
            }

            var userId = GetCurrentUserId();

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

            try
            {
                await _songService.CreateSongWithMediaAsync(
                    model.Title,
                    model.Artist,
                    userId,
                    model.SelectedMoodIds,
                    model.MediaFile,
                    model.YouTubeUrl,
                    _mediaUploadService);

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

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            var song = await _songService.GetSongByIdAsync(id, userId);
            
            if (song == null)
                return NotFound();

            var viewModel = new EditSongViewModel
            {
                Id = song.Id,
                Title = song.Title,
                Artist = song.Artist,
                YouTubeUrl = song.YouTubeUrl,
                SelectedMoodIds = song.SongMoods.Select(sm => sm.MoodId).ToList(),
                AvailableMoods = await _songService.GetAllMoodsAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditSongViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableMoods = await _songService.GetAllMoodsAsync();
                return View(model);
            }

            var userId = GetCurrentUserId();
            
            try
            {
                var updatedSong = await _songService.UpdateSongAsync(
                    model.Id, 
                    userId, 
                    model.Title, 
                    model.Artist, 
                    model.YouTubeUrl, 
                    model.SelectedMoodIds);

                if (updatedSong == null)
                    return NotFound();

                TempData["SuccessMessage"] = "Song updated successfully!";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while updating the song.");
                model.AvailableMoods = await _songService.GetAllMoodsAsync();
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var success = await _songService.DeleteSongAsync(id, userId);
            
            if (!success)
                return NotFound();

            TempData["SuccessMessage"] = "Song deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
