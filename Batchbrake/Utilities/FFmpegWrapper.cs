using Batchbrake.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Batchbrake.Utilities
{
    /// <summary>
    /// Provides functionality to retrieve video details using FFmpeg.
    /// </summary>
    public class FFmpegWrapper
    {
        private readonly string _ffmpegPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegService"/> class.
        /// </summary>
        /// <param name="ffmpegPath">The path to the FFmpeg executable.</param>
        public FFmpegWrapper(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath;
        }

        /// <summary>
        /// Retrieves the details of the specified video file.
        /// </summary>
        /// <param name="filePath">The path to the video file.</param>
        /// <returns>The details of the video file.</returns>
        public VideoInfoModel GetVideoInfo(string filePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-i \"{filePath}\"",
                RedirectStandardError = true, // FFmpeg outputs metadata to standard error
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                var output = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 && process.ExitCode != 1)
                {
                    throw new Exception($"FFmpeg exited with code {process.ExitCode}. Output: {output}");
                }

                return ParseVideoInfo(output, filePath);
            }
        }

        /// <summary>
        /// Parses the FFmpeg output to extract video details.
        /// </summary>
        /// <param name="ffmpegOutput">The output from FFmpeg.</param>
        /// <param name="filePath">The path to the video file.</param>
        /// <returns>The extracted video details.</returns>
        private VideoInfoModel ParseVideoInfo(string ffmpegOutput, string filePath)
        {
            var videoInfo = new VideoInfoModel
            {
                FileName = Path.GetFileName(filePath)
            };

            // Extract duration
            var durationMatch = Regex.Match(ffmpegOutput, @"Duration:\s(\d+):(\d+):(\d+)\.(\d+)");
            if (durationMatch.Success)
            {
                var hours = int.Parse(durationMatch.Groups[1].Value);
                var minutes = int.Parse(durationMatch.Groups[2].Value);
                var seconds = int.Parse(durationMatch.Groups[3].Value);
                videoInfo.Duration = new TimeSpan(hours, minutes, seconds);
            }

            // Extract resolution
            var resolutionMatch = Regex.Match(ffmpegOutput, @"Stream.*Video:.*(\d{3,4})x(\d{2,4})");
            if (resolutionMatch.Success)
            {
                videoInfo.Resolution = $"{resolutionMatch.Groups[1].Value}x{resolutionMatch.Groups[2].Value}";
            }

            // Extract codec
            var codecMatch = Regex.Match(ffmpegOutput, @"Stream.*Video:\s(\w+)");
            if (codecMatch.Success)
            {
                videoInfo.Codec = codecMatch.Groups[1].Value;
            }

            // Get file size
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                videoInfo.FileSizeBytes = fileInfo.Length;
                videoInfo.FileSize = FormatFileSize(fileInfo.Length);
            }

            return videoInfo;
        }

        /// <summary>
        /// Formats a file size in bytes to a human-readable string.
        /// </summary>
        /// <param name="bytes">The file size in bytes.</param>
        /// <returns>A human-readable file size string.</returns>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int index = 0;
            
            while (size >= 1024 && index < sizes.Length - 1)
            {
                size /= 1024;
                index++;
            }
            
            return $"{size:0.##} {sizes[index]}";
        }
    }
}
