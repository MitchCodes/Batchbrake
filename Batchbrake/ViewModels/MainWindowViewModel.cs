using Avalonia.Input;
using Batchbrake.Models;
using Batchbrake.Utilities;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Batchbrake.Services;
using Avalonia.Platform.Storage;
using System.Linq;
using System;
using System.Reactive;
using System.IO;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Collections.Specialized;
using System.Threading;
using System.Text.Json;
using System.Reactive.Linq;

namespace Batchbrake.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private IFilePickerService _filePickerService;
        private IFFmpegWrapper _ffmpegWrapper;
        private CancellationTokenSource? _conversionCancellationTokenSource;
        private FFmpegSettings _ffmpegSettings = new FFmpegSettings();
        private HandBrakeSettings _handBrakeSettings = new HandBrakeSettings();
        private SessionManager _sessionManager;

        // Application preferences
        public Preferences Preferences { get; private set; } = new Preferences();

        private ObservableCollection<VideoModelViewModel> _videoQueue = new ObservableCollection<VideoModelViewModel>();
        public ObservableCollection<VideoModelViewModel> VideoQueue
        {
            get => _videoQueue;
            set
            {
                this.RaiseAndSetIfChanged(ref _videoQueue, value);

                // Assign indexes
                for (int i = 0; i < _videoQueue.Count; i++)
                {
                    _videoQueue[i].Index = i;
                }
            }
        }

        private ObservableCollection<string> _presets = new ObservableCollection<string>();
        public ObservableCollection<string> Presets
        {
            get => _presets;
            set
            {
                this.RaiseAndSetIfChanged(ref _presets, value);
            }
        }

        private int _parallelInstances = 1;
        public int ParallelInstances
        {
            get => _parallelInstances;
            set
            {
                this.RaiseAndSetIfChanged(ref _parallelInstances, value);
            }
        }

        private string? _defaultPreset;
        public string? DefaultPreset
        {
            get => _defaultPreset;
            set
            {
                this.RaiseAndSetIfChanged(ref _defaultPreset, value);
            }
        }

        private string _defaultOutputPath = "$(Folder)\\$(FileName)_conv.$(Ext)";
        public string DefaultOutputPath
        {
            get => _defaultOutputPath;
            set
            {
                this.RaiseAndSetIfChanged(ref _defaultOutputPath, value);
            }
        }

        private bool _isDraggingOver;
        public bool IsDraggingOver
        {
            get => _isDraggingOver;
            set => this.RaiseAndSetIfChanged(ref _isDraggingOver, value);
        }

        private bool _isConverting;
        public bool IsConverting
        {
            get => _isConverting;
            set
            {
                this.RaiseAndSetIfChanged(ref _isConverting, value);
                this.RaisePropertyChanged(nameof(CanStartConversion));
            }
        }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set => this.RaiseAndSetIfChanged(ref _statusText, value);
        }

        private string _logOutput = "";
        public string LogOutput
        {
            get => _logOutput;
            set
            {
                this.RaiseAndSetIfChanged(ref _logOutput, value);
                // Automatically trim log when it gets too long
                if (Preferences != null && Preferences.MaxLogLines > 0)
                {
                    var lines = _logOutput.Split('\n');
                    if (lines.Length > Preferences.MaxLogLines)
                    {
                        var trimmedLines = lines.Skip(lines.Length - Preferences.MaxLogLines).ToArray();
                        this.RaiseAndSetIfChanged(ref _logOutput, string.Join("\n", trimmedLines));
                    }
                }
            }
        }

        private bool _deleteSourceAfterConversion;
        public bool DeleteSourceAfterConversion
        {
            get => _deleteSourceAfterConversion;
            set => this.RaiseAndSetIfChanged(ref _deleteSourceAfterConversion, value);
        }

        private string _defaultOutputFormat = "mp4";
        public string DefaultOutputFormat
        {
            get => _defaultOutputFormat;
            set => this.RaiseAndSetIfChanged(ref _defaultOutputFormat, value);
        }

        public int QueueCount => VideoQueue?.Count ?? 0;
        
        public int ProcessingCount => VideoQueue?.Count(v => v.ConversionStatus == VideoConversionStatus.InProgress) ?? 0;
        
        public int CompletedCount => VideoQueue?.Count(v => v.ConversionStatus == VideoConversionStatus.Completed) ?? 0;

        public bool CanStartConversion => !IsConverting && VideoQueue?.Any() == true;

        public MainWindowViewModel()
        {
            _sessionManager = new SessionManager();
            VideoQueue.CollectionChanged += VideoQueue_CollectionChanged;
        }

        public MainWindowViewModel(IFilePickerService filePickerService) : this()
        {
            _filePickerService = filePickerService;
            _ffmpegWrapper = new FFmpegWrapper(_ffmpegSettings);

            // Load HandBrake settings first, then presets (which depend on the settings)
            Task.Run(async () =>
            {
                await LoadHandBrakeSettingsAsync();
                await LoadPresetsAsync();
            });
            Task.Run(LoadFFmpegSettingsAsync);
            Task.Run(LoadPreferencesAsync);
            Task.Run(LoadSessionAsync);
        }

        public MainWindowViewModel(IFilePickerService filePickerService, IFFmpegWrapper ffmpegWrapper) : this()
        {
            _filePickerService = filePickerService;
            _ffmpegWrapper = ffmpegWrapper;

            // Load HandBrake settings first, then presets (which depend on the settings)
            Task.Run(async () =>
            {
                await LoadHandBrakeSettingsAsync();
                await LoadPresetsAsync();
            });
            Task.Run(LoadFFmpegSettingsAsync);
            Task.Run(LoadPreferencesAsync);
            Task.Run(LoadSessionAsync);
        }

        private async Task LoadPresetsAsync()
        {
            try
            {
                // Debug: Show what custom preset files are configured
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Custom preset files configured: {(_handBrakeSettings.CustomPresetFiles?.Count ?? 0)}\n";
                    if (_handBrakeSettings.CustomPresetFiles != null)
                    {
                        foreach (var file in _handBrakeSettings.CustomPresetFiles)
                        {
                            LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: - {file} (exists: {System.IO.File.Exists(file)})\n";
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(_handBrakeSettings.CustomPresetFile))
                    {
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Legacy custom preset file: {_handBrakeSettings.CustomPresetFile} (exists: {System.IO.File.Exists(_handBrakeSettings.CustomPresetFile)})\n";
                    }
                });
                
                var handbrakeCliWrapper = new HandbrakeCLIWrapper(_handBrakeSettings);
                
                // Subscribe to debug messages
                handbrakeCliWrapper.DebugMessage += (sender, message) =>
                {
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: {message}\n";
                    });
                };
                
                var presets = await handbrakeCliWrapper.GetAvailablePresetsAsync();
                
                System.Diagnostics.Debug.WriteLine($"MainWindow LoadPresetsAsync: Received {presets.Sum(p => p.Value.Count)} presets from {presets.Count} categories");
                
                // Also log to UI for debugging
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Received {presets.Sum(p => p.Value.Count)} presets from {presets.Count} categories\n";
                });
                
                foreach (var cat in presets)
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow Category '{cat.Key}': {string.Join(", ", cat.Value)}");
                    
                    // Also log to UI for debugging (first few presets only to avoid spam)
                    if (cat.Value.Count > 0)
                    {
                        var presetSample = cat.Value.Take(3);
                        RxApp.MainThreadScheduler.Schedule(() =>
                        {
                            LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Category '{cat.Key}' has {cat.Value.Count} presets: {string.Join(", ", presetSample)}{(cat.Value.Count > 3 ? "..." : "")}\n";
                        });
                    }
                }

                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    Presets.Clear();
                    var totalPresetCount = 0;
                    var customPresetCount = 0;

                    foreach (var presetCategory in presets.Keys)
                    {
                        foreach (var preset in presets[presetCategory])
                        {
                            Presets.Add(preset);
                            totalPresetCount++;
                            
                            // Check if this is likely a custom preset (simple heuristic)
                            if (!presetCategory.Equals("General", StringComparison.OrdinalIgnoreCase) &&
                                !presetCategory.Equals("Web", StringComparison.OrdinalIgnoreCase) &&
                                !presetCategory.Equals("Devices", StringComparison.OrdinalIgnoreCase) &&
                                !presetCategory.Equals("Matroska", StringComparison.OrdinalIgnoreCase))
                            {
                                customPresetCount++;
                            }
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"MainWindow: Final preset collection has {Presets.Count} items");
                    
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Final preset collection has {Presets.Count} items\n";
                    if (Presets.Count > 0)
                    {
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: First few presets: {string.Join(", ", Presets.Take(5))}\n";
                    }

                    DefaultPreset = Presets.FirstOrDefault();
                    
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Loaded {totalPresetCount} HandBrake presets";
                    if (customPresetCount > 0)
                    {
                        LogOutput += $" (including {customPresetCount} custom presets)";
                    }
                    LogOutput += "\n";
                    
                    // Log available preset categories for debugging
                    if (presets.Keys.Any())
                    {
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] Preset categories: {string.Join(", ", presets.Keys)}\n";
                    }
                });
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Warning: Could not load HandBrake presets: {ex.Message}\n";
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Note: If you have custom presets in HandBrake GUI, make sure HandBrakeCLI can access them\n";
                    
                    // Add some default presets if HandBrake is not available
                    Presets.Clear();
                    Presets.Add("Fast 1080p30");
                    Presets.Add("HQ 1080p30 Surround");
                    Presets.Add("Super HQ 1080p30 Surround");
                    DefaultPreset = Presets.FirstOrDefault();
                    
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Using fallback built-in presets\n";
                });
            }
        }

        public async Task AddNewFile(string file)
        {
            if (VideoQueue.Contains(VideoQueue.FirstOrDefault(x => x.InputFilePath == file)))
            {
                return;
            }

            var videoInfo = await GetVideoInfoAsync(file); // Assume FFmpeg Wrapper provides this method

            // Resolve output file name based on default output path pattern from preferences
            string outputFolder = System.IO.Path.GetDirectoryName(file) ?? "";
            string resolvedOutputPath = (Preferences.DefaultOutputPath ?? DefaultOutputPath)
                .Replace("$(Folder)", outputFolder)
                .Replace("$(FileName)", System.IO.Path.GetFileNameWithoutExtension(file))
                .Replace("$(Ext)", Preferences.DefaultOutputFormat ?? "mp4");

            // Get the current count of items
            int currentIndex = VideoQueue.Count;

            var video = new VideoModelViewModel
            {
                Index = currentIndex,
                InputFilePath = file,
                VideoInfo = videoInfo,
                Preset = DefaultPreset ?? Presets.FirstOrDefault(),
                Presets = Presets,
                OutputFilePath = resolvedOutputPath,
                OutputFormat = Preferences.DefaultOutputFormat ?? "mp4"
            };

            // Check if queue was empty before adding this video (for auto-start)
            bool wasEmpty = VideoQueue.Count == 0;
            
            VideoQueue.Add(video);
            
            // Auto-save session after adding video
            _ = Task.Run(SaveSessionAsync);
            
            // Auto-start conversion if preference is enabled and queue was empty
            if (Preferences.AutoStartConversions && wasEmpty && !IsConverting)
            {
                LogOutput += $"[{DateTime.Now:HH:mm:ss}] Auto-starting conversions (queue was empty)\n";
                _ = StartConversionCommand.Execute().Subscribe();
            }
        }

        public void RemoveVideo(VideoModelViewModel video)
        {
            if (VideoQueue.Contains(video))
            {
                VideoQueue.Remove(video);
            }

            // Reassign indexes
            for (int i = 0; i < VideoQueue.Count; i++)
            {
                VideoQueue[i].Index = i;
            }
            
            // Auto-save session after removing video
            _ = Task.Run(SaveSessionAsync);
        }

        // Command to remove video - disabled when converting
        public ReactiveCommand<VideoModelViewModel, Unit> RemoveVideoCommand => ReactiveCommand.Create<VideoModelViewModel>(RemoveVideo, this.WhenAnyValue(x => x.IsConverting).Select(converting => !converting));

        public void OpenVideo(VideoModelViewModel video)
        {
            if (File.Exists(video.InputFilePath))
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = video.InputFilePath;
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
        }

        // Command to open video
        public ReactiveCommand<VideoModelViewModel, Unit> OpenVideoCommand => ReactiveCommand.Create<VideoModelViewModel>(OpenVideo);

        public void OpenVideoFolder(VideoModelViewModel video)
        {
            if (File.Exists(video.InputFilePath))
            {
                Process.Start("explorer.exe", $"/select,\"{video.InputFilePath.Replace("/",@"\")}\"");
            }
        }

        // Command to open video
        public ReactiveCommand<VideoModelViewModel, Unit> OpenVideoFolderCommand => ReactiveCommand.Create<VideoModelViewModel>(OpenVideoFolder);

        // Pause Conversion Command
        public ReactiveCommand<Unit, Unit> PauseConversionCommand => ReactiveCommand.Create(() =>
        {
            StatusText = "Conversion paused";
            LogOutput += $"[{DateTime.Now:HH:mm:ss}] Conversion paused by user\n";
        });

        // Stop Conversion Command
        public ReactiveCommand<Unit, Unit> StopConversionCommand => ReactiveCommand.Create(() =>
        {
            // Cancel the conversion process
            _conversionCancellationTokenSource?.Cancel();
            
            IsConverting = false;
            StatusText = "Conversion stopped";
            LogOutput += $"[{DateTime.Now:HH:mm:ss}] Conversion stopped by user\n";
            
            // Mark all in-progress videos as cancelled
            foreach (var video in VideoQueue.Where(v => v.ConversionStatus == VideoConversionStatus.InProgress))
            {
                video.ConversionStatus = VideoConversionStatus.Cancelled;
                video.ConversionProgress = 0;
            }
        });

        // Clear Queue Command - disabled when converting
        public ReactiveCommand<Unit, Unit> ClearQueueCommand => ReactiveCommand.Create(() =>
        {
            var videoCount = VideoQueue.Count;
            VideoQueue.Clear();
            LogOutput += $"[{DateTime.Now:HH:mm:ss}] Cleared {videoCount} videos from queue\n";
            
            // Auto-save session after clearing queue
            _ = Task.Run(SaveSessionAsync);
        }, this.WhenAnyValue(x => x.IsConverting).Select(converting => !converting));

        // Clear Completed Command - can be used while converting (only removes completed items)
        public ReactiveCommand<Unit, Unit> ClearCompletedCommand => ReactiveCommand.Create(() =>
        {
            var completedVideos = VideoQueue.Where(v => v.ConversionStatus == VideoConversionStatus.Completed).ToList();
            foreach (var video in completedVideos)
            {
                VideoQueue.Remove(video);
            }
            LogOutput += $"[{DateTime.Now:HH:mm:ss}] Cleared {completedVideos.Count} completed videos from queue\n";
            
            // Auto-save session after clearing completed videos
            _ = Task.Run(SaveSessionAsync);
        });

        // Open FFmpeg Settings Command
        public ReactiveCommand<Unit, Unit> OpenFFmpegSettingsCommand => ReactiveCommand.CreateFromTask(async () =>
        {
            var window = new FFmpegSettingsWindow();
            var viewModel = new FFmpegSettingsViewModel(_ffmpegSettings, _filePickerService, window);
            window.DataContext = viewModel;
            
            // Find the main window to set as owner
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (mainWindow?.MainWindow != null)
            {
                var result = await window.ShowDialog<bool>(mainWindow.MainWindow);
                if (result)
                {
                    // Settings were saved, save to file
                    await SaveFFmpegSettingsAsync();
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] FFmpeg settings updated\n";
                }
            }
        });

        // Open HandBrake Settings Command
        public ReactiveCommand<Unit, Unit> OpenHandBrakeSettingsCommand => ReactiveCommand.CreateFromTask(async () =>
        {
            var window = new HandBrakeSettingsWindow();
            var viewModel = new HandBrakeSettingsViewModel(_handBrakeSettings, _filePickerService, window);
            window.DataContext = viewModel;
            
            // Find the main window to set as owner
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (mainWindow?.MainWindow != null)
            {
                var result = await window.ShowDialog<bool>(mainWindow.MainWindow);
                if (result)
                {
                    // Settings were saved, save to file
                    await SaveHandBrakeSettingsAsync();
                    
                    // Reload presets with new settings (including custom preset files)
                    await LoadPresetsAsync();
                    
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] HandBrake settings updated and presets reloaded\n";
                }
            }
        });

        // Retrieve video info via FFmpeg wrapper
        private async Task<VideoInfoModel> GetVideoInfoAsync(string filePath)
        {
            // Call FFmpeg wrapper to get video information (e.g., duration, resolution, etc.)
            if (_ffmpegWrapper == null)
            {
                _ffmpegWrapper = new FFmpegWrapper(_ffmpegSettings);
            }
            return await _ffmpegWrapper.GetVideoInfoAsync(filePath);
        }

        // Start Conversion Command
        public ReactiveCommand<Unit, Unit> StartConversionCommand => ReactiveCommand.CreateFromTask(async () =>
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                LogOutput += $"[{DateTime.Now:HH:mm:ss}] Start Conversion button clicked\n";
            });
            // Initial UI update on main thread - check availability first
            if (_ffmpegSettings.UseAsConversionEngine)
            {
                if (!await IsFFmpegAvailableAsync())
                {
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        StatusText = "FFmpeg not found";
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] ERROR: FFmpeg not found at specified path\n";
                    });
                    return;
                }
            }
            else
            {
                if (!await IsHandbrakeCLIAvailableAsync())
                {
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        StatusText = "HandBrakeCLI not found";
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] ERROR: HandBrakeCLI not found at specified path\n";
                    });
                    return;
                }
            }

            // Create cancellation token for this conversion batch
            _conversionCancellationTokenSource?.Cancel();
            _conversionCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _conversionCancellationTokenSource.Token;

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                IsConverting = true;
                StatusText = "Starting conversion...";
                LogOutput += $"[{DateTime.Now:HH:mm:ss}] Starting batch conversion of {VideoQueue.Count} videos\n";
            });

            try
            {
                // Get videos to process (this can be done off UI thread)
                var videosToProcess = VideoQueue.Where(v => v.ConversionStatus == VideoConversionStatus.NotStarted || 
                                                          v.ConversionStatus == VideoConversionStatus.Queued).ToList();

                // Run the entire conversion process on background threads
                await Task.Run(async () =>
                {
                    using var semaphore = new System.Threading.SemaphoreSlim(ParallelInstances, ParallelInstances);
                    
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] Using {ParallelInstances} parallel conversion instance(s)\n";
                    });
                    
                    var tasks = videosToProcess.Select(async video =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            await ProcessVideo(video, cancellationToken);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }).ToArray();

                    await Task.WhenAll(tasks);
                }, cancellationToken);

                // Final UI update on main thread
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    IsConverting = false;
                    StatusText = $"Conversion completed. {CompletedCount} videos processed.";
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Batch conversion completed\n";
                });
            }
            catch (OperationCanceledException)
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    IsConverting = false;
                    StatusText = "Conversion cancelled";
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Batch conversion cancelled by user\n";
                });
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    IsConverting = false;
                    StatusText = "Conversion failed";
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Batch conversion failed: {ex.Message}\n";
                });
            }
        });

        private async Task ProcessVideo(VideoModelViewModel video, CancellationToken cancellationToken)
        {
            try
            {
                // Check for cancellation before starting
                cancellationToken.ThrowIfCancellationRequested();

                // Update UI on main thread
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    video.ConversionStatus = VideoConversionStatus.InProgress;
                    video.ConversionProgress = 0;
                    video.StartTime = DateTime.Now;
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Starting conversion of {video.VideoInfo?.FileName}\n";
                });
                
                var handbrakeCliWrapper = new HandbrakeCLIWrapper(_handBrakeSettings);
                
                // Subscribe to progress events with UI thread marshaling
                handbrakeCliWrapper.ProgressChanged += (sender, e) =>
                {
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        video.ConversionProgress = e.Progress;
                    });
                };
                
                handbrakeCliWrapper.ConversionCompleted += (sender, e) =>
                {
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        video.EndTime = DateTime.Now;
                        if (e.Success)
                        {
                            video.ConversionStatus = VideoConversionStatus.Completed;
                            video.ConversionProgress = 100;
                            LogOutput += $"[{DateTime.Now:HH:mm:ss}] Successfully converted {video.VideoInfo?.FileName}\n";
                            
                            // Show completion notification if enabled
                            if (Preferences.ShowCompletionNotifications)
                            {
                                ShowNotification($"Conversion Complete", $"Successfully converted {video.VideoInfo?.FileName}");
                            }
                            
                            if (Preferences.DeleteSourceAfterConversion && File.Exists(video.InputFilePath))
                            {
                                try
                                {
                                    File.Delete(video.InputFilePath);
                                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Deleted source file {video.VideoInfo?.FileName}\n";
                                }
                                catch (Exception deleteEx)
                                {
                                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to delete source file: {deleteEx.Message}\n";
                                }
                            }
                            
                            // Auto-save session after successful conversion
                            _ = Task.Run(SaveSessionAsync);
                        }
                        else
                        {
                            video.ConversionStatus = VideoConversionStatus.Failed;
                            video.ErrorMessage = e.ErrorMessage;
                            LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to convert {video.VideoInfo?.FileName}: {e.ErrorMessage}\n";
                            
                            // Auto-save session after failed conversion
                            _ = Task.Run(SaveSessionAsync);
                        }
                    });
                };
                
                // Choose conversion engine based on settings
                bool conversionResult;
                if (_ffmpegSettings.UseAsConversionEngine)
                {
                    // Use FFmpeg for conversion
                    var ffmpegWrapper = new FFmpegWrapper(_ffmpegSettings);
                    conversionResult = await ffmpegWrapper.ConvertVideoAsync(
                        video.InputFilePath!,
                        video.OutputFilePath!,
                        progress => {
                            RxApp.MainThreadScheduler.Schedule(() => {
                                video.ConversionProgress = (int)progress;
                            });
                        },
                        cancellationToken);
                    
                    // Handle FFmpeg completion
                    if (conversionResult)
                    {
                        RxApp.MainThreadScheduler.Schedule(() =>
                        {
                            video.ConversionStatus = VideoConversionStatus.Completed;
                            video.ConversionProgress = 100;
                            video.EndTime = DateTime.Now;
                            LogOutput += $"[{DateTime.Now:HH:mm:ss}] Successfully converted {video.VideoInfo?.FileName} using FFmpeg\n";
                            
                            // Show completion notification if enabled
                            if (Preferences.ShowCompletionNotifications)
                            {
                                ShowNotification($"Conversion Complete", $"Successfully converted {video.VideoInfo?.FileName}");
                            }
                            
                            if (Preferences.DeleteSourceAfterConversion && File.Exists(video.InputFilePath))
                            {
                                try
                                {
                                    File.Delete(video.InputFilePath);
                                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Deleted source file {video.VideoInfo?.FileName}\n";
                                }
                                catch (Exception deleteEx)
                                {
                                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to delete source file: {deleteEx.Message}\n";
                                }
                            }
                            
                            // Auto-save session after successful FFmpeg conversion
                            _ = Task.Run(SaveSessionAsync);
                        });
                    }
                }
                else
                {
                    // Use HandBrake for conversion with custom settings applied
                    var additionalArgs = handbrakeCliWrapper.GetAdditionalArgumentsFromSettings();
                    conversionResult = await handbrakeCliWrapper.ConvertVideoAsync(
                        video.InputFilePath!, 
                        video.OutputFilePath!, 
                        video.Preset,
                        additionalArgs,
                        cancellationToken);
                }

                if (!conversionResult)
                {
                    throw new Exception("Conversion failed");
                }
            }
            catch (OperationCanceledException)
            {
                // Update UI on main thread for cancellation
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    video.ConversionStatus = VideoConversionStatus.Cancelled;
                    video.ConversionProgress = 0;
                    video.EndTime = DateTime.Now;
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Conversion cancelled for {video.VideoInfo?.FileName}\n";
                    
                    // Auto-save session after cancellation
                    _ = Task.Run(SaveSessionAsync);
                });
                throw; // Re-throw to propagate cancellation
            }
            catch (Exception ex)
            {
                // Update UI on main thread for error handling
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    video.ConversionStatus = VideoConversionStatus.Failed;
                    video.ErrorMessage = ex.Message;
                    video.EndTime = DateTime.Now;
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Error processing {video.VideoInfo?.FileName}: {ex.Message}\n";
                    
                    // Auto-save session after error
                    _ = Task.Run(SaveSessionAsync);
                });
            }
        }

        // Auto-detect HandbrakeCLI
        private async Task<bool> IsHandbrakeCLIAvailableAsync()
        {
            try
            {
                var handbrakeWrapper = new HandbrakeCLIWrapper(_handBrakeSettings);
                return await handbrakeWrapper.IsAvailableAsync();
            }
            catch
            {
                return false;
            }
        }

        // Auto-detect FFmpeg
        private async Task<bool> IsFFmpegAvailableAsync()
        {
            try
            {
                var ffmpegWrapper = new FFmpegWrapper(_ffmpegSettings);
                return await ffmpegWrapper.IsFFmpegAvailableAsync();
            }
            catch
            {
                return false;
            }
        }

        // Add Videos Command - disabled when converting
        public ReactiveCommand<Unit, Unit> AddVideosCommand => ReactiveCommand.CreateFromTask(async () =>
        {
            // Start async operation to open the dialog.
            var files = await _filePickerService.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                FileTypeFilter = new List<FilePickerFileType>() {
                    new FilePickerFileType("Videos") { Patterns = new List<string> { "*.mp4", "*.mkv", "*.avi", "*.mov", "*.wmv", "*.flv", "*.webm", "*.m4v", "*.mpg", "*.mpeg" } }
                },
                Title = "Select Videos",
                AllowMultiple = true
            });

            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (file != null)
                    {
                        await AddNewFile(Uri.UnescapeDataString(file.Path.AbsolutePath));
                    }
                }
                
                LogOutput += $"[{DateTime.Now:HH:mm:ss}] Added {files.Count} video(s) to queue\n";
            }
        }, this.WhenAnyValue(x => x.IsConverting).Select(converting => !converting));

        // Help -> About Command
        public ReactiveCommand<Unit, Unit> ShowAboutCommand => ReactiveCommand.CreateFromTask(async () =>
        {
            var aboutDialog = new AboutWindow();
            
            // Find the main window to set as owner
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (mainWindow?.MainWindow != null)
            {
                await aboutDialog.ShowDialog(mainWindow.MainWindow);
            }
        });

        // Help -> Custom Presets Command
        public ReactiveCommand<Unit, Unit> ShowCustomPresetsHelpCommand => ReactiveCommand.CreateFromTask(async () =>
        {
            var helpDialog = new CustomPresetsHelpWindow();
            
            // Find the main window to set as owner
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (mainWindow?.MainWindow != null)
            {
                await helpDialog.ShowDialog(mainWindow.MainWindow);
            }
        });

        // File -> Exit Command
        public ReactiveCommand<Unit, Unit> ExitApplicationCommand => ReactiveCommand.Create(() =>
        {
            var appLifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            appLifetime?.Shutdown();
        });

        // Tools -> Preferences Command
        public ReactiveCommand<Unit, Unit> OpenPreferencesCommand => ReactiveCommand.CreateFromTask(async () =>
        {
            PreferencesWindow? currentPreferencesWindow = null;
            
            var preferencesViewModel = new PreferencesViewModel(Preferences, 
                onSave: (prefs) =>
                {
                    // Apply preferences immediately
                    ApplyPreferences(prefs);
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Preferences updated\n";
                    
                    // Save preferences to file
                    _ = Task.Run(SavePreferencesAsync);
                    
                    // Close the dialog
                    currentPreferencesWindow?.Close();
                },
                onCancel: () =>
                {
                    // Close the dialog without saving
                    currentPreferencesWindow?.Close();
                });

            currentPreferencesWindow = new PreferencesWindow(preferencesViewModel);
            
            // Find the main window to set as owner
            var appLifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (appLifetime?.MainWindow != null)
            {
                await currentPreferencesWindow.ShowDialog(appLifetime.MainWindow);
            }
        });

        private void VideoQueue_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Reassign indexes whenever the collection changes
            for (int i = 0; i < VideoQueue.Count; i++)
            {
                VideoQueue[i].Index = i;
            }

            // Update count properties
            this.RaisePropertyChanged(nameof(QueueCount));
            this.RaisePropertyChanged(nameof(ProcessingCount));
            this.RaisePropertyChanged(nameof(CompletedCount));
            this.RaisePropertyChanged(nameof(CanStartConversion));
        }

        private async Task LoadHandBrakeSettingsAsync()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Batchbrake",
                    "handbrake-settings.json"
                );

                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Loading HandBrake settings from: {settingsPath}\n";
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Settings file exists: {File.Exists(settingsPath)}\n";
                });

                if (File.Exists(settingsPath))
                {
                    var json = await File.ReadAllTextAsync(settingsPath);
                    
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Settings JSON length: {json.Length}\n";
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Settings JSON preview: {json.Substring(0, Math.Min(200, json.Length))}...\n";
                    });
                    
                    var settings = JsonSerializer.Deserialize<HandBrakeSettings>(json);
                    if (settings != null)
                    {
                        RxApp.MainThreadScheduler.Schedule(() =>
                        {
                            LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Deserialized settings - CustomPresetFiles count: {settings.CustomPresetFiles?.Count ?? 0}\n";
                            LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Deserialized settings - CustomPresetFile (legacy): '{settings.CustomPresetFile}'\n";
                        });
                        
                        _handBrakeSettings = settings;
                    }
                    else
                    {
                        RxApp.MainThreadScheduler.Schedule(() =>
                        {
                            LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Failed to deserialize settings - settings is null\n";
                        });
                    }
                }
                else
                {
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Settings file does not exist, using default settings\n";
                    });
                }
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to load HandBrake settings: {ex.Message}\n";
                });
            }
        }

        private async Task SaveHandBrakeSettingsAsync()
        {
            try
            {
                var settingsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Batchbrake"
                );
                
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }

                var settingsPath = Path.Combine(settingsDir, "handbrake-settings.json");
                
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Saving HandBrake settings to: {settingsPath}\n";
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: CustomPresetFiles count before save: {_handBrakeSettings.CustomPresetFiles?.Count ?? 0}\n";
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: CustomPresetFile (legacy) before save: '{_handBrakeSettings.CustomPresetFile}'\n";
                });
                
                var json = JsonSerializer.Serialize(_handBrakeSettings, new JsonSerializerOptions { WriteIndented = true });
                
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Serialized JSON length: {json.Length}\n";
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] DEBUG: Serialized JSON preview: {json.Substring(0, Math.Min(300, json.Length))}...\n";
                });
                await File.WriteAllTextAsync(settingsPath, json);
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to save HandBrake settings: {ex.Message}\n";
                });
            }
        }

        private async Task LoadFFmpegSettingsAsync()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Batchbrake",
                    "ffmpeg-settings.json"
                );

                if (File.Exists(settingsPath))
                {
                    var json = await File.ReadAllTextAsync(settingsPath);
                    var settings = JsonSerializer.Deserialize<FFmpegSettings>(json);
                    if (settings != null)
                    {
                        _ffmpegSettings = settings;
                    }
                }
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to load FFmpeg settings: {ex.Message}\n";
                });
            }
        }

        private async Task SaveFFmpegSettingsAsync()
        {
            try
            {
                var settingsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Batchbrake"
                );
                
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }

                var settingsPath = Path.Combine(settingsDir, "ffmpeg-settings.json");
                var json = JsonSerializer.Serialize(_ffmpegSettings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(settingsPath, json);
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to save FFmpeg settings: {ex.Message}\n";
                });
            }
        }

        private async Task LoadSessionAsync()
        {
            try
            {
                var sessionData = await _sessionManager.LoadSessionAsync();
                if (sessionData != null)
                {
                    RxApp.MainThreadScheduler.Schedule(async () =>
                    {
                        await _sessionManager.ApplySessionToViewModelAsync(sessionData, this);
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] Session loaded with {sessionData.Videos.Count} videos\n";
                    });
                }
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to load session: {ex.Message}\n";
                });
            }
        }

        private async Task SaveSessionAsync()
        {
            try
            {
                await _sessionManager.SaveSessionAsync(this);
            }
            catch (Exception ex)
            {
                // Log error but don't disrupt user experience
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to save session: {ex.Message}\n";
                });
            }
        }

        private async Task LoadPreferencesAsync()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Batchbrake",
                    "preferences.json"
                );
                
                if (File.Exists(settingsPath))
                {
                    var json = await File.ReadAllTextAsync(settingsPath);
                    var preferences = JsonSerializer.Deserialize<Preferences>(json);
                    if (preferences != null)
                    {
                        Preferences = preferences;
                        ApplyPreferences(Preferences);
                    }
                }
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to load preferences: {ex.Message}\n";
                });
            }
        }

        private async Task SavePreferencesAsync()
        {
            try
            {
                var settingsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Batchbrake"
                );
                
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }

                var settingsPath = Path.Combine(settingsDir, "preferences.json");
                var json = JsonSerializer.Serialize(Preferences, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(settingsPath, json);
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to save preferences: {ex.Message}\n";
                });
            }
        }

        private void ApplyPreferences(Preferences preferences)
        {
            // Apply preferences that affect the current UI state
            if (preferences.DefaultParallelInstances >= 1 && preferences.DefaultParallelInstances <= 10)
            {
                ParallelInstances = preferences.DefaultParallelInstances;
            }

            // Set up auto-save timer if enabled
            SetupAutoSaveTimer();

            // Apply log verbosity settings
            // This will be used when logging messages
            // LogVerbosity is checked in logging methods
            
            // Apply maximum log lines limit
            TrimLogOutput();
        }

        private System.Threading.Timer? _autoSaveTimer;
        
        private void SetupAutoSaveTimer()
        {
            // Dispose existing timer if any
            _autoSaveTimer?.Dispose();
            _autoSaveTimer = null;

            if (Preferences.AutoSaveSession)
            {
                var interval = TimeSpan.FromMinutes(Preferences.AutoSaveIntervalMinutes);
                _autoSaveTimer = new System.Threading.Timer(async _ =>
                {
                    await SaveSessionAsync();
                }, null, interval, interval);
            }
        }

        private void TrimLogOutput()
        {
            if (Preferences.MaxLogLines <= 0) return;

            var lines = LogOutput.Split('\n');
            if (lines.Length > Preferences.MaxLogLines)
            {
                var trimmedLines = lines.Skip(lines.Length - Preferences.MaxLogLines).ToArray();
                LogOutput = string.Join("\n", trimmedLines);
            }
        }

        private void ShowNotification(string title, string message)
        {
            // Note: Full system notification implementation would require platform-specific code
            // For now, just log it
            LogOutput += $"[{DateTime.Now:HH:mm:ss}] NOTIFICATION: {title} - {message}\n";
            
            // TODO: Implement actual system notifications using:
            // - Windows: Windows.UI.Notifications or System.Windows.Forms.NotifyIcon
            // - macOS: NSUserNotification
            // - Linux: libnotify
        }

        private void LogMessage(string message, int requiredVerbosity = 1)
        {
            // Only log if verbosity level is high enough
            if (Preferences.LogVerbosity >= requiredVerbosity)
            {
                LogOutput += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            }
        }

        private void LogVerbose(string message)
        {
            LogMessage(message, 2); // Only shows in Detailed mode
        }

        private void LogNormal(string message)
        {
            LogMessage(message, 1); // Shows in Normal and Detailed modes
        }

        private void LogMinimal(string message)
        {
            LogMessage(message, 0); // Always shows
        }

        public void Dispose()
        {
            _conversionCancellationTokenSource?.Cancel();
            _conversionCancellationTokenSource?.Dispose();
            _autoSaveTimer?.Dispose();
        }
    }
}
