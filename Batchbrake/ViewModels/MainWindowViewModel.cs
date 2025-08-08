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

        public MainWindowViewModel()
        {
            VideoQueue.CollectionChanged += VideoQueue_CollectionChanged;
        }

        public MainWindowViewModel(IFilePickerService filePickerService)
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

        // Retrieve video info via FFmpeg wrapper
        private async Task<VideoInfoModel> GetVideoInfoAsync(string filePath)
        {
            // Call FFmpeg wrapper to get video information (e.g., duration, resolution, etc.)
            var ffmpegWrapper = new FFmpegWrapper("ffmpeg");
            return ffmpegWrapper.GetVideoInfo(filePath);
        }

        // Start Conversion Command
        public async Task StartConversionCommand()
        {
            if (!IsHandbrakeCLIAvailable())
            {
                // Notify user that HandbrakeCLI is not available
                return;
            }
            /*
            foreach (var video in VideoQueue)
            {
                if (video.Clips == null || video.Clips.Count == 0)
                {
                    // Convert entire video using Handbrake CLI wrapper
                    var handbrakeCLI = new HandbrakeCLIWrapper("handbrake-cli-path");
                    await handbrakeCLI.ConvertVideoAsync(video.InputFilePath, video.OutputFilePath, video.Preset);
                }
                else
                {
                    // Implement clip-based conversion (trim clips)
                }

                // Optionally delete source video after conversion
            }
            */
        }

        // Auto-detect HandbrakeCLI
        private bool IsHandbrakeCLIAvailable()
        {
            var handbrakePath = "handbrake-cli-path"; // Logic to auto-detect HandbrakeCLI
            return System.IO.File.Exists(handbrakePath);
        }

        // Add Videos Command
        public async Task AddVideosCommand()
        {
            // Start async operation to open the dialog.
            var files = await _filePickerService.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                FileTypeFilter = new List<FilePickerFileType>() {
                    new FilePickerFileType("Videos") { Patterns = new List<string> { "*.mp4", "*.mkv", "*.avi" } }
                },
                Title = "Videos",
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
            }
        }

        private void VideoQueue_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Reassign indexes whenever the collection changes
            for (int i = 0; i < VideoQueue.Count; i++)
            {
                VideoQueue[i].Index = i;
            }
        }
    }
}
