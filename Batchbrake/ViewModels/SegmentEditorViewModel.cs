using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;

namespace Batchbrake.ViewModels
{
    /// <summary>
    /// ViewModel for the Segment Editor dialog
    /// </summary>
    public class SegmentEditorViewModel : ViewModelBase
    {
        private readonly VideoModelViewModel _videoModel;
        
        public event EventHandler<bool>? DialogResult;

        private ObservableCollection<SegmentViewModel> _segments = new ObservableCollection<SegmentViewModel>();
        public ObservableCollection<SegmentViewModel> Segments
        {
            get => _segments;
            set => this.RaiseAndSetIfChanged(ref _segments, value);
        }

        private string _newSegmentStartTime = "";
        public string NewSegmentStartTime
        {
            get => _newSegmentStartTime;
            set => this.RaiseAndSetIfChanged(ref _newSegmentStartTime, value);
        }

        private string _newSegmentEndTime = "";
        public string NewSegmentEndTime
        {
            get => _newSegmentEndTime;
            set => this.RaiseAndSetIfChanged(ref _newSegmentEndTime, value);
        }

        public string VideoFileName => _videoModel.VideoInfo?.FileName ?? "Unknown";
        public TimeSpan VideoDuration => _videoModel.VideoInfo?.Duration ?? TimeSpan.Zero;
        public int SegmentCount => Segments?.Count ?? 0;
        public bool HasSegments => SegmentCount > 0;

        public ReactiveCommand<Unit, Unit> AddSegmentCommand { get; }
        public ReactiveCommand<SegmentViewModel, Unit> RemoveSegmentCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearAllCommand { get; }
        public ReactiveCommand<Unit, Unit> OkCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public SegmentEditorViewModel(VideoModelViewModel videoModel)
        {
            _videoModel = videoModel ?? throw new ArgumentNullException(nameof(videoModel));

            // Initialize segments from the video model
            LoadSegmentsFromVideo();

            // Set up commands
            AddSegmentCommand = ReactiveCommand.Create(AddSegment);
            RemoveSegmentCommand = ReactiveCommand.Create<SegmentViewModel>(RemoveSegment);
            ClearAllCommand = ReactiveCommand.Create(ClearAllSegments);
            OkCommand = ReactiveCommand.Create(SaveAndClose);
            CancelCommand = ReactiveCommand.Create(Cancel);

            // Wire up property change notifications
            Segments.CollectionChanged += (s, e) =>
            {
                this.RaisePropertyChanged(nameof(SegmentCount));
                this.RaisePropertyChanged(nameof(HasSegments));
                UpdateSegmentIndexes();
            };
        }

        private void LoadSegmentsFromVideo()
        {
            Segments.Clear();
            if (_videoModel.Clips != null)
            {
                foreach (var clip in _videoModel.Clips)
                {
                    var segmentVm = new SegmentViewModel(clip);
                    Segments.Add(segmentVm);
                }
            }
            UpdateSegmentIndexes();
        }

        private void AddSegment()
        {
            var startTime = ParseTimeSpan(NewSegmentStartTime);
            var endTime = ParseTimeSpan(NewSegmentEndTime);

            if (startTime == null)
            {
                // TODO: Show error message
                return;
            }

            if (endTime == null)
            {
                // TODO: Show error message
                return;
            }

            if (startTime >= endTime)
            {
                // TODO: Show error message - start must be before end
                return;
            }

            if (endTime > VideoDuration)
            {
                // TODO: Show error message - end time exceeds video duration
                return;
            }

            var clipModel = new ClipModel
            {
                Start = startTime.Value,
                End = endTime.Value
            };

            var segmentVm = new SegmentViewModel(clipModel);
            Segments.Add(segmentVm);

            // Sort segments by start time
            var sortedSegments = Segments.OrderBy(s => s.ClipModel.Start).ToList();
            Segments.Clear();
            foreach (var segment in sortedSegments)
            {
                Segments.Add(segment);
            }

            // Clear input fields
            NewSegmentStartTime = "";
            NewSegmentEndTime = "";
        }

        private void RemoveSegment(SegmentViewModel segment)
        {
            Segments.Remove(segment);
        }

        private void ClearAllSegments()
        {
            Segments.Clear();
        }

        private void SaveAndClose()
        {
            // Update the video model with the current segments
            if (_videoModel.Clips == null)
            {
                _videoModel.Clips = new ObservableCollection<ClipModel>();
            }
            else
            {
                _videoModel.Clips.Clear();
            }

            foreach (var segmentVm in Segments)
            {
                _videoModel.Clips.Add(segmentVm.ClipModel);
            }

            DialogResult?.Invoke(this, true);
        }

