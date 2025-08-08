using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Linq;
using Batchbrake.Models;

namespace Batchbrake.Utilities
{
    public class HandbrakeCLIWrapper
    {
        private readonly string _handbrakeCLIPath;
        private readonly HandBrakeSettings _settings;
        private Process _currentProcess;
        private CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<ConversionProgressEventArgs> ProgressChanged;
        public event EventHandler<ConversionCompletedEventArgs> ConversionCompleted;

        public HandbrakeCLIWrapper(string handbrakeCLIPath = "handbrakecli")
        {
            _handbrakeCLIPath = handbrakeCLIPath;
            _settings = new HandBrakeSettings { HandBrakeCLIPath = handbrakeCLIPath };
        }

        public HandbrakeCLIWrapper(HandBrakeSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _handbrakeCLIPath = settings.HandBrakeCLIPath;
        }

        /// <summary>
        /// Converts a video using HandbrakeCLI with the specified options.
        /// </summary>
        /// <param name="inputFile">The path to the input video file.</param>
        /// <param name="outputFile">The path where the output video should be saved.</param>
        /// <param name="preset">The Handbrake preset to use.</param>
        /// <param name="additionalOptions">Any additional CLI options to pass to HandbrakeCLI.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task<bool> ConvertVideoAsync(string inputFile, string outputFile, string preset = null, string additionalOptions = null, CancellationToken cancellationToken = default)
        {
            var arguments = BuildHandBrakeArguments(inputFile, outputFile, preset, additionalOptions);
            return await ExecuteConversionAsync(arguments, cancellationToken);
        }

        /// <summary>
        /// Retrieves the list of available Handbrake presets.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary where keys are categories and values are lists of preset names.</returns>
        public async Task<Dictionary<string, List<string>>> GetAvailablePresetsAsync(bool importFromGui = true)
        {
            Exception? lastException = null;
            
            try
            {
                // First try to get presets with GUI import to include custom presets
                var output = await ExecuteHandbrakeCommandAsync(importFromGui ? "--preset-list --preset-import-gui" : "--preset-list");
                var presets = ParsePresetOutput(output);
                
                // If we got presets with custom presets included, return them
                if (presets.Any())
                {
                    return presets;
                }
            }
            catch (Exception ex)
            {
                // If importing GUI presets fails, fall back to built-in presets only
                System.Diagnostics.Debug.WriteLine("Failed to import GUI presets, falling back to built-in presets only");
                lastException = ex;
            }
            
            // Fallback: get built-in presets only
            try
            {
                var output = await ExecuteHandbrakeCommandAsync("--preset-list");
                return ParsePresetOutput(output);
            }
            catch (Exception ex)
            {
                // If both methods fail, throw the last exception
                throw lastException ?? ex;
            }
        }

        /// <summary>
        /// Parses the Handbrake CLI preset output to extract preset names and their categories.
        /// </summary>
        /// <param name="output">The output from the Handbrake CLI command.</param>
        /// <returns>A dictionary where keys are categories and values are lists of preset names.</returns>
        private Dictionary<string, List<string>> ParsePresetOutput(string output)
        {
            var presetsByCategory = new Dictionary<string, List<string>>();
            string currentCategory = null;

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Check if the line defines a category (ends with "/")
                if (trimmedLine.EndsWith("/"))
                {
                    currentCategory = trimmedLine.TrimEnd('/');
                    presetsByCategory[currentCategory] = new List<string>();
                }
                // Otherwise, if it's not empty and not a system message, it might be a preset
                else if (!string.IsNullOrEmpty(currentCategory) && !trimmedLine.StartsWith("[") && !trimmedLine.StartsWith("HandBrake") && line.StartsWith("   ") && !line.StartsWith("     "))
                {
                    // The first line under a category is the preset name, and subsequent lines are details we can ignore
                    presetsByCategory[currentCategory].Add(trimmedLine);
                }
            }

            return presetsByCategory;
        }

