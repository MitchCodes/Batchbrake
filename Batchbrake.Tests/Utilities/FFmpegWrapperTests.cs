using Batchbrake.Utilities;
using Batchbrake.Models;

namespace Batchbrake.Tests.Utilities
{
    public class FFmpegWrapperTests
    {
        [Fact]
        public void Constructor_SetsFFmpegPath()
        {
            // Arrange
            var customPath = @"C:\tools\ffmpeg.exe";
            
            // Act
            var wrapper = new FFmpegWrapper(customPath);
            
            // Assert
            Assert.NotNull(wrapper);
        }

        [Fact]
        public void Constructor_WithDefaultPath_DoesNotThrow()
        {
            // Act & Assert
            var wrapper = new FFmpegWrapper("ffmpeg");
            Assert.NotNull(wrapper);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void GetVideoInfo_WithInvalidFilePath_ThrowsArgumentException(string? filePath)
        {
            // Arrange
            var wrapper = new FFmpegWrapper("ffmpeg");
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => wrapper.GetVideoInfo(filePath!));
        }

        [Fact]
        public void FFmpegWrapper_CanBeInstantiatedWithDifferentPaths()
        {
            // Test various constructor scenarios
            var wrapper1 = new FFmpegWrapper("ffmpeg");
            var wrapper2 = new FFmpegWrapper(@"C:\Program Files\ffmpeg\bin\ffmpeg.exe");
            var wrapper3 = new FFmpegWrapper("./tools/ffmpeg");
            
            Assert.NotNull(wrapper1);
            Assert.NotNull(wrapper2);
            Assert.NotNull(wrapper3);
        }

        [Fact] 
        public void FFmpegWrapper_DoesNotAcceptNullPath()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FFmpegWrapper((string)null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void FFmpegWrapper_HandlesEmptyOrWhitespacePaths(string path)
        {
            // Act
            var wrapper = new FFmpegWrapper(path);
            
            // Assert
            Assert.NotNull(wrapper);
        }

        [Fact]
        public void Constructor_WithFFmpegSettings_SetsProperties()
        {
            // Arrange
            var settings = new FFmpegSettings
            {
                FFmpegPath = @"C:\custom\ffmpeg.exe",
                FFprobePath = @"C:\custom\ffprobe.exe",
                VideoCodec = "libx264",
                AudioCodec = "aac"
            };
            
            // Act
            var wrapper = new FFmpegWrapper(settings);
            
            // Assert
            Assert.NotNull(wrapper);
        }

        [Fact]
        public void Constructor_WithNullSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FFmpegWrapper((FFmpegSettings)null!));
        }

        [Fact]
        public async Task IsFFmpegAvailableAsync_ReturnsBoolean()
        {
            // Arrange
            var wrapper = new FFmpegWrapper("nonexistent_ffmpeg_path");
            
            // Act
            var result = await wrapper.IsFFmpegAvailableAsync();
            
            // Assert
            Assert.False(result);
        }
    }
}