using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Batchbrake.Models;
using Batchbrake.Services;
using Batchbrake.Utilities;
using ReactiveUI;

namespace Batchbrake.ViewModels
{
    public class HandBrakeSettingsViewModel : ViewModelBase
    {
        private readonly HandBrakeSettings _originalSettings;
        private readonly IFilePickerService _filePickerService;
        private readonly Window _window;
        
        private string _handBrakeCLIPath;
        private string _defaultPreset;
        private string _outputFormat;
        private int _qualityValue;
        private bool _twoPass;
        private bool _turboFirstPass;
        private string _videoEncoder;
        private string _audioEncoder;
        private int _audioBitrate;
        private string _audioMixdown;
        private bool _includeChapterMarkers;
        private bool _optimizeForWeb;
        private bool _iPodCompatible;
        private string _additionalArguments;
        private int _verbosity;
        private bool _updateCheckDisabled;
        private bool _sendFileDone;
        private string _customPresetFile;
        private ObservableCollection<string> _customPresetFiles;
        private string? _selectedPresetFile;
        private string _newPresetFilePath = "";
        private ObservableCollection<string> _availablePresets;

        public HandBrakeSettingsViewModel(HandBrakeSettings settings, IFilePickerService filePickerService, Window window)
        {
            _originalSettings = settings;
            _filePickerService = filePickerService;
            _window = window;

            // Initialize properties from settings
            HandBrakeCLIPath = settings.HandBrakeCLIPath;
            DefaultPreset = settings.DefaultPreset;
            OutputFormat = settings.OutputFormat;
            QualityValue = settings.QualityValue;
            TwoPass = settings.TwoPass;
            TurboFirstPass = settings.TurboFirstPass;
            VideoEncoder = settings.VideoEncoder;
            AudioEncoder = settings.AudioEncoder;
            AudioBitrate = settings.AudioBitrate;
            AudioMixdown = settings.AudioMixdown;
            IncludeChapterMarkers = settings.IncludeChapterMarkers;
            OptimizeForWeb = settings.OptimizeForWeb;
            IPodCompatible = settings.IPodCompatible;
            AdditionalArguments = settings.AdditionalArguments;
            Verbosity = settings.Verbosity;
            UpdateCheckDisabled = settings.UpdateCheckEnabled;
            SendFileDone = settings.SendFileDone;
            CustomPresetFile = settings.CustomPresetFile;
            
            // Initialize custom preset files collection
            _customPresetFiles = new ObservableCollection<string>(settings.CustomPresetFiles ?? new List<string>());
            
            // Migrate legacy single file to collection if needed
            if (!string.IsNullOrWhiteSpace(settings.CustomPresetFile) && !_customPresetFiles.Contains(settings.CustomPresetFile))
            {
                _customPresetFiles.Add(settings.CustomPresetFile);
            }

            // Initialize available presets
            _availablePresets = new ObservableCollection<string>();
            LoadAvailablePresets();

            // Initialize commands
            BrowseHandBrakeCLICommand = ReactiveCommand.CreateFromTask(BrowseHandBrakeCLI);
            BrowseCustomPresetFileCommand = ReactiveCommand.CreateFromTask(BrowseCustomPresetFile);
            AddPresetFileCommand = ReactiveCommand.Create(AddPresetFile);
            RemovePresetFileCommand = ReactiveCommand.Create<string>(RemovePresetFile);
            ClearPresetFilesCommand = ReactiveCommand.Create(ClearPresetFiles);
            SaveCommand = ReactiveCommand.Create(Save);
            CancelCommand = ReactiveCommand.Create(Cancel);
            ResetToDefaultsCommand = ReactiveCommand.Create(ResetToDefaults);
        }

        public string HandBrakeCLIPath
        {
            get => _handBrakeCLIPath;
            set => this.RaiseAndSetIfChanged(ref _handBrakeCLIPath, value);
        }

        public string DefaultPreset
        {
            get => _defaultPreset;
            set => this.RaiseAndSetIfChanged(ref _defaultPreset, value);
        }

        public string OutputFormat
        {
            get => _outputFormat;
            set => this.RaiseAndSetIfChanged(ref _outputFormat, value);
        }

        public int QualityValue
        {
            get => _qualityValue;
            set => this.RaiseAndSetIfChanged(ref _qualityValue, value);
        }

