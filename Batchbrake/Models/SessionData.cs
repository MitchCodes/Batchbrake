using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Batchbrake.ViewModels;

namespace Batchbrake.Models
{
    /// <summary>
    /// Serializable session data containing video queue information and settings
    /// </summary>
    public class SessionData
    {
        /// <summary>
        /// List of videos in the conversion queue
        /// </summary>
        public List<VideoSessionData> Videos { get; set; } = new List<VideoSessionData>();

        /// <summary>
        /// Default preset for new videos
        /// </summary>
        public string? DefaultPreset { get; set; }

        /// <summary>
        /// Default output path template
        /// </summary>
        public string DefaultOutputPath { get; set; } = "$(Folder)\\$(FileName)_conv.$(Ext)";

        /// <summary>
        /// Default output format
        /// </summary>
        public string DefaultOutputFormat { get; set; } = "mp4";

        /// <summary>
        /// Number of parallel conversion instances
        /// </summary>
        public int ParallelInstances { get; set; } = 1;

        /// <summary>
        /// Whether to delete source files after conversion
        /// </summary>
        public bool DeleteSourceAfterConversion { get; set; }

        /// <summary>
        /// Timestamp when the session was last saved
        /// </summary>
        public DateTime LastSaved { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Serializable video data for session persistence
    /// </summary>
    public class VideoSessionData
    {
        /// <summary>
        /// Input file path
        /// </summary>
        public string InputFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Output file path
        /// </summary>
        public string OutputFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Selected preset
        /// </summary>
        public string? Preset { get; set; }

        /// <summary>
        /// Conversion status
        /// </summary>
        public VideoConversionStatus ConversionStatus { get; set; }

        /// <summary>
        /// Video information
        /// </summary>
        public VideoInfoModel? VideoInfo { get; set; }

        /// <summary>
        /// Error message if conversion failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Start time of conversion
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time of conversion
        /// </summary>
        public DateTime? EndTime { get; set; }
    }
}