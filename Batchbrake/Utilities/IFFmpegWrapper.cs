using Batchbrake.Models;
using System.Threading.Tasks;

namespace Batchbrake.Utilities
{
    /// <summary>
    /// Interface for FFmpeg wrapper functionality.
    /// </summary>
    public interface IFFmpegWrapper
    {
        /// <summary>
        /// Retrieves the details of the specified video file.
        /// </summary>
        /// <param name="filePath">The path to the video file.</param>
        /// <returns>The details of the video file.</returns>
        VideoInfoModel GetVideoInfo(string filePath);

        /// <summary>
        /// Asynchronously retrieves the details of the specified video file.
        /// </summary>
        /// <param name="filePath">The path to the video file.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<VideoInfoModel> GetVideoInfoAsync(string filePath);
    }
}