        /// <summary>
        /// Converts a video clip using HandbrakeCLI with specified start and end times.
        /// </summary>
        /// <param name="inputFile">The path to the input video file.</param>
        /// <param name="outputFile">The path where the output video should be saved.</param>
        /// <param name="startTime">Start time of the clip.</param>
        /// <param name="endTime">End time of the clip.</param>
        /// <param name="preset">The Handbrake preset to use.</param>
        /// <param name="additionalOptions">Any additional CLI options.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task<bool> ConvertVideoClipAsync(string inputFile, string outputFile, TimeSpan startTime, TimeSpan endTime, string preset = null, string additionalOptions = null, CancellationToken cancellationToken = default)
        {
            var arguments = BuildHandBrakeArguments(inputFile, outputFile, preset, additionalOptions);
            
            // Add start and stop time parameters for clip conversion
            var startSeconds = (int)startTime.TotalSeconds;
            var duration = (int)(endTime - startTime).TotalSeconds;
            arguments += $" --start-at seconds:{startSeconds} --stop-at seconds:{duration}";

            return await ExecuteConversionAsync(arguments, cancellationToken);
        }

        /// <summary>
        /// Builds HandBrake command-line arguments based on the current settings.
        /// </summary>
        private string BuildHandBrakeArguments(string inputFile, string outputFile, string preset = null, string additionalOptions = null)
        {
            var arguments = new StringBuilder();
            
            // Always import GUI presets to ensure custom presets are available
            arguments.Append("--preset-import-gui");
            
            arguments.AppendFormat(" -i \"{0}\" -o \"{1}\"", inputFile, outputFile);

            // Use preset if specified - this is the most reliable approach
            if (!string.IsNullOrWhiteSpace(preset))
            {
                arguments.AppendFormat(" -Z \"{0}\"", preset);
            }

            // Add additional options if specified (maintaining backward compatibility)
            if (!string.IsNullOrWhiteSpace(additionalOptions))
            {
                arguments.AppendFormat(" {0}", additionalOptions);
            }
            
            // Add user additional arguments from settings if specified
            if (!string.IsNullOrWhiteSpace(_settings.AdditionalArguments))
            {
                arguments.AppendFormat(" {0}", _settings.AdditionalArguments);
            }

            var commandLine = arguments.ToString();
            System.Diagnostics.Debug.WriteLine($"HandBrake Command: {_handbrakeCLIPath} {commandLine}");
            
            return commandLine;
        }

        /// <summary>
        /// Generates additional HandBrake arguments based on the current settings.
        /// This can be used to apply custom settings when presets are not sufficient.
        /// </summary>
        /// <returns>Additional arguments string that can be used with additionalOptions parameter</returns>
        public string GetAdditionalArgumentsFromSettings()
        {
            var args = new StringBuilder();
            
            // Only add non-default settings to avoid conflicts with presets
            
            // Quality setting (only if not default)
            if (_settings.QualityValue != 22)
            {
                args.AppendFormat(" -q {0}", _settings.QualityValue);
            }
            
            // Video encoder (only if not default)
            if (!string.IsNullOrEmpty(_settings.VideoEncoder) && _settings.VideoEncoder != "x264")
            {
                args.AppendFormat(" -e {0}", _settings.VideoEncoder);
            }
            
            // Two-pass encoding
            if (_settings.TwoPass)
            {
                args.Append(" -2");
                if (_settings.TurboFirstPass)
                {
                    args.Append(" -T");
                }
            }
            
            // Audio encoder (only if not default)
            if (!string.IsNullOrEmpty(_settings.AudioEncoder) && _settings.AudioEncoder != "av_aac")
            {
                args.AppendFormat(" -E {0}", _settings.AudioEncoder);
            }
            
            // Audio bitrate (only if not default)
            if (_settings.AudioBitrate != 160)
            {
                args.AppendFormat(" -B {0}", _settings.AudioBitrate);
            }
            
            return args.ToString().Trim();
        }

