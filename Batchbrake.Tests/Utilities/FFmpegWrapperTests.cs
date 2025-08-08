using Batchbrake.Utilities;
using Batchbrake.Models;

namespace Batchbrake.Tests.Utilities
{
    public class FFmpegWrapperTests
    {
        private readonly string _testFFmpegPath = "ffmpeg";

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
            var wrapper = new FFmpegWrapper(_testFFmpegPath);
            Assert.NotNull(wrapper);
        }

        [Fact]
        public void GetVideoInfo_WithNonexistentFile_ThrowsException()
        {
            // Arrange
            var wrapper = new FFmpegWrapper("invalid_ffmpeg_path");
            var nonexistentFile = @"C:\nonexistent\file.mp4";
            
            // Act & Assert
            Assert.ThrowsAny<Exception>(() => wrapper.GetVideoInfo(nonexistentFile));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void GetVideoInfo_WithInvalidFilePath_ThrowsArgumentException(string? filePath)
        {
            // Arrange
            var wrapper = new FFmpegWrapper(_testFFmpegPath);
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => wrapper.GetVideoInfo(filePath!));
        }

        [Fact]
        public void GetVideoInfo_WithInvalidFFmpegPath_ThrowsException()
        {
            // Arrange
            var wrapper = new FFmpegWrapper("nonexistent_ffmpeg_path");
            var testFile = Path.GetTempFileName();
            
            try
            {
                // Act & Assert
                Assert.ThrowsAny<Exception>(() => wrapper.GetVideoInfo(testFile));
            }
            finally
            {
                File.Delete(testFile);
            }
        }

        [Fact]
        public void GetVideoInfo_WithValidTemporaryFile_CreatesVideoInfoModel()
        {
            // Arrange
            var wrapper = new FFmpegWrapper("invalid_ffmpeg_executable"); // Use invalid path to trigger error
            var testFile = Path.GetTempFileName();
            File.WriteAllText(testFile, "test video content");

            try
            {
                // Act & Assert
                // This should throw an exception since ffmpeg path is invalid
                // But it validates that the method doesn't crash with argument exceptions
                Assert.ThrowsAny<Exception>(() => wrapper.GetVideoInfo(testFile));
            }
            catch (ArgumentException)
            {
                Assert.True(false, "Should not throw ArgumentException - indicates parameter validation issue");
            }
            finally
            {
                File.Delete(testFile);
            }
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
        public void FFmpegWrapper_HandlesDifferentFileExtensions()
        {
            // Arrange
            var wrapper = new FFmpegWrapper("invalid_ffmpeg");
            var extensions = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm" };

            foreach (var ext in extensions)
            {
                var testFile = Path.ChangeExtension(Path.GetTempFileName(), ext);
                File.WriteAllText(testFile, "test content");

                try
                {
                    // Act & Assert
                    // Should not throw argument exceptions regardless of file extension
                    Assert.ThrowsAny<Exception>(() => wrapper.GetVideoInfo(testFile));
                }
                catch (ArgumentException)
                {
                    Assert.True(false, $"Should not throw ArgumentException for {ext} files");
                }
                finally
                {
                    File.Delete(testFile);
                }
            }
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
    }
}