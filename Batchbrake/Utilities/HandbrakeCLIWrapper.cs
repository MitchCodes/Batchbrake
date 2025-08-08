using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text.Json;
using Batchbrake.Models;

namespace Batchbrake.Utilities
{
    public class HandbrakeCLIWrapper
    {
        private readonly string _handbrakeCLIPath;
        private readonly HandBrakeSettings _settings;
        private Process _currentProcess;
        private CancellationTokenSource _cancellationTokenSource;
        
        public event EventHandler<string> DebugMessage;

        public event EventHandler<ConversionProgressEventArgs> ProgressChanged;
        public event EventHandler<ConversionCompletedEventArgs> ConversionCompleted;

        public HandbrakeCLIWrapper(string handbrakeCLIPath = "handbrakecli")
        {
            _handbrakeCLIPath = handbrakeCLIPath;
            _settings = new HandBrakeSettings { HandBrakeCLIPath = handbrakeCLIPath };
            
            // Debug output for string constructor
            System.Diagnostics.Debug.WriteLine($"HandbrakeCLIWrapper string constructor: Using default settings with no custom presets");
            Console.WriteLine($"[HandBrakeCLI] String Constructor: Using default settings with no custom presets");
        }

        public HandbrakeCLIWrapper(HandBrakeSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _handbrakeCLIPath = settings.HandBrakeCLIPath;
            
            // Debug output for constructor
            System.Diagnostics.Debug.WriteLine($"HandbrakeCLIWrapper constructor: CustomPresetFiles = {(_settings.CustomPresetFiles == null ? "null" : _settings.CustomPresetFiles.Count.ToString())}");
            Console.WriteLine($"[HandBrakeCLI] Constructor: CustomPresetFiles = {(_settings.CustomPresetFiles == null ? "null" : _settings.CustomPresetFiles.Count.ToString())}");
            
            if (_settings.CustomPresetFiles != null)
            {
                foreach (var file in _settings.CustomPresetFiles)
                {
                    System.Diagnostics.Debug.WriteLine($"HandbrakeCLIWrapper constructor: File = {file}");
                    Console.WriteLine($"[HandBrakeCLI] Constructor: File = {file}");
                }
            }
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
                string command;
                
                // Check for multiple custom preset files first
                var validPresetFiles = new List<string>();
                
                System.Diagnostics.Debug.WriteLine($"DEBUG: Checking custom preset files...");
                Console.WriteLine($"[HandBrakeCLI] DEBUG: Checking custom preset files...");
                
                System.Diagnostics.Debug.WriteLine($"DEBUG: _settings.CustomPresetFiles is null: {_settings.CustomPresetFiles == null}");
                Console.WriteLine($"[HandBrakeCLI] DEBUG: _settings.CustomPresetFiles is null: {_settings.CustomPresetFiles == null}");
                
                if (_settings.CustomPresetFiles != null)
                {
                    System.Diagnostics.Debug.WriteLine($"DEBUG: _settings.CustomPresetFiles count: {_settings.CustomPresetFiles.Count}");
                    Console.WriteLine($"[HandBrakeCLI] DEBUG: _settings.CustomPresetFiles count: {_settings.CustomPresetFiles.Count}");
                    
                    foreach (var file in _settings.CustomPresetFiles)
                    {
                        var exists = File.Exists(file);
                        System.Diagnostics.Debug.WriteLine($"DEBUG: Checking file: '{file}', exists: {exists}");
                        Console.WriteLine($"[HandBrakeCLI] DEBUG: Checking file: '{file}', exists: {exists}");
                    }
                    validPresetFiles.AddRange(_settings.CustomPresetFiles.Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f)));
                }
                
                var legacyExists = !string.IsNullOrWhiteSpace(_settings.CustomPresetFile) ? File.Exists(_settings.CustomPresetFile) : false;
                System.Diagnostics.Debug.WriteLine($"DEBUG: _settings.CustomPresetFile: '{_settings.CustomPresetFile}', exists: {(legacyExists ? "True" : "False/N/A")}");
                Console.WriteLine($"[HandBrakeCLI] DEBUG: _settings.CustomPresetFile: '{_settings.CustomPresetFile}', exists: {(legacyExists ? "True" : "False/N/A")}");
                
