using System;
using System.IO;

namespace SimpleRecord.Models
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

        /// <summary>
        /// Full path to a user-chosen picture to show behind the main
        /// window, or null to use the plain dark background (the default,
        /// and what every other window in the app always uses). This is
        /// always a copy stored inside the app's own AppData folder (made
        /// by <see cref="Services.SettingsService.SaveBackgroundImage"/>),
        /// never the original file the user picked - that way it keeps
        /// working even if the original file is later moved or deleted.
        /// </summary>
        public string? BackgroundImagePath { get; set; }

        /// <summary>
        /// Whether to record audio from the microphone. Off by default,
        /// since adding sound to a recording that previously had none
        /// shouldn't happen without the user choosing it.
        /// </summary>
        public bool MicrophoneEnabled { get; set; } = false;

        /// <summary>Used the very first time the app runs, before any settings have been saved.</summary>
        public static readonly string DefaultOutputFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Simple Record");
    }
}
