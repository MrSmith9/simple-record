using System;
using System.IO;

namespace RecorderByKyleSmith.Models
{
    /// <summary>
    /// The user's saved preferences: where recordings are saved, and what
    /// size they're saved at. Loaded once at startup and saved back to disk
    /// (via <see cref="Services.SettingsService"/>) whenever the Settings
    /// window is used to change them, so they carry over between runs.
    /// </summary>
    public class AppSettings
    {
        /// <summary>The folder recordings are saved into, before a date/time file name is generated.</summary>
        public string OutputFolder { get; set; } = DefaultOutputFolder;

        /// <summary>The fixed output size to record at, or Automatic to match the source exactly.</summary>
        public VideoResolutionPreset Resolution { get; set; } = VideoResolutionPreset.Automatic;

        /// <summary>Used the very first time the app runs, before any settings have been saved.</summary>
        public static readonly string DefaultOutputFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Recording by Kyle Smith");
    }
}
