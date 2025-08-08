using Batchbrake.ViewModels;
using System;
using System.Collections.Generic;

namespace Batchbrake.Models
{
    /// <summary>
    /// Represents a session containing a queue of videos to be converted,
    /// and other session-related settings.
    /// </summary>
    public class SessionModel
    {
        public MainWindowViewModel MainWindowViewModel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the session is paused.
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// Gets or sets the last time the session was saved.
        /// </summary>
        public DateTime LastSaveTime { get; set; }
    }
}
