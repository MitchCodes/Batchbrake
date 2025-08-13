using Batchbrake.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Batchbrake.ViewModels;
using System.Linq;

namespace Batchbrake.Services
{
    /// <summary>
    /// Manages automatic session persistence to local filesystem
    /// </summary>
    public class SessionManager
    {
        private readonly string _sessionFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public SessionManager()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Batchbrake"
            );
            
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _sessionFilePath = Path.Combine(appDataPath, "session.json");
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Automatically saves the current session state
        /// </summary>
        /// <param name="viewModel">The main window view model containing current state</param>
        public async Task SaveSessionAsync(MainWindowViewModel viewModel)
        {
            try
            {
                var sessionData = CreateSessionDataFromViewModel(viewModel);
                var json = JsonSerializer.Serialize(sessionData, _jsonOptions);
                await File.WriteAllTextAsync(_sessionFilePath, json);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - session saving should not disrupt user experience
                System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the saved session if it exists
        /// </summary>
        /// <returns>The loaded session data, or null if no session exists or loading fails</returns>
        public async Task<SessionData?> LoadSessionAsync()
        {
            try
            {
                if (!File.Exists(_sessionFilePath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(_sessionFilePath);
                return JsonSerializer.Deserialize<SessionData>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                // Log error but return null - failed session loading should not prevent app startup
                System.Diagnostics.Debug.WriteLine($"Failed to load session: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Applies loaded session data to the view model
        /// </summary>
        /// <param name="sessionData">The session data to apply</param>
        /// <param name="viewModel">The view model to update</param>
        public async Task ApplySessionToViewModelAsync(SessionData sessionData, MainWindowViewModel viewModel)
        {
            if (sessionData == null) return;

            // Apply settings
            viewModel.DefaultPreset = sessionData.DefaultPreset;
            viewModel.DefaultOutputPath = sessionData.DefaultOutputPath;
            viewModel.DefaultOutputFormat = sessionData.DefaultOutputFormat;
            viewModel.ParallelInstances = sessionData.ParallelInstances;
            viewModel.DeleteSourceAfterConversion = sessionData.DeleteSourceAfterConversion;

            // Load videos (only those that aren't in progress)
            foreach (var videoData in sessionData.Videos.Where(v => v.ConversionStatus != VideoConversionStatus.InProgress))
            {
                // Verify the input file still exists
                if (File.Exists(videoData.InputFilePath))
                {
                    var videoViewModel = new VideoModelViewModel
                    {
                        InputFilePath = videoData.InputFilePath,
                        OutputFilePath = videoData.OutputFilePath,
                        Preset = videoData.Preset,
                        ConversionStatus = videoData.ConversionStatus,
                        VideoInfo = videoData.VideoInfo,
                        ErrorMessage = videoData.ErrorMessage,
                        StartTime = videoData.StartTime,
                        EndTime = videoData.EndTime,
                        Presets = viewModel.Presets
                    };

                    // Restore clips if they exist
                    if (videoData.Clips != null && videoData.Clips.Count > 0)
                    {
                        videoViewModel.Clips = new System.Collections.ObjectModel.ObservableCollection<ClipModel>(videoData.Clips);
                    }

                    viewModel.VideoQueue.Add(videoViewModel);
                }
            }
        }

        /// <summary>
        /// Creates session data from the current view model state
        /// </summary>
        private SessionData CreateSessionDataFromViewModel(MainWindowViewModel viewModel)
        {
            var sessionData = new SessionData
            {
                DefaultPreset = viewModel.DefaultPreset,
                DefaultOutputPath = viewModel.DefaultOutputPath,
                DefaultOutputFormat = viewModel.DefaultOutputFormat,
                ParallelInstances = viewModel.ParallelInstances,
                DeleteSourceAfterConversion = viewModel.DeleteSourceAfterConversion,
                LastSaved = DateTime.Now
            };

            // Only save videos that are not currently in progress
            foreach (var video in viewModel.VideoQueue.Where(v => v.ConversionStatus != VideoConversionStatus.InProgress))
            {
                sessionData.Videos.Add(new VideoSessionData
                {
                    InputFilePath = video.InputFilePath!,
                    OutputFilePath = video.OutputFilePath!,
                    Preset = video.Preset,
                    ConversionStatus = video.ConversionStatus,
                    VideoInfo = video.VideoInfo,
                    ErrorMessage = video.ErrorMessage,
                    StartTime = video.StartTime,
                    EndTime = video.EndTime,
                    Clips = video.Clips?.ToList() // Convert ObservableCollection to List for serialization
                });
            }

            return sessionData;
        }

        /// <summary>
        /// Deletes the saved session file
        /// </summary>
        public void ClearSession()
        {
            try
            {
                if (File.Exists(_sessionFilePath))
                {
                    File.Delete(_sessionFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear session: {ex.Message}");
            }
        }
    }
}