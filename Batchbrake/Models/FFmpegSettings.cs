using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Batchbrake.Models
{
    public class FFmpegSettings : INotifyPropertyChanged
    {
        private string _ffmpegPath = "ffmpeg";
        private string _ffprobePath = "ffprobe";
        private int _threadCount = 0;
        private string _videoCodec = "libx264";
        private string _audioCodec = "aac";
        private string _additionalArguments = "";
        private bool _hardwareAcceleration = false;
        private string _hardwareAccelerationMethod = "auto";
        private int _logLevel = 2;
        private bool _overwriteOutput = true;

        public string FFmpegPath
        {
            get => _ffmpegPath;
            set
            {
                if (_ffmpegPath != value)
                {
                    _ffmpegPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FFprobePath
        {
            get => _ffprobePath;
            set
            {
                if (_ffprobePath != value)
                {
                    _ffprobePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ThreadCount
        {
            get => _threadCount;
            set
            {
                if (_threadCount != value)
                {
                    _threadCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public string VideoCodec
        {
            get => _videoCodec;
            set
            {
                if (_videoCodec != value)
                {
                    _videoCodec = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AudioCodec
        {
            get => _audioCodec;
            set
            {
                if (_audioCodec != value)
                {
                    _audioCodec = value;
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

        public bool HardwareAcceleration
        {
            get => _hardwareAcceleration;
            set
            {
                if (_hardwareAcceleration != value)
                {
                    _hardwareAcceleration = value;
                    OnPropertyChanged();
                }
            }
        }

        public string HardwareAccelerationMethod
        {
            get => _hardwareAccelerationMethod;
            set
            {
                if (_hardwareAccelerationMethod != value)
                {
                    _hardwareAccelerationMethod = value;
                    OnPropertyChanged();
                }
            }
        }

        public int LogLevel
        {
            get => _logLevel;
            set
            {
                if (_logLevel != value)
                {
                    _logLevel = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool OverwriteOutput
        {
            get => _overwriteOutput;
            set
            {
                if (_overwriteOutput != value)
                {
                    _overwriteOutput = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public FFmpegSettings Clone()
        {
            return new FFmpegSettings
            {
                FFmpegPath = FFmpegPath,
                FFprobePath = FFprobePath,
                ThreadCount = ThreadCount,
                VideoCodec = VideoCodec,
                AudioCodec = AudioCodec,
                AdditionalArguments = AdditionalArguments,
                HardwareAcceleration = HardwareAcceleration,
                HardwareAccelerationMethod = HardwareAccelerationMethod,
                LogLevel = LogLevel,
                OverwriteOutput = OverwriteOutput
            };
        }
    }
}