        private void Cancel()
        {
            DialogResult?.Invoke(this, false);
        }

        private void UpdateSegmentIndexes()
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                Segments[i].Index = i + 1;
            }
        }

        private static TimeSpan? ParseTimeSpan(string timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return null;

            // Support formats: HH:MM:SS, MM:SS, SS
            var patterns = new[]
            {
                @"^(\d{1,2}):(\d{2}):(\d{2})$", // HH:MM:SS
                @"^(\d{1,2}):(\d{2})$",        // MM:SS
                @"^(\d+)$"                     // SS
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(timeString, pattern);
                if (match.Success)
                {
                    try
                    {
                        if (match.Groups.Count == 4) // HH:MM:SS
                        {
                            var hours = int.Parse(match.Groups[1].Value);
                            var minutes = int.Parse(match.Groups[2].Value);
                            var seconds = int.Parse(match.Groups[3].Value);
                            return new TimeSpan(hours, minutes, seconds);
                        }
                        else if (match.Groups.Count == 3) // MM:SS
                        {
                            var minutes = int.Parse(match.Groups[1].Value);
                            var seconds = int.Parse(match.Groups[2].Value);
                            return new TimeSpan(0, minutes, seconds);
                        }
                        else if (match.Groups.Count == 2) // SS
                        {
                            var seconds = int.Parse(match.Groups[1].Value);
                            return new TimeSpan(0, 0, seconds);
                        }
                    }
                    catch
                    {
                        // Invalid number format
                        continue;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// ViewModel for individual segments in the editor
    /// </summary>
    public class SegmentViewModel : ViewModelBase
    {
        public ClipModel ClipModel { get; }

        private int _index;
        public int Index
        {
            get => _index;
            set => this.RaiseAndSetIfChanged(ref _index, value);
        }

        public string StartTimeText
        {
            get => ClipModel.Start.ToString(@"hh\:mm\:ss");
            set
            {
                var timeSpan = ParseTimeSpan(value);
                if (timeSpan.HasValue)
                {
                    ClipModel.Start = timeSpan.Value;
                    this.RaisePropertyChanged();
                    this.RaisePropertyChanged(nameof(Duration));
                    this.RaisePropertyChanged(nameof(PreviewText));
                }
            }
        }

        public string EndTimeText
        {
            get => ClipModel.End.ToString(@"hh\:mm\:ss");
            set
            {
                var timeSpan = ParseTimeSpan(value);
                if (timeSpan.HasValue)
                {
                    ClipModel.End = timeSpan.Value;
                    this.RaisePropertyChanged();
                    this.RaisePropertyChanged(nameof(Duration));
                    this.RaisePropertyChanged(nameof(PreviewText));
                }
            }
        }

        public TimeSpan Duration => ClipModel.End - ClipModel.Start;

        public string PreviewText => $"Extract {Duration.TotalSeconds:F0}s from {ClipModel.Start:hh\\:mm\\:ss}";

        public SegmentViewModel(ClipModel clipModel)
        {
            ClipModel = clipModel ?? throw new ArgumentNullException(nameof(clipModel));
        }

        private static TimeSpan? ParseTimeSpan(string timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return null;

            // Support formats: HH:MM:SS, MM:SS, SS
            var patterns = new[]
            {
                @"^(\d{1,2}):(\d{2}):(\d{2})$", // HH:MM:SS
                @"^(\d{1,2}):(\d{2})$",        // MM:SS
                @"^(\d+)$"                     // SS
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(timeString, pattern);
                if (match.Success)
                {
                    try
                    {
                        if (match.Groups.Count == 4) // HH:MM:SS
                        {
                            var hours = int.Parse(match.Groups[1].Value);
                            var minutes = int.Parse(match.Groups[2].Value);
                            var seconds = int.Parse(match.Groups[3].Value);
                            return new TimeSpan(hours, minutes, seconds);
                        }
                        else if (match.Groups.Count == 3) // MM:SS
                        {
                            var minutes = int.Parse(match.Groups[1].Value);
                            var seconds = int.Parse(match.Groups[2].Value);
                            return new TimeSpan(0, minutes, seconds);
                        }
                        else if (match.Groups.Count == 2) // SS
                        {
                            var seconds = int.Parse(match.Groups[1].Value);
                            return new TimeSpan(0, 0, seconds);
                        }
                    }
                    catch
                    {
                        // Invalid number format
                        continue;
                    }
                }
            }

            return null;
        }
    }
}