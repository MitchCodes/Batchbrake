using Batchbrake.Models;
using Batchbrake.Services;
using Batchbrake.ViewModels;
using Moq;
using System.IO;

namespace Batchbrake.Tests.Services
{
    public class SessionManagerTests : IDisposable
    {
        private readonly SessionManager _sessionManager;
        private readonly string _testSessionPath;

        public SessionManagerTests()
        {
            _sessionManager = new SessionManager();
            
            // Use the actual session file path that SessionManager uses
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Batchbrake"
            );
            _testSessionPath = Path.Combine(appDataPath, "session.json");
            
            // Clean up any existing session file before each test
            if (File.Exists(_testSessionPath))
            {
                File.Delete(_testSessionPath);
            }
        }

        public void Dispose()
        {
            // Clean up test session file
            if (File.Exists(_testSessionPath))
            {
                File.Delete(_testSessionPath);
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
            
            // Add test videos with different statuses
            await viewModel.AddNewFile(@"C:\test\video1.mp4");
            await viewModel.AddNewFile(@"C:\test\video2.mkv");
            
            // Simulate one video being completed
            viewModel.VideoQueue[0].ConversionStatus = VideoConversionStatus.Completed;
            viewModel.VideoQueue[0].ConversionProgress = 100;
            
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
            await Task.Delay(100); // Give time for initialization
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
}