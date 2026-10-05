using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimpleRecord.Models;

namespace SimpleRecord.Services
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
            "Simple Record",
            "settings.json");

        private static readonly string BackgroundImageFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Simple Record",
            "Background");

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

        /// <summary>
        /// Copies a user-chosen image into the app's own AppData folder, so
        /// it keeps working even if the original file is later moved,
        /// renamed, or deleted. Overwrites any previously chosen background
        /// image. Returns the new stored path to save into
        /// <see cref="AppSettings.BackgroundImagePath"/>, or null if the
        /// copy failed (e.g. disk full, no permission, file in use) - the
        /// caller should keep whatever background was set before in that case.
        /// </summary>
        public static string? SaveBackgroundImage(string sourceFilePath)
        {
            try
            {
                Directory.CreateDirectory(BackgroundImageFolder);

                // Clear out any previously saved background first, in case
                // it used a different file extension than the new one -
                // otherwise both copies would sit there taking up space.
                DeleteBackgroundImage();

                string extension = Path.GetExtension(sourceFilePath);
                string destinationPath = Path.Combine(BackgroundImageFolder, "background" + extension);
                File.Copy(sourceFilePath, destinationPath, overwrite: true);
                return destinationPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Deletes the stored background image copy, if one exists. Never
        /// throws - if it can't be deleted for some reason, it's simply
        /// left there, which does no harm beyond a little leftover disk
        /// space (it's only ever read when <see cref="AppSettings.BackgroundImagePath"/>
        /// still points at it).
        /// </summary>
        public static void DeleteBackgroundImage()
        {
            try
            {
                if (Directory.Exists(BackgroundImageFolder))
                {
                    foreach (string file in Directory.GetFiles(BackgroundImageFolder))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // Not being able to delete the old file isn't harmful
                // enough to bother the user with an error about it.
            }
        }
    }
}
