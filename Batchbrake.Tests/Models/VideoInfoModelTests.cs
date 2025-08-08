using Batchbrake.Models;

namespace Batchbrake.Tests.Models
{
    public class VideoInfoModelTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var videoInfo = new VideoInfoModel();

            // Assert
            Assert.Null(videoInfo.FileName);
            Assert.Equal(TimeSpan.Zero, videoInfo.Duration);
            Assert.Null(videoInfo.Resolution);
            Assert.Null(videoInfo.Codec);
            Assert.Null(videoInfo.FileSize);
            Assert.Equal(0, videoInfo.FileSizeBytes);
        }

        [Fact]
        public void FileName_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            var fileName = "test_video.mp4";
            
            // Act
            videoInfo.FileName = fileName;
            
            // Assert
            Assert.Equal(fileName, videoInfo.FileName);
        }

        [Theory]
        [InlineData("video.mp4")]
        [InlineData("movie.mkv")]
        [InlineData("clip.avi")]
        [InlineData("")]
        [InlineData(null)]
        public void FileName_AcceptsVariousValues(string? fileName)
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            
            // Act
            videoInfo.FileName = fileName;
            
            // Assert
            Assert.Equal(fileName, videoInfo.FileName);
        }

        [Fact]
        public void Duration_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            var duration = TimeSpan.FromMinutes(5);
            
            // Act
            videoInfo.Duration = duration;
            
            // Assert
            Assert.Equal(duration, videoInfo.Duration);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 5, 30)]
        [InlineData(1, 23, 45)]
        [InlineData(2, 0, 0)]
        public void Duration_AcceptsVariousTimeSpans(int hours, int minutes, int seconds)
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            var duration = new TimeSpan(hours, minutes, seconds);
            
            // Act
            videoInfo.Duration = duration;
            
            // Assert
            Assert.Equal(duration, videoInfo.Duration);
        }

        [Fact]
        public void Resolution_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            var resolution = "1920x1080";
            
            // Act
            videoInfo.Resolution = resolution;
            
            // Assert
            Assert.Equal(resolution, videoInfo.Resolution);
        }

        [Theory]
        [InlineData("1920x1080")]
        [InlineData("1280x720")]
        [InlineData("3840x2160")]
        [InlineData("640x480")]
        [InlineData("")]
        [InlineData(null)]
        public void Resolution_AcceptsVariousValues(string? resolution)
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            
            // Act
            videoInfo.Resolution = resolution;
            
            // Assert
            Assert.Equal(resolution, videoInfo.Resolution);
        }

        [Fact]
        public void Codec_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            var codec = "h264";
            
            // Act
            videoInfo.Codec = codec;
            
            // Assert
            Assert.Equal(codec, videoInfo.Codec);
        }

        [Theory]
        [InlineData("h264")]
        [InlineData("h265")]
        [InlineData("vp8")]
        [InlineData("vp9")]
        [InlineData("av1")]
        [InlineData("mpeg4")]
        [InlineData("")]
        [InlineData(null)]
        public void Codec_AcceptsVariousValues(string? codec)
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            
            // Act
            videoInfo.Codec = codec;
            
            // Assert
            Assert.Equal(codec, videoInfo.Codec);
        }

        [Fact]
        public void FileSize_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            var fileSize = "100 MB";
            
            // Act
            videoInfo.FileSize = fileSize;
            
            // Assert
            Assert.Equal(fileSize, videoInfo.FileSize);
        }

        [Theory]
        [InlineData("100 MB")]
        [InlineData("1.5 GB")]
        [InlineData("500 KB")]
        [InlineData("2 TB")]
        [InlineData("1024 B")]
        [InlineData("")]
        [InlineData(null)]
        public void FileSize_AcceptsVariousValues(string? fileSize)
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            
            // Act
            videoInfo.FileSize = fileSize;
            
            // Assert
            Assert.Equal(fileSize, videoInfo.FileSize);
        }

        [Fact]
        public void FileSizeBytes_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            var fileSizeBytes = 104857600L; // 100 MB in bytes
            
            // Act
            videoInfo.FileSizeBytes = fileSizeBytes;
            
            // Assert
            Assert.Equal(fileSizeBytes, videoInfo.FileSizeBytes);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(1024L)]
        [InlineData(1048576L)]
        [InlineData(1073741824L)]
        [InlineData(1099511627776L)]
        public void FileSizeBytes_AcceptsVariousValues(long fileSizeBytes)
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            
            // Act
            videoInfo.FileSizeBytes = fileSizeBytes;
            
            // Assert
            Assert.Equal(fileSizeBytes, videoInfo.FileSizeBytes);
        }

        [Fact]
        public void AllProperties_CanBeSetTogether()
        {
            // Arrange
            var videoInfo = new VideoInfoModel();
            var fileName = "test_movie.mp4";
            var duration = TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(15));
            var resolution = "1920x1080";
            var codec = "h264";
            var fileSize = "2.5 GB";
            var fileSizeBytes = 2684354560L;
            
            // Act
            videoInfo.FileName = fileName;
            videoInfo.Duration = duration;
            videoInfo.Resolution = resolution;
            videoInfo.Codec = codec;
            videoInfo.FileSize = fileSize;
            videoInfo.FileSizeBytes = fileSizeBytes;
            
            // Assert
            Assert.Equal(fileName, videoInfo.FileName);
            Assert.Equal(duration, videoInfo.Duration);
            Assert.Equal(resolution, videoInfo.Resolution);
            Assert.Equal(codec, videoInfo.Codec);
            Assert.Equal(fileSize, videoInfo.FileSize);
            Assert.Equal(fileSizeBytes, videoInfo.FileSizeBytes);
        }

        [Fact]
        public void VideoInfoModel_SupportsObjectInitializer()
        {
            // Arrange & Act
            var videoInfo = new VideoInfoModel
            {
                FileName = "test.mp4",
                Duration = TimeSpan.FromMinutes(10),
                Resolution = "1280x720",
                Codec = "h265",
                FileSize = "500 MB",
                FileSizeBytes = 524288000L
            };
            
            // Assert
            Assert.Equal("test.mp4", videoInfo.FileName);
            Assert.Equal(TimeSpan.FromMinutes(10), videoInfo.Duration);
            Assert.Equal("1280x720", videoInfo.Resolution);
            Assert.Equal("h265", videoInfo.Codec);
            Assert.Equal("500 MB", videoInfo.FileSize);
            Assert.Equal(524288000L, videoInfo.FileSizeBytes);
        }

        [Fact]
        public void VideoInfoModel_PropertiesAreIndependent()
        {
            // Arrange
            var videoInfo1 = new VideoInfoModel { FileName = "video1.mp4", Duration = TimeSpan.FromMinutes(5) };
            var videoInfo2 = new VideoInfoModel { FileName = "video2.mkv", Duration = TimeSpan.FromMinutes(10) };
            
            // Act
            videoInfo1.Resolution = "1920x1080";
            videoInfo2.Resolution = "1280x720";
            
            // Assert
            Assert.Equal("video1.mp4", videoInfo1.FileName);
            Assert.Equal("video2.mkv", videoInfo2.FileName);
            Assert.Equal(TimeSpan.FromMinutes(5), videoInfo1.Duration);
            Assert.Equal(TimeSpan.FromMinutes(10), videoInfo2.Duration);
            Assert.Equal("1920x1080", videoInfo1.Resolution);
            Assert.Equal("1280x720", videoInfo2.Resolution);
        }
    }
}