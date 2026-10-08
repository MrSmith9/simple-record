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
        /// Which color theme every window in the app is drawn in. Changing
        /// this in Settings is saved immediately and, while nothing is
        /// recording, shows right away (the main window reopens itself
        /// with the new colors - see <see cref="Services.ThemeManager"/>
        /// and MainWindow.xaml.cs's SettingsButton_Click). If a recording
        /// is in progress when it's changed, it takes effect the next time
        /// the app is opened instead, since the main window can't safely
        /// be replaced mid-recording.
        /// </summary>
        public AppTheme Theme { get; set; } = AppTheme.Dark;

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
