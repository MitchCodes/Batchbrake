using Batchbrake.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batchbrake.ViewModels
{
    /// <summary>
    /// Represents a video to be converted, including input/output paths, 
    /// preset settings, clip details, and conversion status.
    /// </summary>
    public class VideoModelViewModel : ViewModelBase
    {

        private int _index;
        public int Index
        {
            get => _index;
            set
            {
                this.RaiseAndSetIfChanged(ref _index, value);
            }
        }

        private string? _inputFilePath;
        /// <summary>
        /// Gets or sets the path to the input video file.
        /// </summary>
        public string? InputFilePath
        {
            get => _inputFilePath;
            set
            {
                this.RaiseAndSetIfChanged(ref _inputFilePath, value);
            }
        }

        private VideoInfoModel? _videoInfo;
        /// <summary>
        /// Gets or sets the details of the video file.
        /// </summary>
        public VideoInfoModel? VideoInfo
        {
            get => _videoInfo;
            set
            {
                this.RaiseAndSetIfChanged(ref _videoInfo, value);
            }
        }

        private string? _outputFilePath;
        /// <summary>
        /// Gets or sets the path where the output video should be saved.
        /// </summary>
        public string? OutputFilePath
        {
            get => _outputFilePath;
            set
            {
                this.RaiseAndSetIfChanged(ref _outputFilePath, value);
            }
        }

        private string? _outputFileName;
        /// <summary>
        /// Gets or sets the output file name, defaulting to the input file name.
        /// </summary>
        public string? OutputFileName
        {
            get => _outputFileName;
            set
            {
                this.RaiseAndSetIfChanged(ref _outputFileName, value);
            }
        }

        private string? _preset;
        /// <summary>
        /// Gets or sets the Handbrake preset to use for conversion.
        /// </summary>
        public string? Preset
        {
            get => _preset;
            set
            {
                this.RaiseAndSetIfChanged(ref _preset, value);
            }
        }

        private ObservableCollection<string> _presets = new ObservableCollection<string>();
        /// <summary>
        /// Gets or sets the available presets for the video.
        /// </summary>
        public ObservableCollection<string> Presets
        {
            get => _presets;
            set
            {
                this.RaiseAndSetIfChanged(ref _presets, value);
            }
        }

        private ObservableCollection<ClipModel>? _clips;
        /// <summary>
        /// Gets or sets the list of clips to be extracted from the video.
        /// If no clips are specified, the entire video will be converted.
        /// </summary>
        public ObservableCollection<ClipModel>? Clips
        {
            get => _clips;
            set
            {
                this.RaiseAndSetIfChanged(ref _clips, value);
                this.RaisePropertyChanged(nameof(HasSegments));
            }
        }

        /// <summary>
        /// Gets a value indicating whether this video has segments defined.
        /// </summary>
        public bool HasSegments => Clips != null && Clips.Count > 0;

        private VideoConversionStatus _conversionStatus;
        /// <summary>
        /// Gets or sets the conversion status of the video.
        /// </summary>
        public VideoConversionStatus ConversionStatus
        {
            get => _conversionStatus;
            set
            {
                this.RaiseAndSetIfChanged(ref _conversionStatus, value);
                this.RaisePropertyChanged(nameof(CanEditVideo));
            }
        }

        /// <summary>
        /// Gets a value indicating whether this video can be edited (not currently in progress).
        /// </summary>
        public bool CanEditVideo => ConversionStatus != VideoConversionStatus.InProgress;

        private double _conversionProgress;
        /// <summary>
        /// Gets or sets the conversion progress (0-100).
        /// </summary>
        public double ConversionProgress
        {
            get => _conversionProgress;
            set
            {
                this.RaiseAndSetIfChanged(ref _conversionProgress, value);
            }
        }

        private string? _errorMessage;
        /// <summary>
        /// Gets or sets the error message if conversion fails.
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _errorMessage, value);
            }
        }

        private DateTime? _startTime;
        /// <summary>
        /// Gets or sets the time when conversion started.
        /// </summary>
        public DateTime? StartTime
        {
            get => _startTime;
            set
            {
                this.RaiseAndSetIfChanged(ref _startTime, value);
            }
        }

        private DateTime? _endTime;
        /// <summary>
        /// Gets or sets the time when conversion ended.
        /// </summary>
        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                this.RaiseAndSetIfChanged(ref _endTime, value);
            }
        }

        private bool _deleteSourceAfterConversion;
        /// <summary>
        /// Gets or sets whether to delete the source file after successful conversion.
        /// </summary>
        public bool DeleteSourceAfterConversion
        {
            get => _deleteSourceAfterConversion;
            set
            {
                this.RaiseAndSetIfChanged(ref _deleteSourceAfterConversion, value);
            }
        }

        private string? _outputFormat = "mp4";
        /// <summary>
        /// Gets or sets the output format/container.
        /// </summary>
        public string? OutputFormat
        {
            get => _outputFormat;
            set
            {
                this.RaiseAndSetIfChanged(ref _outputFormat, value);
                UpdateOutputFilePath();
            }
        }

        private void UpdateOutputFilePath()
        {
            if (!string.IsNullOrEmpty(OutputFilePath) && !string.IsNullOrEmpty(OutputFormat))
            {
                var directory = System.IO.Path.GetDirectoryName(OutputFilePath);
                var fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(OutputFilePath);
                OutputFilePath = System.IO.Path.Combine(directory, $"{fileNameWithoutExt}.{OutputFormat}");
            }
        }
    }

    /// <summary>
    /// Represents a video clip with start and end times.
    /// </summary>
    public class ClipModel : ViewModelBase
    {
        private TimeSpan _start;
        /// <summary>
        /// Gets or sets the start time of the clip.
        /// </summary>
        public TimeSpan Start
        {
            get => _start;
            set
            {
                this.RaiseAndSetIfChanged(ref _start, value);
            }
        }

        private TimeSpan _end;
        /// <summary>
        /// Gets or sets the end time of the clip.
        /// </summary>
        public TimeSpan End
        {
            get => _end;
            set
            {
                this.RaiseAndSetIfChanged(ref _end, value);
            }
        }
    }

    /// <summary>
    /// Represents the conversion status of a video.
    /// </summary>
    public enum VideoConversionStatus
    {
        NotStarted = 0,
        Queued = 1,
        InProgress = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5
    }
}