        /// <summary>
        /// Executes the conversion with progress tracking.
        /// </summary>
        private async Task<bool> ExecuteConversionAsync(string commandArguments, CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"Executing HandBrake: {_handbrakeCLIPath} {commandArguments}");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = _handbrakeCLIPath,
                Arguments = commandArguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (_currentProcess = new Process { StartInfo = startInfo })
            {
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();
                var lastProgress = -1.0;

                _currentProcess.OutputDataReceived += (sender, e) => 
                { 
                    if (e.Data != null) 
                    {
                        outputBuilder.AppendLine(e.Data);
                        System.Diagnostics.Debug.WriteLine($"HandBrake Output: {e.Data}");
                        
                        // Parse progress from HandBrake output
                        var progressMatch = Regex.Match(e.Data, @"Encoding: task \d+ of \d+, (\d+\.\d+) %");
                        if (progressMatch.Success && double.TryParse(progressMatch.Groups[1].Value, out var progress))
                        {
                            if (Math.Abs(progress - lastProgress) > 0.1) // Only report if changed significantly
                            {
                                lastProgress = progress;
                                ProgressChanged?.Invoke(this, new ConversionProgressEventArgs { Progress = progress });
                            }
                        }
                    }
                };
                
                _currentProcess.ErrorDataReceived += (sender, e) => 
                { 
                    if (e.Data != null) 
                    {
                        errorBuilder.AppendLine(e.Data);
                        System.Diagnostics.Debug.WriteLine($"HandBrake Error: {e.Data}");
                    }
                };

                try
                {
                    _currentProcess.Start();
                    _currentProcess.BeginOutputReadLine();
                    _currentProcess.BeginErrorReadLine();

                    await _currentProcess.WaitForExitAsync(_cancellationTokenSource.Token);

                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        if (!_currentProcess.HasExited)
                        {
                            _currentProcess.Kill();
                        }
                        ConversionCompleted?.Invoke(this, new ConversionCompletedEventArgs 
                        { 
                            Success = false, 
                            ErrorMessage = "Conversion was cancelled" 
                        });
                        return false;
                    }

                    var success = _currentProcess.ExitCode == 0;
                    System.Diagnostics.Debug.WriteLine($"HandBrake Exit Code: {_currentProcess.ExitCode}");
                    if (!success)
                    {
                        System.Diagnostics.Debug.WriteLine($"HandBrake Error Output: {errorBuilder}");
                        System.Diagnostics.Debug.WriteLine($"HandBrake Standard Output: {outputBuilder}");
                    }
                    
                    ConversionCompleted?.Invoke(this, new ConversionCompletedEventArgs 
                    { 
                        Success = success, 
                        ErrorMessage = success ? null : errorBuilder.ToString() 
                    });
                    
                    return success;
                }
                catch (Exception ex)
                {
                    ConversionCompleted?.Invoke(this, new ConversionCompletedEventArgs 
                    { 
                        Success = false, 
                        ErrorMessage = ex.Message 
                    });
                    return false;
                }
                finally
                {
                    _currentProcess = null;
                }
            }
        }

        /// <summary>
        /// Cancels the current conversion if one is in progress.
        /// </summary>
        public void CancelConversion()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Checks if HandBrakeCLI is available at the specified path.
        /// </summary>
        /// <returns>True if HandBrakeCLI is available, false otherwise.</returns>
        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var output = await ExecuteHandbrakeCommandAsync("--version");
                return output.Contains("HandBrake");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets format options supported by HandBrake.
        /// </summary>
        /// <returns>List of supported container formats.</returns>
        public List<string> GetSupportedFormats()
        {
            return new List<string> { "mp4", "mkv", "webm" };
        }

        /// <summary>
        /// Executes a custom HandbrakeCLI command and returns the output.
        /// </summary>
        /// <param name="commandArguments">The command-line arguments to pass to HandbrakeCLI.</param>
        /// <returns>A task that represents the asynchronous operation and contains the output of the command.</returns>
        public async Task<string> ExecuteHandbrakeCommandAsync(string commandArguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _handbrakeCLIPath,
                Arguments = commandArguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                // Combine output and error for logging or further analysis
                var output = outputBuilder.ToString() + errorBuilder.ToString();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"HandbrakeCLI exited with code {process.ExitCode}.\nError: {errorBuilder}");
                }

                // Return the output for further processing (like in GetAvailablePresetsAsync)
                return output;
            }
        }
    }

    public class ConversionProgressEventArgs : EventArgs
    {
        public double Progress { get; set; }
    }

    public class ConversionCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