        public bool TwoPass
        {
            get => _twoPass;
            set => this.RaiseAndSetIfChanged(ref _twoPass, value);
        }

        public bool TurboFirstPass
        {
            get => _turboFirstPass;
            set => this.RaiseAndSetIfChanged(ref _turboFirstPass, value);
        }

        public string VideoEncoder
        {
            get => _videoEncoder;
            set => this.RaiseAndSetIfChanged(ref _videoEncoder, value);
        }

        public string AudioEncoder
        {
            get => _audioEncoder;
            set => this.RaiseAndSetIfChanged(ref _audioEncoder, value);
        }

        public int AudioBitrate
        {
            get => _audioBitrate;
            set => this.RaiseAndSetIfChanged(ref _audioBitrate, value);
        }

        public string AudioMixdown
        {
            get => _audioMixdown;
            set => this.RaiseAndSetIfChanged(ref _audioMixdown, value);
        }

        public bool IncludeChapterMarkers
        {
            get => _includeChapterMarkers;
            set => this.RaiseAndSetIfChanged(ref _includeChapterMarkers, value);
        }

        public bool OptimizeForWeb
        {
            get => _optimizeForWeb;
            set => this.RaiseAndSetIfChanged(ref _optimizeForWeb, value);
        }

        public bool IPodCompatible
        {
            get => _iPodCompatible;
            set => this.RaiseAndSetIfChanged(ref _iPodCompatible, value);
        }

        public string AdditionalArguments
        {
            get => _additionalArguments;
            set => this.RaiseAndSetIfChanged(ref _additionalArguments, value);
        }

        public string CustomPresetFile
        {
            get => _customPresetFile;
            set => this.RaiseAndSetIfChanged(ref _customPresetFile, value);
        }

        public int Verbosity
        {
            get => _verbosity;
            set => this.RaiseAndSetIfChanged(ref _verbosity, value);
        }

        public bool UpdateCheckDisabled
        {
            get => _updateCheckDisabled;
            set => this.RaiseAndSetIfChanged(ref _updateCheckDisabled, value);
        }

        public bool SendFileDone
        {
            get => _sendFileDone;
            set => this.RaiseAndSetIfChanged(ref _sendFileDone, value);
        }

        public ObservableCollection<string> AvailablePresets
        {
            get => _availablePresets;
            set => this.RaiseAndSetIfChanged(ref _availablePresets, value);
        }

        public ObservableCollection<string> CustomPresetFiles
        {
            get => _customPresetFiles;
            set => this.RaiseAndSetIfChanged(ref _customPresetFiles, value);
        }

        public string? SelectedPresetFile
        {
            get => _selectedPresetFile;
            set => this.RaiseAndSetIfChanged(ref _selectedPresetFile, value);
        }

        public string NewPresetFilePath
        {
            get => _newPresetFilePath;
            set => this.RaiseAndSetIfChanged(ref _newPresetFilePath, value);
        }

        public ReactiveCommand<Unit, Unit> BrowseHandBrakeCLICommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseCustomPresetFileCommand { get; }
        public ReactiveCommand<Unit, Unit> AddPresetFileCommand { get; }
        public ReactiveCommand<string, Unit> RemovePresetFileCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearPresetFilesCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetToDefaultsCommand { get; }

        private async void LoadAvailablePresets()
        {
            try
            {
                var settings = new HandBrakeSettings 
                { 
                    HandBrakeCLIPath = _handBrakeCLIPath,
                    CustomPresetFile = _customPresetFile,
                    CustomPresetFiles = new List<string>(_customPresetFiles)
                };
                var handbrakeWrapper = new HandbrakeCLIWrapper(settings);
                var presets = await handbrakeWrapper.GetAvailablePresetsAsync();
                
                AvailablePresets.Clear();
                foreach (var category in presets.Values)
                {
                    foreach (var preset in category)
                    {
                        AvailablePresets.Add(preset);
                    }
                }

                // If current preset is not in the list, add it
                if (!string.IsNullOrEmpty(DefaultPreset) && !AvailablePresets.Contains(DefaultPreset))
                {
                    AvailablePresets.Add(DefaultPreset);
                }
            }
            catch
            {
                // If we can't load presets, add some defaults
                AvailablePresets.Clear();
                AvailablePresets.Add("Fast 1080p30");
                AvailablePresets.Add("Fast 720p30");
                AvailablePresets.Add("Fast 480p30");
                AvailablePresets.Add("HQ 1080p30 Surround");
                AvailablePresets.Add("HQ 720p30 Surround");
                AvailablePresets.Add("Super HQ 1080p30 Surround");
                
                if (!string.IsNullOrEmpty(DefaultPreset) && !AvailablePresets.Contains(DefaultPreset))
                {
                    AvailablePresets.Add(DefaultPreset);
                }
            }
        }

