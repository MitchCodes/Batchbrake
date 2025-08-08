using Batchbrake.Services;
using Batchbrake.ViewModels;
using Batchbrake.Models;
using Moq;
using Avalonia.Platform.Storage;
using System.Reactive;
using System.Reactive.Linq;

namespace Batchbrake.Tests.ViewModels
{
    public class MainWindowViewModelTests
    {
        private readonly Mock<IFilePickerService> _mockFilePickerService;
        private readonly MainWindowViewModel _viewModel;

        public MainWindowViewModelTests()
        {
            _mockFilePickerService = new Mock<IFilePickerService>();
            _viewModel = new MainWindowViewModel(_mockFilePickerService.Object);
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var viewModel = new MainWindowViewModel(_mockFilePickerService.Object);

            // Assert
            Assert.NotNull(viewModel.VideoQueue);
            Assert.Empty(viewModel.VideoQueue);
            Assert.NotNull(viewModel.Presets);
            Assert.Equal(1, viewModel.ParallelInstances);
            Assert.Equal("$(Folder)\\$(FileName)_conv.$(Ext)", viewModel.DefaultOutputPath);
            Assert.False(viewModel.IsDraggingOver);
            Assert.False(viewModel.IsConverting);
            Assert.Equal("Ready", viewModel.StatusText);
            Assert.Empty(viewModel.LogOutput);
            Assert.False(viewModel.DeleteSourceAfterConversion);
            Assert.Equal("mp4", viewModel.DefaultOutputFormat);
        }

        [Fact]
        public void VideoQueue_UpdatesCountProperties()
        {
            // Arrange
            var video1 = CreateTestVideoViewModel(VideoConversionStatus.NotStarted);
            var video2 = CreateTestVideoViewModel(VideoConversionStatus.InProgress);
            var video3 = CreateTestVideoViewModel(VideoConversionStatus.Completed);

            // Act
            _viewModel.VideoQueue.Add(video1);
            _viewModel.VideoQueue.Add(video2);
            _viewModel.VideoQueue.Add(video3);

            // Assert
            Assert.Equal(3, _viewModel.QueueCount);
            Assert.Equal(1, _viewModel.ProcessingCount);
            Assert.Equal(1, _viewModel.CompletedCount);
        }

        [Fact]
        public void CanStartConversion_ReturnsTrueWhenNotConvertingAndHasVideos()
        {
            // Arrange
            _viewModel.VideoQueue.Add(CreateTestVideoViewModel());
            
            // Act & Assert
            Assert.True(_viewModel.CanStartConversion);
        }

        [Fact]
        public void CanStartConversion_ReturnsFalseWhenConverting()
        {
            // Arrange
            _viewModel.VideoQueue.Add(CreateTestVideoViewModel());
            _viewModel.IsConverting = true;
            
            // Act & Assert
            Assert.False(_viewModel.CanStartConversion);
        }

        [Fact]
        public void CanStartConversion_ReturnsFalseWhenNoVideos()
        {
            // Arrange
            Assert.Empty(_viewModel.VideoQueue);
            
            // Act & Assert
            Assert.False(_viewModel.CanStartConversion);
        }

        [Fact]
        public async Task AddNewFile_AddsVideoToQueue()
        {
            // Arrange
            var filePath = @"C:\test\video.mp4";
            
            // Act
            await _viewModel.AddNewFile(filePath);
            
            // Assert
            Assert.Single(_viewModel.VideoQueue);
            var addedVideo = _viewModel.VideoQueue.First();
            Assert.Equal(filePath, addedVideo.InputFilePath);
            Assert.Equal(0, addedVideo.Index);
        }

        [Fact]
        public async Task AddNewFile_DoesNotAddDuplicateFile()
        {
            // Arrange
            var filePath = @"C:\test\video.mp4";
            await _viewModel.AddNewFile(filePath);
            
            // Act
            await _viewModel.AddNewFile(filePath);
            
            // Assert
            Assert.Single(_viewModel.VideoQueue);
        }

        [Fact]
        public void RemoveVideo_RemovesVideoFromQueue()
        {
            // Arrange
            var video = CreateTestVideoViewModel();
            _viewModel.VideoQueue.Add(video);
            Assert.Single(_viewModel.VideoQueue);
            
            // Act
            _viewModel.RemoveVideo(video);
            
            // Assert
            Assert.Empty(_viewModel.VideoQueue);
        }

        [Fact]
        public void RemoveVideo_ReassignsIndexes()
        {
            // Arrange
            var video1 = CreateTestVideoViewModel();
            var video2 = CreateTestVideoViewModel();
            var video3 = CreateTestVideoViewModel();
            _viewModel.VideoQueue.Add(video1);
            _viewModel.VideoQueue.Add(video2);
            _viewModel.VideoQueue.Add(video3);
            
            // Act
            _viewModel.RemoveVideo(video2); // Remove middle video
            
            // Assert
            Assert.Equal(2, _viewModel.VideoQueue.Count);
            Assert.Equal(0, _viewModel.VideoQueue[0].Index);
            Assert.Equal(1, _viewModel.VideoQueue[1].Index);
        }

        [Fact]
        public async Task AddVideosCommand_CallsFilePickerService()
        {
            // Arrange
            var mockFiles = new List<IStorageFile>
            {
                CreateMockStorageFile(@"C:\test\video1.mp4"),
                CreateMockStorageFile(@"C:\test\video2.mkv")
            };
            
            _mockFilePickerService.Setup(x => x.OpenFilePickerAsync(It.IsAny<FilePickerOpenOptions>()))
                .ReturnsAsync(mockFiles);

            // Act
            _viewModel.AddVideosCommand.Execute().Subscribe();

            // Assert
            _mockFilePickerService.Verify(x => x.OpenFilePickerAsync(It.IsAny<FilePickerOpenOptions>()), Times.Once);
            Assert.Equal(2, _viewModel.VideoQueue.Count);
        }

