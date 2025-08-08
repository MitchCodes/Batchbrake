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
using Avalonia.Controls.Shapes;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Collections.Specialized;

namespace Batchbrake.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private IFilePickerService _filePickerService;

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
            set => this.RaiseAndSetIfChanged(ref _logOutput, value);
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
            VideoQueue.CollectionChanged += VideoQueue_CollectionChanged;
        }

        public MainWindowViewModel(IFilePickerService filePickerService) : this()
        {
            _filePickerService = filePickerService;

            RxApp.MainThreadScheduler.Schedule(LoadPresets);
        }

        private async void LoadPresets()
        {
            var handbrakeCliWrapper = new HandbrakeCLIWrapper();
            var presets = await handbrakeCliWrapper.GetAvailablePresetsAsync();

            Presets.Clear();

            foreach (var presetCategory in presets.Keys)
            {
                foreach (var preset in presets[presetCategory])
                {
                    Presets.Add(preset);
                }
            }

            DefaultPreset = Presets.FirstOrDefault();
        }

        public async Task AddNewFile(string file)
        {
            if (VideoQueue.Contains(VideoQueue.FirstOrDefault(x => x.InputFilePath == file)))
            {
                return;
            }

            var videoInfo = await GetVideoInfoAsync(file); // Assume FFmpeg Wrapper provides this method

            // Resolve output file name based on default output path pattern
            string outputFolder = System.IO.Path.GetDirectoryName(file);
            string resolvedOutputPath = DefaultOutputPath
                .Replace("$(Folder)", outputFolder)
                .Replace("$(FileName)", System.IO.Path.GetFileNameWithoutExtension(file))
                .Replace("$(Ext)", System.IO.Path.GetExtension(file).TrimStart('.'));

            // Get the current count of items
            int currentIndex = VideoQueue.Count;

            var video = new VideoModelViewModel
            {
                Index = currentIndex,
                InputFilePath = file,
                VideoInfo = videoInfo,
                Preset = DefaultPreset ?? Presets.FirstOrDefault(),
                Presets = Presets,
                OutputFilePath = resolvedOutputPath
            };

            VideoQueue.Add(video);
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
        }

        // Command to remove video
        public ReactiveCommand<VideoModelViewModel, Unit> RemoveVideoCommand => ReactiveCommand.Create<VideoModelViewModel>(RemoveVideo);

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

        // Clear Completed Command
        public ReactiveCommand<Unit, Unit> ClearCompletedCommand => ReactiveCommand.Create(() =>
        {
            var completedVideos = VideoQueue.Where(v => v.ConversionStatus == VideoConversionStatus.Completed).ToList();
            foreach (var video in completedVideos)
            {
                VideoQueue.Remove(video);
            }
            LogOutput += $"[{DateTime.Now:HH:mm:ss}] Cleared {completedVideos.Count} completed videos from queue\n";
        });

        // Retrieve video info via FFmpeg wrapper
        private async Task<VideoInfoModel> GetVideoInfoAsync(string filePath)
        {
            // Call FFmpeg wrapper to get video information (e.g., duration, resolution, etc.)
            var ffmpegWrapper = new FFmpegWrapper("ffmpeg");
            return ffmpegWrapper.GetVideoInfo(filePath);
        }

        // Start Conversion Command
        public ReactiveCommand<Unit, Unit> StartConversionCommand => ReactiveCommand.CreateFromTask(async () =>
        {
            if (!IsHandbrakeCLIAvailable())
            {
                StatusText = "HandBrakeCLI not found";
                LogOutput += $"[{DateTime.Now:HH:mm:ss}] ERROR: HandBrakeCLI not found at specified path\n";
                return;
            }

            IsConverting = true;
            StatusText = "Starting conversion...";
            LogOutput += $"[{DateTime.Now:HH:mm:ss}] Starting batch conversion of {VideoQueue.Count} videos\n";

            var videosToProcess = VideoQueue.Where(v => v.ConversionStatus == VideoConversionStatus.NotStarted || 
                                                      v.ConversionStatus == VideoConversionStatus.Queued).ToList();

            var semaphore = new System.Threading.SemaphoreSlim(ParallelInstances, ParallelInstances);
            var tasks = videosToProcess.Select(async video =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await ProcessVideo(video);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks);

            IsConverting = false;
            StatusText = $"Conversion completed. {CompletedCount} videos processed.";
            LogOutput += $"[{DateTime.Now:HH:mm:ss}] Batch conversion completed\n";
        });

        private async Task ProcessVideo(VideoModelViewModel video)
        {
            try
            {
                video.ConversionStatus = VideoConversionStatus.InProgress;
                video.ConversionProgress = 0;
                video.StartTime = DateTime.Now;
                
                LogOutput += $"[{DateTime.Now:HH:mm:ss}] Starting conversion of {video.VideoInfo?.FileName}\n";
                
                var handbrakeCliWrapper = new HandbrakeCLIWrapper();
                
                // Subscribe to progress events
                handbrakeCliWrapper.ProgressChanged += (sender, e) =>
                {
                    video.ConversionProgress = e.Progress;
                };
                
                handbrakeCliWrapper.ConversionCompleted += (sender, e) =>
                {
                    video.EndTime = DateTime.Now;
                    if (e.Success)
                    {
                        video.ConversionStatus = VideoConversionStatus.Completed;
                        video.ConversionProgress = 100;
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] Successfully converted {video.VideoInfo?.FileName}\n";
                        
                        if (DeleteSourceAfterConversion && File.Exists(video.InputFilePath))
                        {
                            try
                            {
                                File.Delete(video.InputFilePath);
                                LogOutput += $"[{DateTime.Now:HH:mm:ss}] Deleted source file {video.VideoInfo?.FileName}\n";
                            }
                            catch (Exception ex)
                            {
                                LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to delete source file: {ex.Message}\n";
                            }
                        }
                    }
                    else
                    {
                        video.ConversionStatus = VideoConversionStatus.Failed;
                        video.ErrorMessage = e.ErrorMessage;
                        LogOutput += $"[{DateTime.Now:HH:mm:ss}] Failed to convert {video.VideoInfo?.FileName}: {e.ErrorMessage}\n";
                    }
                };
                
                await handbrakeCliWrapper.ConvertVideoAsync(
                    video.InputFilePath!, 
                    video.OutputFilePath!, 
                    video.Preset);
            }
            catch (Exception ex)
            {
                video.ConversionStatus = VideoConversionStatus.Failed;
                video.ErrorMessage = ex.Message;
                video.EndTime = DateTime.Now;
                LogOutput += $"[{DateTime.Now:HH:mm:ss}] Error processing {video.VideoInfo?.FileName}: {ex.Message}\n";
            }
        }

        // Auto-detect HandbrakeCLI
        private bool IsHandbrakeCLIAvailable()
        {
            try
            {
                var handbrakeWrapper = new HandbrakeCLIWrapper();
                return handbrakeWrapper.IsAvailableAsync().Result;
            }
            catch
            {
                return false;
            }
        }

        // Add Videos Command
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
    }
}
