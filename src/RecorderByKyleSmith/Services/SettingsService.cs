using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RecorderByKyleSmith.Models;

namespace RecorderByKyleSmith.Services
{
    /// <summary>
    /// Loads and saves <see cref="AppSettings"/> as a small JSON file in the
    /// user's AppData folder, so settings survive between runs of the app
    /// without needing a database or the Windows Registry.
    ///
    /// Both Load and Save are deliberately forgiving: a missing, corrupted,
    /// or unwritable settings file should never stop the app from starting
    /// or from recording - it should just fall back to sensible defaults,
    /// the same way <see cref="UpdateChecker"/> never lets a network problem
    /// become a crash.
    /// </summary>
    public static class SettingsService
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Recording by Kyle Smith",
            "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Loads saved settings, or returns the defaults if none have been
        /// saved yet (first run) or the file can't be read.
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return new AppSettings();
                }

                string json = File.ReadAllText(SettingsFilePath);
                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        /// <summary>
        /// Saves settings to disk. Returns false (rather than throwing) if
        /// that wasn't possible, e.g. no permission to write to AppData - the
        /// caller can still keep using the new settings for the rest of this
        /// session, they just won't be remembered next time.
        /// </summary>
        public static bool Save(AppSettings settings)
        {
            try
            {
                string? folder = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
