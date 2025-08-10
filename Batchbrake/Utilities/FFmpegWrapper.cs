using Batchbrake.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Batchbrake.Utilities
{
    /// <summary>
    /// Provides functionality to retrieve video details using FFmpeg.
    /// </summary>
    public class FFmpegWrapper : IFFmpegWrapper
    {
        private readonly string _ffmpegPath;
        private readonly FFmpegSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegService"/> class.
        /// </summary>
        /// <param name="ffmpegPath">The path to the FFmpeg executable.</param>
        public FFmpegWrapper(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath ?? throw new ArgumentNullException(nameof(ffmpegPath));
            _settings = new FFmpegSettings { FFmpegPath = ffmpegPath };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegService"/> class with settings.
        /// </summary>
        /// <param name="settings">The FFmpeg settings.</param>
        public FFmpegWrapper(FFmpegSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _ffmpegPath = settings.FFmpegPath;
        }

        /// <summary>
        /// Retrieves the details of the specified video file.
        /// </summary>
        /// <param name="filePath">The path to the video file.</param>
        /// <returns>The details of the video file.</returns>
        public VideoInfoModel GetVideoInfo(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
                
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
        /// Asynchronously retrieves the details of the specified video file.
        /// </summary>
        /// <param name="filePath">The path to the video file.</param>
        /// <returns>The details of the video file.</returns>
        public async Task<VideoInfoModel> GetVideoInfoAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
                
            // Use ffprobe if available for better metadata extraction
            var probePath = _settings?.FFprobePath ?? "ffprobe";
            var useFFprobe = !string.IsNullOrEmpty(probePath) && probePath != "ffprobe";
            
            var startInfo = new ProcessStartInfo
            {
                FileName = useFFprobe ? probePath : _ffmpegPath,
                Arguments = useFFprobe 
                    ? $"-v quiet -print_format json -show_format -show_streams \"{filePath}\""
                    : $"-i \"{filePath}\"",
                RedirectStandardError = true, // FFmpeg outputs metadata to standard error
                RedirectStandardOutput = useFFprobe,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                var output = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

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

        /// <summary>
        /// Converts a video file using FFmpeg with the configured settings.
        /// </summary>
        /// <param name="inputFile">The path to the input video file.</param>
        /// <param name="outputFile">The path where the output video should be saved.</param>
        /// <param name="progressCallback">Optional callback for progress updates.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>True if conversion was successful, false otherwise.</returns>
        public async Task<bool> ConvertVideoAsync(
            string inputFile, 
            string outputFile, 
            Action<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inputFile))
                throw new ArgumentException("Input file path cannot be null or empty.", nameof(inputFile));
            if (string.IsNullOrWhiteSpace(outputFile))
                throw new ArgumentException("Output file path cannot be null or empty.", nameof(outputFile));

            // Get video duration for progress calculation
            var videoInfo = await GetVideoInfoAsync(inputFile);
            var totalDuration = videoInfo.Duration.TotalSeconds;

            var arguments = BuildFFmpegArguments(inputFile, outputFile);
            
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                    throw new Exception("Failed to start FFmpeg process");

                // Read stderr for progress updates
                var progressTask = Task.Run(async () =>
                {
                    using (var reader = process.StandardError)
                    {
                        var buffer = new char[256];
                        var lineBuilder = new StringBuilder();
                        
                        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                        {
                            var charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                            if (charsRead > 0)
                            {
                                lineBuilder.Append(buffer, 0, charsRead);
                                var lines = lineBuilder.ToString().Split('\r', '\n');
                                
                                for (int i = 0; i < lines.Length - 1; i++)
                                {
                                    ProcessFFmpegOutputLine(lines[i], totalDuration, progressCallback);
                                }
                                
                                lineBuilder.Clear();
                                if (lines.Length > 0)
                                    lineBuilder.Append(lines[lines.Length - 1]);
                            }
                        }
                    }
                });

                // Wait for process to complete or cancellation
                while (!process.HasExited)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                        return false;
                    }
                    await Task.Delay(100);
                }

                await progressTask;
                return process.ExitCode == 0;
            }
        }

        /// <summary>
        /// Builds FFmpeg command-line arguments based on the current settings.
        /// </summary>
        private string BuildFFmpegArguments(string inputFile, string outputFile)
        {
            var args = new StringBuilder();

            // Hardware acceleration (input)
            if (_settings.HardwareAcceleration)
            {
                switch (_settings.HardwareAccelerationMethod.ToLower())
                {
                    case "cuda":
                        args.Append("-hwaccel cuda -hwaccel_output_format cuda ");
                        break;
                    case "nvenc":
                        args.Append("-hwaccel cuda ");
                        break;
                    case "qsv":
                        args.Append("-hwaccel qsv ");
                        break;
                    case "vaapi":
                        args.Append("-hwaccel vaapi -hwaccel_device /dev/dri/renderD128 ");
                        break;
                    case "dxva2":
                        args.Append("-hwaccel dxva2 ");
                        break;
                    case "videotoolbox":
                        args.Append("-hwaccel videotoolbox ");
                        break;
                    case "auto":
                        args.Append("-hwaccel auto ");
                        break;
                }
            }

            // Input file
            args.Append($"-i \"{inputFile}\" ");

            // Overwrite output file if needed
            if (_settings.OverwriteOutput)
            {
                args.Append("-y ");
            }
            else
            {
                args.Append("-n ");
            }

            // Video codec
            if (!string.IsNullOrEmpty(_settings.VideoCodec) && _settings.VideoCodec != "copy")
            {
                var videoCodec = _settings.VideoCodec;
                
                // Handle hardware-accelerated encoders
                if (_settings.HardwareAcceleration)
                {
                    switch (_settings.HardwareAccelerationMethod.ToLower())
                    {
                        case "cuda":
                        case "nvenc":
                            if (videoCodec == "libx264") videoCodec = "h264_nvenc";
                            else if (videoCodec == "libx265") videoCodec = "hevc_nvenc";
                            break;
                        case "qsv":
                            if (videoCodec == "libx264") videoCodec = "h264_qsv";
                            else if (videoCodec == "libx265") videoCodec = "hevc_qsv";
                            break;
                        case "vaapi":
                            if (videoCodec == "libx264") videoCodec = "h264_vaapi";
                            else if (videoCodec == "libx265") videoCodec = "hevc_vaapi";
                            break;
                        case "videotoolbox":
                            if (videoCodec == "libx264") videoCodec = "h264_videotoolbox";
                            else if (videoCodec == "libx265") videoCodec = "hevc_videotoolbox";
                            break;
                    }
                }
                
                args.Append($"-c:v {videoCodec} ");
            }
            else if (_settings.VideoCodec == "copy")
            {
                args.Append("-c:v copy ");
            }

            // Audio codec
            if (!string.IsNullOrEmpty(_settings.AudioCodec) && _settings.AudioCodec != "copy")
            {
                args.Append($"-c:a {_settings.AudioCodec} ");
            }
            else if (_settings.AudioCodec == "copy")
            {
                args.Append("-c:a copy ");
            }

            // Thread count
            if (_settings.ThreadCount > 0)
            {
                args.Append($"-threads {_settings.ThreadCount} ");
            }

            // Log level
            var logLevels = new[] { "quiet", "panic", "fatal", "error", "warning", "info", "verbose", "debug" };
            if (_settings.LogLevel >= 0 && _settings.LogLevel < logLevels.Length)
            {
                args.Append($"-loglevel {logLevels[_settings.LogLevel]} ");
            }

            // Additional arguments
            if (!string.IsNullOrWhiteSpace(_settings.AdditionalArguments))
            {
                args.Append($"{_settings.AdditionalArguments} ");
            }

            // Output file
            args.Append($"\"{outputFile}\"");

            return args.ToString();
        }

        /// <summary>
        /// Processes a line of FFmpeg output to extract progress information.
        /// </summary>
        private void ProcessFFmpegOutputLine(string line, double totalDuration, Action<double>? progressCallback)
        {
            if (progressCallback == null || totalDuration <= 0)
                return;

            // Look for time progress in FFmpeg output
            var timeMatch = Regex.Match(line, @"time=(\d+):(\d+):(\d+)\.(\d+)");
            if (timeMatch.Success)
            {
                var hours = int.Parse(timeMatch.Groups[1].Value);
                var minutes = int.Parse(timeMatch.Groups[2].Value);
                var seconds = int.Parse(timeMatch.Groups[3].Value);
                var currentTime = hours * 3600 + minutes * 60 + seconds;
                
                var progress = (currentTime / totalDuration) * 100;
                progressCallback(Math.Min(progress, 100));
            }
        }

        /// <summary>
        /// Checks if FFmpeg is available at the configured path.
        /// </summary>
        public async Task<bool> IsFFmpegAvailableAsync()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        return false;

                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
