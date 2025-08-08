using Batchbrake.ViewModels;
using Batchbrake.Models;

namespace Batchbrake.Tests.ViewModels
{
    public class VideoViewModelTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var viewModel = new VideoModelViewModel();

            // Assert
            Assert.Equal(0, viewModel.Index);
            Assert.Null(viewModel.InputFilePath);
            Assert.Null(viewModel.VideoInfo);
            Assert.Null(viewModel.OutputFilePath);
            Assert.Null(viewModel.OutputFileName);
            Assert.Null(viewModel.Preset);
            Assert.NotNull(viewModel.Presets);
            Assert.Empty(viewModel.Presets);
            Assert.Null(viewModel.Clips);
            Assert.Equal(VideoConversionStatus.NotStarted, viewModel.ConversionStatus);
            Assert.Equal(0, viewModel.ConversionProgress);
            Assert.Null(viewModel.ErrorMessage);
            Assert.Null(viewModel.StartTime);
            Assert.Null(viewModel.EndTime);
            Assert.False(viewModel.DeleteSourceAfterConversion);
            Assert.Equal("mp4", viewModel.OutputFormat);
        }

        [Fact]
        public void Index_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            
            // Act
            viewModel.Index = 5;
            
            // Assert
            Assert.Equal(5, viewModel.Index);
        }

        [Fact]
        public void InputFilePath_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            var filePath = @"C:\videos\test.mp4";
            
            // Act
            viewModel.InputFilePath = filePath;
            
            // Assert
            Assert.Equal(filePath, viewModel.InputFilePath);
        }

        [Fact]
        public void VideoInfo_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            var videoInfo = new VideoInfoModel
            {
                FileName = "test.mp4",
                Duration = TimeSpan.FromMinutes(10),
                Resolution = "1920x1080",
                Codec = "h264",
                FileSize = "500 MB",
                FileSizeBytes = 524288000
            };
            
            // Act
            viewModel.VideoInfo = videoInfo;
            
            // Assert
            Assert.Equal(videoInfo, viewModel.VideoInfo);
            Assert.Equal("test.mp4", viewModel.VideoInfo.FileName);
            Assert.Equal(TimeSpan.FromMinutes(10), viewModel.VideoInfo.Duration);
            Assert.Equal("1920x1080", viewModel.VideoInfo.Resolution);
        }

        [Fact]
        public void OutputFilePath_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            var outputPath = @"C:\output\converted.mp4";
            
            // Act
            viewModel.OutputFilePath = outputPath;
            
            // Assert
            Assert.Equal(outputPath, viewModel.OutputFilePath);
        }

        [Fact]
        public void OutputFileName_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            var fileName = "converted_video.mp4";
            
            // Act
            viewModel.OutputFileName = fileName;
            
            // Assert
            Assert.Equal(fileName, viewModel.OutputFileName);
        }

        [Fact]
        public void Preset_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            var preset = "Fast 1080p30";
            
            // Act
            viewModel.Preset = preset;
            
            // Assert
            Assert.Equal(preset, viewModel.Preset);
        }

        [Fact]
        public void Presets_CanAddAndRemove()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            
            // Act
            viewModel.Presets.Add("Fast 1080p30");
            viewModel.Presets.Add("Slow 4K");
            
            // Assert
            Assert.Equal(2, viewModel.Presets.Count);
            Assert.Contains("Fast 1080p30", viewModel.Presets);
            Assert.Contains("Slow 4K", viewModel.Presets);
        }

        [Fact]
        public void ConversionStatus_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            
            // Act & Assert for each status
            viewModel.ConversionStatus = VideoConversionStatus.Queued;
            Assert.Equal(VideoConversionStatus.Queued, viewModel.ConversionStatus);
            
            viewModel.ConversionStatus = VideoConversionStatus.InProgress;
            Assert.Equal(VideoConversionStatus.InProgress, viewModel.ConversionStatus);
            
            viewModel.ConversionStatus = VideoConversionStatus.Completed;
            Assert.Equal(VideoConversionStatus.Completed, viewModel.ConversionStatus);
            
            viewModel.ConversionStatus = VideoConversionStatus.Failed;
            Assert.Equal(VideoConversionStatus.Failed, viewModel.ConversionStatus);
            
            viewModel.ConversionStatus = VideoConversionStatus.Cancelled;
            Assert.Equal(VideoConversionStatus.Cancelled, viewModel.ConversionStatus);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(25)]
        [InlineData(50)]
        [InlineData(75)]
        [InlineData(100)]
        public void ConversionProgress_SetAndGet_WorksCorrectly(double progress)
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            
            // Act
            viewModel.ConversionProgress = progress;
            
            // Assert
            Assert.Equal(progress, viewModel.ConversionProgress);
        }

        [Fact]
        public void ErrorMessage_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            var errorMessage = "Conversion failed due to invalid codec";
            
            // Act
            viewModel.ErrorMessage = errorMessage;
            
            // Assert
            Assert.Equal(errorMessage, viewModel.ErrorMessage);
        }

        [Fact]
        public void StartTime_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            var startTime = DateTime.Now;
            
            // Act
            viewModel.StartTime = startTime;
            
            // Assert
            Assert.Equal(startTime, viewModel.StartTime);
        }

        [Fact]
        public void EndTime_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            var endTime = DateTime.Now;
            
            // Act
            viewModel.EndTime = endTime;
            
            // Assert
            Assert.Equal(endTime, viewModel.EndTime);
        }

        [Fact]
        public void DeleteSourceAfterConversion_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            
            // Act
            viewModel.DeleteSourceAfterConversion = true;
            
            // Assert
            Assert.True(viewModel.DeleteSourceAfterConversion);
            
            // Act
            viewModel.DeleteSourceAfterConversion = false;
            
            // Assert
            Assert.False(viewModel.DeleteSourceAfterConversion);
        }

        [Theory]
        [InlineData("mp4")]
        [InlineData("mkv")]
        [InlineData("avi")]
        [InlineData("webm")]
        public void OutputFormat_SetAndGet_WorksCorrectly(string format)
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            
            // Act
            viewModel.OutputFormat = format;
            
            // Assert
            Assert.Equal(format, viewModel.OutputFormat);
        }

        [Fact]
        public void OutputFormat_UpdatesOutputFilePath()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            viewModel.OutputFilePath = @"C:\output\test.mp4";
            
            // Act
            viewModel.OutputFormat = "mkv";
            
            // Assert
            Assert.Equal(@"C:\output\test.mkv", viewModel.OutputFilePath);
        }

        [Fact]
        public void OutputFormat_DoesNotUpdateEmptyOutputFilePath()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            viewModel.OutputFilePath = null;
            
            // Act
            viewModel.OutputFormat = "mkv";
            
            // Assert
            Assert.Null(viewModel.OutputFilePath);
        }

        [Fact]
        public void Clips_CanAddAndRemove()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            var clip1 = new ClipModel { Start = TimeSpan.FromSeconds(30), End = TimeSpan.FromSeconds(90) };
            var clip2 = new ClipModel { Start = TimeSpan.FromSeconds(120), End = TimeSpan.FromSeconds(180) };
            
            viewModel.Clips = new System.Collections.ObjectModel.ObservableCollection<ClipModel>();
            
            // Act
            viewModel.Clips.Add(clip1);
            viewModel.Clips.Add(clip2);
            
            // Assert
            Assert.Equal(2, viewModel.Clips.Count);
            Assert.Contains(clip1, viewModel.Clips);
            Assert.Contains(clip2, viewModel.Clips);
        }

        [Fact]
        public void PropertyChanges_RaisePropertyChangedEvent()
        {
            // Arrange
            var viewModel = new VideoModelViewModel();
            var propertyChangedEvents = new List<string>();
            
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                    propertyChangedEvents.Add(e.PropertyName);
            };
            
            // Act
            viewModel.Index = 1;
            viewModel.ConversionProgress = 50;
            viewModel.ConversionStatus = VideoConversionStatus.InProgress;
            viewModel.ErrorMessage = "Test error";
            
            // Assert
            Assert.Contains(nameof(viewModel.Index), propertyChangedEvents);
            Assert.Contains(nameof(viewModel.ConversionProgress), propertyChangedEvents);
            Assert.Contains(nameof(viewModel.ConversionStatus), propertyChangedEvents);
            Assert.Contains(nameof(viewModel.ErrorMessage), propertyChangedEvents);
        }
    }

    public class ClipModelTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var clipModel = new ClipModel();

            // Assert
            Assert.Equal(TimeSpan.Zero, clipModel.Start);
            Assert.Equal(TimeSpan.Zero, clipModel.End);
        }

        [Fact]
        public void Start_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var clipModel = new ClipModel();
            var startTime = TimeSpan.FromSeconds(30);
            
            // Act
            clipModel.Start = startTime;
            
            // Assert
            Assert.Equal(startTime, clipModel.Start);
        }

        [Fact]
        public void End_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var clipModel = new ClipModel();
            var endTime = TimeSpan.FromSeconds(120);
            
            // Act
            clipModel.End = endTime;
            
            // Assert
            Assert.Equal(endTime, clipModel.End);
        }

        [Fact]
        public void PropertyChanges_RaisePropertyChangedEvent()
        {
            // Arrange
            var clipModel = new ClipModel();
            var propertyChangedEvents = new List<string>();
            
            clipModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                    propertyChangedEvents.Add(e.PropertyName);
            };
            
            // Act
            clipModel.Start = TimeSpan.FromSeconds(10);
            clipModel.End = TimeSpan.FromSeconds(60);
            
            // Assert
            Assert.Contains(nameof(clipModel.Start), propertyChangedEvents);
            Assert.Contains(nameof(clipModel.End), propertyChangedEvents);
        }
    }

    public class VideoConversionStatusTests
    {
        [Fact]
        public void VideoConversionStatus_HasCorrectValues()
        {
            // Assert
            Assert.Equal(0, (int)VideoConversionStatus.NotStarted);
            Assert.Equal(1, (int)VideoConversionStatus.Queued);
            Assert.Equal(2, (int)VideoConversionStatus.InProgress);
            Assert.Equal(3, (int)VideoConversionStatus.Completed);
            Assert.Equal(4, (int)VideoConversionStatus.Failed);
            Assert.Equal(5, (int)VideoConversionStatus.Cancelled);
        }
    }
}