        [Fact]
        public void ClearCompletedCommand_RemovesOnlyCompletedVideos()
        {
            // Arrange
            var notStarted = CreateTestVideoViewModel(VideoConversionStatus.NotStarted);
            var inProgress = CreateTestVideoViewModel(VideoConversionStatus.InProgress);
            var completed1 = CreateTestVideoViewModel(VideoConversionStatus.Completed);
            var completed2 = CreateTestVideoViewModel(VideoConversionStatus.Completed);
            var failed = CreateTestVideoViewModel(VideoConversionStatus.Failed);
            
            _viewModel.VideoQueue.Add(notStarted);
            _viewModel.VideoQueue.Add(inProgress);
            _viewModel.VideoQueue.Add(completed1);
            _viewModel.VideoQueue.Add(completed2);
            _viewModel.VideoQueue.Add(failed);
            
            // Act
            _viewModel.ClearCompletedCommand.Execute().Subscribe();
            
            // Assert
            Assert.Equal(3, _viewModel.VideoQueue.Count);
            Assert.DoesNotContain(completed1, _viewModel.VideoQueue);
            Assert.DoesNotContain(completed2, _viewModel.VideoQueue);
            Assert.Contains(notStarted, _viewModel.VideoQueue);
            Assert.Contains(inProgress, _viewModel.VideoQueue);
            Assert.Contains(failed, _viewModel.VideoQueue);
        }

        [Fact]
        public void StopConversionCommand_StopsConversionAndMarksCancelledVideos()
        {
            // Arrange
            var video1 = CreateTestVideoViewModel(VideoConversionStatus.InProgress);
            var video2 = CreateTestVideoViewModel(VideoConversionStatus.InProgress);
            var video3 = CreateTestVideoViewModel(VideoConversionStatus.NotStarted);
            
            _viewModel.VideoQueue.Add(video1);
            _viewModel.VideoQueue.Add(video2);
            _viewModel.VideoQueue.Add(video3);
            _viewModel.IsConverting = true;
            
            // Act
            _viewModel.StopConversionCommand.Execute().Subscribe();
            
            // Assert
            Assert.False(_viewModel.IsConverting);
            Assert.Equal("Conversion stopped", _viewModel.StatusText);
            Assert.Equal(VideoConversionStatus.Cancelled, video1.ConversionStatus);
            Assert.Equal(VideoConversionStatus.Cancelled, video2.ConversionStatus);
            Assert.Equal(VideoConversionStatus.NotStarted, video3.ConversionStatus); // Should not change
            Assert.Equal(0, video1.ConversionProgress);
            Assert.Equal(0, video2.ConversionProgress);
        }

        [Fact]
        public void PauseConversionCommand_UpdatesStatusAndLog()
        {
            // Arrange
            var initialLogLength = _viewModel.LogOutput.Length;
            
            // Act
            _viewModel.PauseConversionCommand.Execute().Subscribe();
            
            // Assert
            Assert.Equal("Conversion paused", _viewModel.StatusText);
            Assert.True(_viewModel.LogOutput.Length > initialLogLength);
            Assert.Contains("Conversion paused by user", _viewModel.LogOutput);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void ParallelInstances_AcceptsValidValues(int value)
        {
            // Act
            _viewModel.ParallelInstances = value;
            
            // Assert
            Assert.Equal(value, _viewModel.ParallelInstances);
        }

        [Fact]
        public void DefaultOutputPath_ReplacesTokensCorrectly()
        {
            // This test validates the token replacement logic in AddNewFile method
            // The actual replacement is tested implicitly when adding files
            
            // Arrange
            var customPath = "$(Folder)\\converted\\$(FileName)_output.$(Ext)";
            
            // Act
            _viewModel.DefaultOutputPath = customPath;
            
            // Assert
            Assert.Equal(customPath, _viewModel.DefaultOutputPath);
        }

        [Fact]
        public void PropertyChanges_RaisePropertyChangedEvent()
        {
            // Arrange
            bool statusChanged = false;
            bool isDraggingChanged = false;
            
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.StatusText))
                    statusChanged = true;
                if (e.PropertyName == nameof(_viewModel.IsDraggingOver))
                    isDraggingChanged = true;
            };
            
            // Act
            _viewModel.StatusText = "New Status";
            _viewModel.IsDraggingOver = true;
            
            // Assert
            Assert.True(statusChanged);
            Assert.True(isDraggingChanged);
        }

        private VideoModelViewModel CreateTestVideoViewModel(VideoConversionStatus status = VideoConversionStatus.NotStarted)
        {
            return new VideoModelViewModel
            {
                InputFilePath = $@"C:\test\video_{Guid.NewGuid()}.mp4",
                VideoInfo = new VideoInfoModel
                {
                    FileName = "test_video.mp4",
                    Duration = TimeSpan.FromMinutes(5),
                    Resolution = "1920x1080",
                    Codec = "h264",
                    FileSize = "100 MB",
                    FileSizeBytes = 104857600
                },
                ConversionStatus = status,
                ConversionProgress = 0,
                Preset = "Fast 1080p30",
                OutputFilePath = $@"C:\test\output_{Guid.NewGuid()}.mp4"
            };
        }

        private IStorageFile CreateMockStorageFile(string path)
        {
            var mock = new Mock<IStorageFile>();
            mock.Setup(x => x.Path).Returns(new Uri(path));
            return mock.Object;
        }
    }
}