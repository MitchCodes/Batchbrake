using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batchbrake.Models
{
    /// <summary>
    /// Represents the details of a video file.
    /// </summary>
    public class VideoInfoModel
    {
        /// <summary>
        /// Gets or sets the name of the video file.
        /// </summary>
        public string? FileName { get; set; }
         
        /// <summary>
        /// Gets or sets the duration of the video.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the resolution of the video.
        /// </summary>
        public string? Resolution { get; set; }

        /// <summary>
        /// Gets or sets the video codec.
        /// </summary>
        public string? Codec { get; set; }
    }
}
