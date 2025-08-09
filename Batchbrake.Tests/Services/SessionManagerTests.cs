using Batchbrake.Models;
using Batchbrake.Services;
using Batchbrake.ViewModels;
using Moq;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace Batchbrake.Tests.Services
{
    public class SessionManagerTests : IDisposable
    {
        private readonly TestSessionManager _sessionManager;
        private readonly string _testSessionPath;

        public SessionManagerTests()
        {
            // Create a unique test session file for each test instance
            var testId = Guid.NewGuid().ToString("N")[..8];
            var tempPath = Path.GetTempPath();
            _testSessionPath = Path.Combine(tempPath, $"test-session-{testId}.json");
            
            _sessionManager = new TestSessionManager(_testSessionPath);
            
            // Clean up any existing session file before each test
            if (File.Exists(_testSessionPath))
            {
                File.Delete(_testSessionPath);
            }
        }

        public void Dispose()
        {
            // Clean up test session file
            try
            {
                if (File.Exists(_testSessionPath))
                {
                    File.Delete(_testSessionPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Fact]
        public async Task SaveSessionAsync_CreatesSessionFile()
        {
            // Arrange
            var mockFilePickerService = new Mock<IFilePickerService>();
            var viewModel = new MainWindowViewModel(mockFilePickerService.Object);
            
            // Add a test video
            await viewModel.AddNewFile(@"C:\test\video.mp4");

            // Act
            await _sessionManager.SaveSessionAsync(viewModel);

            // Assert
            Assert.True(File.Exists(_testSessionPath));
        }

        [Fact]
        public async Task LoadSessionAsync_ReturnsNullWhenNoSessionExists()
        {
            // Arrange
            if (File.Exists(_testSessionPath))
            {
                File.Delete(_testSessionPath);
            }

            // Act
            var result = await _sessionManager.LoadSessionAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SaveAndLoadSession_PreservesVideoData()
        {
            // Arrange
            var mockFilePickerService = new Mock<IFilePickerService>();
            var viewModel = new MainWindowViewModel(mockFilePickerService.Object);
            
            // Wait for async initialization to complete
            await Task.Delay(200);
            
            // Add test videos with different statuses
            await viewModel.AddNewFile(@"C:\test\video1.mp4");
            await viewModel.AddNewFile(@"C:\test\video2.mkv");
            
            // Simulate one video being completed
            viewModel.VideoQueue[0].ConversionStatus = VideoConversionStatus.Completed;
            viewModel.VideoQueue[0].ConversionProgress = 100;
            
            // Set properties after initialization is complete
            viewModel.DefaultPreset = "Fast 1080p30";
            viewModel.ParallelInstances = 2;

            // Act - Save session
            await _sessionManager.SaveSessionAsync(viewModel);
            
            // Act - Load session
            var sessionData = await _sessionManager.LoadSessionAsync();

            // Assert
            Assert.NotNull(sessionData);
            Assert.Equal(2, sessionData.Videos.Count);
            Assert.Equal("Fast 1080p30", sessionData.DefaultPreset);
            Assert.Equal(2, sessionData.ParallelInstances);
            
            // Check video data is preserved
            var completedVideo = sessionData.Videos.FirstOrDefault(v => v.ConversionStatus == VideoConversionStatus.Completed);
            Assert.NotNull(completedVideo);
            Assert.Equal(@"C:\test\video1.mp4", completedVideo.InputFilePath);
        }

        [Fact]
        public async Task SaveSession_ExcludesVideosInProgress()
        {
            // Arrange
            var mockFilePickerService = new Mock<IFilePickerService>();
            var viewModel = new MainWindowViewModel(mockFilePickerService.Object);
            
            // Wait for async initialization to complete
            await Task.Delay(200);
            
            await viewModel.AddNewFile(@"C:\test\video1.mp4");
            await viewModel.AddNewFile(@"C:\test\video2.mkv");
            
            // Mark one video as in progress
            viewModel.VideoQueue[0].ConversionStatus = VideoConversionStatus.InProgress;
            viewModel.VideoQueue[1].ConversionStatus = VideoConversionStatus.Completed;

            // Act
            await _sessionManager.SaveSessionAsync(viewModel);
            var sessionData = await _sessionManager.LoadSessionAsync();

            // Assert
            Assert.NotNull(sessionData);
            Assert.Single(sessionData.Videos); // Only the completed video should be saved
            Assert.Equal(VideoConversionStatus.Completed, sessionData.Videos[0].ConversionStatus);
        }

        [Fact]
        public async Task ApplySessionToViewModelAsync_RestoresVideoQueue()
        {
            // Arrange
            var sessionData = new SessionData
            {
                DefaultPreset = "Super HQ 1080p30 Surround",
                ParallelInstances = 3,
                DeleteSourceAfterConversion = true,
                Videos = new List<VideoSessionData>
                {
                    new VideoSessionData
                    {
                        InputFilePath = @"C:\test\video1.mp4",
                        OutputFilePath = @"C:\test\video1_conv.mp4",
                        ConversionStatus = VideoConversionStatus.Completed,
                        Preset = "Fast 1080p30"
                    }
                }
            };

            var mockFilePickerService = new Mock<IFilePickerService>();
            var viewModel = new MainWindowViewModel(mockFilePickerService.Object);

            // Wait for initialization and clear any existing queue
            await Task.Delay(200); // Give time for initialization
            viewModel.VideoQueue.Clear();

            // Create a fake file for the test
            Directory.CreateDirectory(@"C:\test");
            await File.WriteAllTextAsync(@"C:\test\video1.mp4", "fake video content");

            try
            {
                // Act
                await _sessionManager.ApplySessionToViewModelAsync(sessionData, viewModel);

                // Assert
                Assert.Equal("Super HQ 1080p30 Surround", viewModel.DefaultPreset);
                Assert.Equal(3, viewModel.ParallelInstances);
                Assert.True(viewModel.DeleteSourceAfterConversion);
                Assert.Single(viewModel.VideoQueue);
                Assert.Equal(@"C:\test\video1.mp4", viewModel.VideoQueue[0].InputFilePath);
                Assert.Equal(VideoConversionStatus.Completed, viewModel.VideoQueue[0].ConversionStatus);
            }
            finally
            {
                // Cleanup
                if (File.Exists(@"C:\test\video1.mp4"))
                    File.Delete(@"C:\test\video1.mp4");
                if (Directory.Exists(@"C:\test"))
                    Directory.Delete(@"C:\test");
            }
        }
    }

    /// <summary>
    /// Test-specific SessionManager that uses a custom session file path
    /// </summary>
    public class TestSessionManager : SessionManager
    {
        private readonly string _testSessionFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public TestSessionManager(string sessionFilePath)
        {
            _testSessionFilePath = sessionFilePath;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public new async Task SaveSessionAsync(MainWindowViewModel viewModel)
        {
            try
            {
                var sessionData = CreateSessionDataFromViewModel(viewModel);
                var json = JsonSerializer.Serialize(sessionData, _jsonOptions);
                await File.WriteAllTextAsync(_testSessionFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
            }
        }

        public new async Task<SessionData?> LoadSessionAsync()
        {
            try
            {
                if (!File.Exists(_testSessionFilePath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(_testSessionFilePath);
                return JsonSerializer.Deserialize<SessionData>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load session: {ex.Message}");
                return null;
            }
        }

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
                    EndTime = video.EndTime
                });
            }

            return sessionData;
        }
    }
}