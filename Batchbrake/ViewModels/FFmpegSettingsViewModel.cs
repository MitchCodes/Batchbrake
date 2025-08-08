using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Batchbrake.Models;
using Batchbrake.Services;
using ReactiveUI;

namespace Batchbrake.ViewModels
{
    public class FFmpegSettingsViewModel : ViewModelBase
    {
        private readonly FFmpegSettings _originalSettings;
        private readonly IFilePickerService _filePickerService;
        private readonly Window _window;
        
        private string _ffmpegPath;
        private string _ffprobePath;
        private int _threadCount;
        private string _videoCodec;
        private string _audioCodec;
        private string _additionalArguments;
        private bool _hardwareAcceleration;
        private string _hardwareAccelerationMethod;
        private int _logLevel;
        private bool _overwriteOutput;
        private bool _useAsConversionEngine;

        public FFmpegSettingsViewModel(FFmpegSettings settings, IFilePickerService filePickerService, Window window)
        {
            _originalSettings = settings;
            _filePickerService = filePickerService;
            _window = window;

            FFmpegPath = settings.FFmpegPath;
            FFprobePath = settings.FFprobePath;
            ThreadCount = settings.ThreadCount;
            VideoCodec = settings.VideoCodec;
            AudioCodec = settings.AudioCodec;
            AdditionalArguments = settings.AdditionalArguments;
            HardwareAcceleration = settings.HardwareAcceleration;
            HardwareAccelerationMethod = settings.HardwareAccelerationMethod;
            LogLevel = settings.LogLevel;
            OverwriteOutput = settings.OverwriteOutput;
            UseAsConversionEngine = settings.UseAsConversionEngine;

            BrowseFFmpegCommand = ReactiveCommand.CreateFromTask(BrowseFFmpeg);
            BrowseFFprobeCommand = ReactiveCommand.CreateFromTask(BrowseFFprobe);
            SaveCommand = ReactiveCommand.Create(Save);
            CancelCommand = ReactiveCommand.Create(Cancel);
            ResetToDefaultsCommand = ReactiveCommand.Create(ResetToDefaults);
        }

        public string FFmpegPath
        {
            get => _ffmpegPath;
            set => this.RaiseAndSetIfChanged(ref _ffmpegPath, value);
        }

        public string FFprobePath
        {
            get => _ffprobePath;
            set => this.RaiseAndSetIfChanged(ref _ffprobePath, value);
        }

        public int ThreadCount
        {
            get => _threadCount;
            set => this.RaiseAndSetIfChanged(ref _threadCount, value);
        }

        public string VideoCodec
        {
            get => _videoCodec;
            set => this.RaiseAndSetIfChanged(ref _videoCodec, value);
        }

        public string AudioCodec
        {
            get => _audioCodec;
            set => this.RaiseAndSetIfChanged(ref _audioCodec, value);
        }

        public string AdditionalArguments
        {
            get => _additionalArguments;
            set => this.RaiseAndSetIfChanged(ref _additionalArguments, value);
        }

        public bool HardwareAcceleration
        {
            get => _hardwareAcceleration;
            set => this.RaiseAndSetIfChanged(ref _hardwareAcceleration, value);
        }

        public string HardwareAccelerationMethod
        {
            get => _hardwareAccelerationMethod;
            set => this.RaiseAndSetIfChanged(ref _hardwareAccelerationMethod, value);
        }

        public int LogLevel
        {
            get => _logLevel;
            set => this.RaiseAndSetIfChanged(ref _logLevel, value);
        }

        public bool OverwriteOutput
        {
            get => _overwriteOutput;
            set => this.RaiseAndSetIfChanged(ref _overwriteOutput, value);
        }

        public bool UseAsConversionEngine
        {
            get => _useAsConversionEngine;
            set => this.RaiseAndSetIfChanged(ref _useAsConversionEngine, value);
        }

        public ReactiveCommand<Unit, Unit> BrowseFFmpegCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseFFprobeCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetToDefaultsCommand { get; }

        private async Task BrowseFFmpeg()
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select FFmpeg Executable",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Executable Files")
                    {
                        Patterns = OperatingSystem.IsWindows() ? new[] { "*.exe" } : new[] { "*" }
                    }
                }
            };

            var result = await _filePickerService.OpenFilePickerAsync(options);
            if (result != null && result.Count > 0)
            {
                FFmpegPath = result[0].Path.LocalPath;
            }
        }

        private async Task BrowseFFprobe()
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select FFprobe Executable",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Executable Files")
                    {
                        Patterns = OperatingSystem.IsWindows() ? new[] { "*.exe" } : new[] { "*" }
                    }
                }
            };

            var result = await _filePickerService.OpenFilePickerAsync(options);
            if (result != null && result.Count > 0)
            {
                FFprobePath = result[0].Path.LocalPath;
            }
        }

        private void Save()
        {
            _originalSettings.FFmpegPath = FFmpegPath;
            _originalSettings.FFprobePath = FFprobePath;
            _originalSettings.ThreadCount = ThreadCount;
            _originalSettings.VideoCodec = VideoCodec;
            _originalSettings.AudioCodec = AudioCodec;
            _originalSettings.AdditionalArguments = AdditionalArguments;
            _originalSettings.HardwareAcceleration = HardwareAcceleration;
            _originalSettings.HardwareAccelerationMethod = HardwareAccelerationMethod;
            _originalSettings.LogLevel = LogLevel;
            _originalSettings.OverwriteOutput = OverwriteOutput;
            _originalSettings.UseAsConversionEngine = UseAsConversionEngine;

            _window.Close(true);
        }

        private void Cancel()
        {
            _window.Close(false);
        }

        private void ResetToDefaults()
        {
            FFmpegPath = "ffmpeg";
            FFprobePath = "ffprobe";
            ThreadCount = 0;
            VideoCodec = "libx264";
            AudioCodec = "aac";
            AdditionalArguments = "";
            HardwareAcceleration = false;
            HardwareAccelerationMethod = "auto";
            LogLevel = 2;
            OverwriteOutput = true;
            UseAsConversionEngine = false;
        }
    }
}