                // Add legacy single file for backward compatibility
                if (!string.IsNullOrWhiteSpace(_settings.CustomPresetFile) && File.Exists(_settings.CustomPresetFile))
                {
                    if (!validPresetFiles.Contains(_settings.CustomPresetFile))
                    {
                        validPresetFiles.Add(_settings.CustomPresetFile);
                        Console.WriteLine($"[HandBrakeCLI] DEBUG: Added legacy file to valid list");
                    }
                    else
                    {
                        Console.WriteLine($"[HandBrakeCLI] DEBUG: Legacy file already in valid list");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"DEBUG: Total valid preset files found: {validPresetFiles.Count}");
                
                // Also log to console for debugging visibility
                Console.WriteLine($"[HandBrakeCLI] DEBUG: Total valid preset files found: {validPresetFiles.Count}");
                DebugMessage?.Invoke(this, $"HandBrakeCLI: Total valid preset files found: {validPresetFiles.Count}");
                
                foreach (var file in validPresetFiles)
                {
                    Console.WriteLine($"[HandBrakeCLI] DEBUG: Valid file: {file}");
                    DebugMessage?.Invoke(this, $"HandBrakeCLI: Valid preset file: {file}");
                }
                
                Dictionary<string, List<string>> allPresets = new Dictionary<string, List<string>>();
                
                if (validPresetFiles.Any())
                {
                    // Import preset files one at a time and combine results
                    System.Diagnostics.Debug.WriteLine($"Loading presets from {validPresetFiles.Count} custom file(s): {string.Join(", ", validPresetFiles)}");
                    Console.WriteLine($"[HandBrakeCLI] Loading presets from {validPresetFiles.Count} custom file(s)");
                    DebugMessage?.Invoke(this, $"HandBrakeCLI: Loading presets from {validPresetFiles.Count} custom file(s)");
                    
                    foreach (var presetFile in validPresetFiles)
                    {
                        try
                        {
                            DebugMessage?.Invoke(this, $"HandBrakeCLI: Processing custom preset file: {presetFile}");
                            
                            // Try to directly parse the JSON preset file
                            var customPresets = await ParseCustomPresetFileAsync(presetFile);
                            DebugMessage?.Invoke(this, $"HandBrakeCLI: Found {customPresets.Count} custom presets in JSON file");
                            
                            if (customPresets.Any())
                            {
                                // Add custom presets to a special category
                                var customCategoryName = "Custom";
                                if (!allPresets.ContainsKey(customCategoryName))
                                {
                                    allPresets[customCategoryName] = new List<string>();
                                }
                                
                                foreach (var preset in customPresets)
                                {
                                    if (!allPresets[customCategoryName].Contains(preset))
                                    {
                                        allPresets[customCategoryName].Add(preset);
                                        DebugMessage?.Invoke(this, $"HandBrakeCLI: Added custom preset: {preset}");
                                    }
                                }
                            }
                            else
                            {
                                DebugMessage?.Invoke(this, $"HandBrakeCLI: No presets found in JSON file, trying HandBrake CLI method");
                                
                                // Fallback to original HandBrake CLI method
                                command = $"--preset-import-file \"{presetFile}\" --preset-list";
                                System.Diagnostics.Debug.WriteLine($"HandBrake command for file {presetFile}: {command}");
                                DebugMessage?.Invoke(this, $"HandBrakeCLI: Executing command: {command}");
                                
                                var fileOutput = await ExecuteHandbrakeCommandAsync(command);
                                System.Diagnostics.Debug.WriteLine($"HandBrake output length for {presetFile}: {fileOutput.Length}");
                                Console.WriteLine($"[HandBrakeCLI] DEBUG: HandBrake output length for {presetFile}: {fileOutput.Length}");
                                DebugMessage?.Invoke(this, $"HandBrakeCLI: Command output length: {fileOutput.Length}");
                                
                                var filePresets = ParsePresetOutput(fileOutput);
                                System.Diagnostics.Debug.WriteLine($"Parsed {filePresets.Sum(p => p.Value.Count)} presets from {filePresets.Count} categories in {presetFile}");
                                Console.WriteLine($"[HandBrakeCLI] DEBUG: Parsed {filePresets.Sum(p => p.Value.Count)} presets from {filePresets.Count} categories in {presetFile}");
                                DebugMessage?.Invoke(this, $"HandBrakeCLI: Parsed {filePresets.Sum(p => p.Value.Count)} presets from {filePresets.Count} categories");
                                
                                // Merge presets from HandBrake CLI output
                                foreach (var category in filePresets)
                                {
                                    if (!allPresets.ContainsKey(category.Key))
                                    {
                                        allPresets[category.Key] = new List<string>();
                                    }
                                    
                                    foreach (var preset in category.Value)
                                    {
                                        if (!allPresets[category.Key].Contains(preset))
                                        {
                                            allPresets[category.Key].Add(preset);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception fileEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error loading presets from {presetFile}: {fileEx.Message}");
                            DebugMessage?.Invoke(this, $"HandBrakeCLI: Error loading presets from {presetFile}: {fileEx.Message}");
                        }
                    }
                }
                
                // Always load built-in presets and merge with custom presets
                Console.WriteLine($"[HandBrakeCLI] DEBUG: After custom preset processing, allPresets.Count = {allPresets.Count}");
                DebugMessage?.Invoke(this, $"HandBrakeCLI: After custom preset processing, found {allPresets.Count} preset categories");
                
                // Load built-in presets regardless of whether we have custom presets
                Console.WriteLine($"[HandBrakeCLI] DEBUG: Loading built-in presets");
                DebugMessage?.Invoke(this, $"HandBrakeCLI: Loading built-in presets");
                
                if (importFromGui)
                {
                    command = "--preset-list --preset-import-gui";
                    System.Diagnostics.Debug.WriteLine("Attempting to import GUI presets");
                }
                else
                {
                    command = "--preset-list";
                    System.Diagnostics.Debug.WriteLine("Using built-in presets only");
                }
                
                var output = await ExecuteHandbrakeCommandAsync(command);
                System.Diagnostics.Debug.WriteLine($"HandBrake command: {command}");
                System.Diagnostics.Debug.WriteLine($"HandBrake output length: {output.Length}");
                System.Diagnostics.Debug.WriteLine($"HandBrake output preview: {output.Substring(0, Math.Min(500, output.Length))}");
                
                var builtInPresets = ParsePresetOutput(output);
                System.Diagnostics.Debug.WriteLine($"Parsed {builtInPresets.Sum(p => p.Value.Count)} built-in presets from {builtInPresets.Count} categories");
                DebugMessage?.Invoke(this, $"HandBrakeCLI: Parsed {builtInPresets.Sum(p => p.Value.Count)} built-in presets from {builtInPresets.Count} categories");
                
                // Merge built-in presets with custom presets
                foreach (var category in builtInPresets)
                {
                    if (!allPresets.ContainsKey(category.Key))
                    {
                        allPresets[category.Key] = new List<string>();
                    }
                    
                    foreach (var preset in category.Value)
                    {
                        if (!allPresets[category.Key].Contains(preset))
                        {
                            allPresets[category.Key].Add(preset);
                        }
                    }
                }
                
                // Return merged presets
                DebugMessage?.Invoke(this, $"HandBrakeCLI: Final merged presets: {allPresets.Sum(p => p.Value.Count)} presets from {allPresets.Count} categories");
                if (allPresets.Any())
                {
                    return allPresets;
                }
            }
            catch (Exception ex)
            {
                // If importing custom/GUI presets fails, fall back to built-in presets only
                System.Diagnostics.Debug.WriteLine($"Failed to import presets: {ex.Message}");
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
        /// Parses a custom preset JSON file to extract preset names.
        /// </summary>
        /// <param name="presetFilePath">Path to the JSON preset file.</param>
        /// <returns>A list of preset names found in the file.</returns>
        private async Task<List<string>> ParseCustomPresetFileAsync(string presetFilePath)
        {
            var presetNames = new List<string>();
            
            try
            {
                var json = await File.ReadAllTextAsync(presetFilePath);
                DebugMessage?.Invoke(this, $"HandBrakeCLI: Read JSON file, length: {json.Length}");
                
                using (var doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty("PresetList", out var presetList))
                    {
                        DebugMessage?.Invoke(this, $"HandBrakeCLI: Found PresetList array");
                        
                        foreach (var preset in presetList.EnumerateArray())
                        {
                            if (preset.TryGetProperty("PresetName", out var nameProperty))
                            {
                                var presetName = nameProperty.GetString();
                                if (!string.IsNullOrWhiteSpace(presetName))
                                {
                                    presetNames.Add(presetName);
                                    DebugMessage?.Invoke(this, $"HandBrakeCLI: Found preset name: {presetName}");
                                }
                            }
                        }
                    }
                    else
                    {
                        DebugMessage?.Invoke(this, $"HandBrakeCLI: No PresetList found in JSON");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugMessage?.Invoke(this, $"HandBrakeCLI: Error parsing JSON file: {ex.Message}");
            }
            
            return presetNames;
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
            
            // Import custom preset files if specified
            var validPresetFiles = new List<string>();
            
            // Add files from the new list
            if (_settings.CustomPresetFiles != null)
            {
                validPresetFiles.AddRange(_settings.CustomPresetFiles.Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f)));
            }
            
            // Add legacy single file for backward compatibility
            if (!string.IsNullOrWhiteSpace(_settings.CustomPresetFile) && File.Exists(_settings.CustomPresetFile))
            {
                if (!validPresetFiles.Contains(_settings.CustomPresetFile))
                {
                    validPresetFiles.Add(_settings.CustomPresetFile);
                }
            }
            
            if (validPresetFiles.Any())
            {
                // Import multiple preset files
                foreach (var presetFile in validPresetFiles)
                {
                    arguments.AppendFormat("--preset-import-file \"{0}\" ", presetFile);
                }
            }
            else
            {
                // Otherwise try to import GUI presets
                arguments.Append("--preset-import-gui ");
            }
            
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
