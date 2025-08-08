using System;
using System.Collections.Generic;

namespace Batchbrake.Models
{
    /// <summary>
    /// Application preferences and settings
    /// </summary>
    public class Preferences
    {
        /// <summary>
        /// Default number of parallel conversion instances (1-10)
        /// </summary>
        public int DefaultParallelInstances { get; set; } = 2;

        /// <summary>
        /// Default output format for new videos
        /// </summary>
        public string DefaultOutputFormat { get; set; } = "mp4";

        /// <summary>
        /// Default output path template
        /// </summary>
        public string DefaultOutputPath { get; set; } = "$(Folder)\\$(FileName)_converted.$(Ext)";

        /// <summary>
        /// Whether to delete source files after successful conversion by default
        /// </summary>
        public bool DeleteSourceAfterConversion { get; set; } = false;

        /// <summary>
        /// Whether to automatically save session on changes
        /// </summary>
        public bool AutoSaveSession { get; set; } = true;

        /// <summary>
        /// Session auto-save interval in minutes
        /// </summary>
        public int AutoSaveIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// Whether to show system notifications when conversions complete
        /// </summary>
        public bool ShowCompletionNotifications { get; set; } = true;

        /// <summary>
        /// Whether to minimize to system tray when window is closed
        /// </summary>
        public bool MinimizeToTray { get; set; } = false;

        /// <summary>
        /// Whether to start conversions automatically when videos are added (if queue was empty)
        /// </summary>
        public bool AutoStartConversions { get; set; } = false;

        /// <summary>
        /// Log verbosity level (0=Minimal, 1=Normal, 2=Detailed)
        /// </summary>
        public int LogVerbosity { get; set; } = 1;

        /// <summary>
        /// Maximum number of log lines to keep in memory
        /// </summary>
        public int MaxLogLines { get; set; } = 1000;

        /// <summary>
        /// Whether to remember window size and position
        /// </summary>
        public bool RememberWindowState { get; set; } = true;

        /// <summary>
        /// Supported video file extensions for drag and drop
        /// </summary>
        public List<string> SupportedVideoExtensions { get; set; } = new List<string> 
        { 
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp", ".ogv" 
        };
    }
}