        private async Task BrowseHandBrakeCLI()
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select HandBrakeCLI Executable",
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
                HandBrakeCLIPath = result[0].Path.LocalPath;
                
                // Reload presets with new path
                LoadAvailablePresets();
            }
        }

        private async Task BrowseCustomPresetFile()
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select HandBrake Preset File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON Files")
                    {
                        Patterns = new[] { "*.json" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*" }
                    }
                }
            };

            var result = await _filePickerService.OpenFilePickerAsync(options);
            if (result != null && result.Count > 0)
            {
                NewPresetFilePath = result[0].Path.LocalPath;
            }
        }

        private void AddPresetFile()
        {
            if (!string.IsNullOrWhiteSpace(NewPresetFilePath))
            {
                if (!CustomPresetFiles.Contains(NewPresetFilePath))
                {
                    CustomPresetFiles.Add(NewPresetFilePath);
                    NewPresetFilePath = ""; // Clear the textbox
                    
                    // Reload presets with new file
                    LoadAvailablePresets();
                }
            }
        }

        private void RemovePresetFile(string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                CustomPresetFiles.Remove(filePath);
                
                // Reload presets without the removed file
                LoadAvailablePresets();
            }
        }

        private void ClearPresetFiles()
        {
            CustomPresetFiles.Clear();
            
            // Reload presets without custom files
            LoadAvailablePresets();
        }

        private void Save()
        {
            // Update original settings with new values
            _originalSettings.HandBrakeCLIPath = HandBrakeCLIPath;
            _originalSettings.DefaultPreset = DefaultPreset;
            _originalSettings.OutputFormat = OutputFormat;
            _originalSettings.QualityValue = QualityValue;
            _originalSettings.TwoPass = TwoPass;
            _originalSettings.TurboFirstPass = TurboFirstPass;
            _originalSettings.VideoEncoder = VideoEncoder;
            _originalSettings.AudioEncoder = AudioEncoder;
            _originalSettings.AudioBitrate = AudioBitrate;
            _originalSettings.AudioMixdown = AudioMixdown;
            _originalSettings.IncludeChapterMarkers = IncludeChapterMarkers;
            _originalSettings.OptimizeForWeb = OptimizeForWeb;
            _originalSettings.IPodCompatible = IPodCompatible;
            _originalSettings.AdditionalArguments = AdditionalArguments;
            _originalSettings.Verbosity = Verbosity;
            _originalSettings.UpdateCheckEnabled = !UpdateCheckDisabled;
            _originalSettings.SendFileDone = SendFileDone;
            _originalSettings.CustomPresetFile = CustomPresetFile;
            _originalSettings.CustomPresetFiles = new List<string>(CustomPresetFiles);

            _window.Close(true);
        }

        private void Cancel()
        {
            _window.Close(false);
        }

        private void ResetToDefaults()
        {
            HandBrakeCLIPath = "HandBrakeCLI";
            DefaultPreset = "Fast 1080p30";
            OutputFormat = "mp4";
            QualityValue = 22;
            TwoPass = false;
            TurboFirstPass = true;
            VideoEncoder = "x264";
            AudioEncoder = "av_aac";
            AudioBitrate = 160;
            AudioMixdown = "stereo";
            IncludeChapterMarkers = true;
            OptimizeForWeb = true;
            IPodCompatible = false;
            AdditionalArguments = "";
            Verbosity = 1;
            UpdateCheckDisabled = false;
            SendFileDone = false;
            CustomPresetFile = "";
            CustomPresetFiles.Clear();
            
            // Reload presets with default path
            LoadAvailablePresets();
        }
    }
}