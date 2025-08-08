using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Batchbrake.Models
{
    public class HandBrakeSettings : INotifyPropertyChanged
    {
        private string _handBrakeCLIPath = "HandBrakeCLI";
        private string _defaultPreset = "Fast 1080p30";
        private string _outputFormat = "mp4";
        private int _qualityValue = 22;
        private bool _twoPass = false;
        private bool _turboFirstPass = true;
        private string _videoEncoder = "x264";
        private string _audioEncoder = "av_aac";
        private int _audioBitrate = 160;
        private string _audioMixdown = "stereo";
        private bool _includeChapterMarkers = true;
        private bool _optimizeForWeb = true;
        private bool _iPodCompatible = false;
        private string _additionalArguments = "";
        private int _verbosity = 1;
        private bool _updateCheckEnabled = false;
        private bool _sendFileDone = false;
        private string _customPresetFile = "";
        private List<string> _customPresetFiles = new List<string>();

        public string HandBrakeCLIPath
        {
            get => _handBrakeCLIPath;
            set
            {
                if (_handBrakeCLIPath != value)
                {
                    _handBrakeCLIPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DefaultPreset
        {
            get => _defaultPreset;
            set
            {
                if (_defaultPreset != value)
                {
                    _defaultPreset = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OutputFormat
        {
            get => _outputFormat;
            set
            {
                if (_outputFormat != value)
                {
                    _outputFormat = value;
                    OnPropertyChanged();
                }
            }
        }

        public int QualityValue
        {
            get => _qualityValue;
            set
            {
                if (_qualityValue != value)
                {
                    _qualityValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool TwoPass
        {
            get => _twoPass;
            set
            {
                if (_twoPass != value)
                {
                    _twoPass = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool TurboFirstPass
        {
            get => _turboFirstPass;
            set
            {
                if (_turboFirstPass != value)
                {
                    _turboFirstPass = value;
                    OnPropertyChanged();
                }
            }
        }

        public string VideoEncoder
        {
            get => _videoEncoder;
            set
            {
                if (_videoEncoder != value)
                {
                    _videoEncoder = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AudioEncoder
        {
            get => _audioEncoder;
            set
            {
                if (_audioEncoder != value)
                {
                    _audioEncoder = value;
                    OnPropertyChanged();
                }
            }
        }

        public int AudioBitrate
        {
            get => _audioBitrate;
            set
            {
                if (_audioBitrate != value)
                {
                    _audioBitrate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AudioMixdown
        {
            get => _audioMixdown;
            set
            {
                if (_audioMixdown != value)
                {
                    _audioMixdown = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeChapterMarkers
        {
            get => _includeChapterMarkers;
            set
            {
                if (_includeChapterMarkers != value)
                {
                    _includeChapterMarkers = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool OptimizeForWeb
        {
            get => _optimizeForWeb;
            set
            {
                if (_optimizeForWeb != value)
                {
                    _optimizeForWeb = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IPodCompatible
        {
            get => _iPodCompatible;
            set
            {
                if (_iPodCompatible != value)
                {
                    _iPodCompatible = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AdditionalArguments
        {
            get => _additionalArguments;
            set
            {
                if (_additionalArguments != value)
                {
                    _additionalArguments = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Verbosity
        {
            get => _verbosity;
            set
            {
                if (_verbosity != value)
                {
                    _verbosity = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool UpdateCheckEnabled
        {
            get => _updateCheckEnabled;
            set
            {
                if (_updateCheckEnabled != value)
                {
                    _updateCheckEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool SendFileDone
        {
            get => _sendFileDone;
            set
            {
                if (_sendFileDone != value)
                {
                    _sendFileDone = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Path to custom preset JSON file exported from HandBrake GUI (legacy - maintained for backward compatibility)
        /// </summary>
        public string CustomPresetFile
        {
            get => _customPresetFile;
            set
            {
                if (_customPresetFile != value)
                {
                    _customPresetFile = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// List of custom preset JSON files exported from HandBrake GUI
        /// </summary>
        public List<string> CustomPresetFiles
        {
            get => _customPresetFiles;
            set
            {
                if (_customPresetFiles != value)
                {
                    _customPresetFiles = value ?? new List<string>();
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public HandBrakeSettings Clone()
        {
            return new HandBrakeSettings
            {
                HandBrakeCLIPath = HandBrakeCLIPath,
                DefaultPreset = DefaultPreset,
                OutputFormat = OutputFormat,
                QualityValue = QualityValue,
                TwoPass = TwoPass,
                TurboFirstPass = TurboFirstPass,
                VideoEncoder = VideoEncoder,
                AudioEncoder = AudioEncoder,
                AudioBitrate = AudioBitrate,
                AudioMixdown = AudioMixdown,
                IncludeChapterMarkers = IncludeChapterMarkers,
                OptimizeForWeb = OptimizeForWeb,
                IPodCompatible = IPodCompatible,
                AdditionalArguments = AdditionalArguments,
                Verbosity = Verbosity,
                UpdateCheckEnabled = UpdateCheckEnabled,
                SendFileDone = SendFileDone,
                CustomPresetFile = CustomPresetFile,
                CustomPresetFiles = new List<string>(CustomPresetFiles)
            };
        }
    }
}