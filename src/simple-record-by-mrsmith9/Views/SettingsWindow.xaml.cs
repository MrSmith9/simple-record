using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SimpleRecord.Models;
using SimpleRecord.Services;

namespace SimpleRecord.Views
{
    public partial class SettingsWindow : Window
    {
        /// <summary>
        /// The settings to use after this dialog closes. Only meaningful
        /// when <see cref="Window.DialogResult"/> is true (Save was
        /// clicked) - on Cancel, the caller should keep using whatever
        /// settings it already had.
        /// </summary>
        public AppSettings ResultSettings { get; private set; }

        private string _selectedOutputFolder;

        // Set only after "Check for Updates" finds something newer - "Get
        // It" stays hidden until there's actually somewhere for it to send
        // the user.
        private UpdateInfo? _pendingUpdate;

        public SettingsWindow(AppSettings currentSettings)
        {
            InitializeComponent();

            ResultSettings = currentSettings;
            _selectedOutputFolder = currentSettings.OutputFolder;
            OutputFolderText.Text = _selectedOutputFolder;

            RadioButtonFor(currentSettings.Resolution).IsChecked = true;

            (currentSettings.Theme == AppTheme.Light ? LightThemeRadio : DarkThemeRadio).IsChecked = true;

            MicrophoneCheckBox.IsChecked = currentSettings.MicrophoneEnabled;
            ExportGifCheckBox.IsChecked = currentSettings.ExportGifEnabled;

            CurrentVersionText.Text = $"You're using version {UpdateChecker.FormatVersion(UpdateChecker.GetCurrentVersion())}.";
        }

        private RadioButton RadioButtonFor(VideoResolutionPreset preset) => preset switch
        {
            VideoResolutionPreset.R720p => R720Radio,
            VideoResolutionPreset.R1080p => R1080Radio,
            VideoResolutionPreset.R1440p => R1440Radio,
            VideoResolutionPreset.R2160p => R2160Radio,
            VideoResolutionPreset.R4320p => R4320Radio,
            _ => AutomaticRadio
        };

        private VideoResolutionPreset SelectedResolution()
        {
            if (R720Radio.IsChecked == true) return VideoResolutionPreset.R720p;
            if (R1080Radio.IsChecked == true) return VideoResolutionPreset.R1080p;
            if (R1440Radio.IsChecked == true) return VideoResolutionPreset.R1440p;
            if (R2160Radio.IsChecked == true) return VideoResolutionPreset.R2160p;
            if (R4320Radio.IsChecked == true) return VideoResolutionPreset.R4320p;
            return VideoResolutionPreset.Automatic;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Choose where to save recordings",
                InitialDirectory = Directory.Exists(_selectedOutputFolder)
                    ? _selectedOutputFolder
                    : AppSettings.DefaultOutputFolder
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedOutputFolder = dialog.FolderName;
                OutputFolderText.Text = _selectedOutputFolder;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ResultSettings = new AppSettings
            {
                OutputFolder = _selectedOutputFolder,
                Resolution = SelectedResolution(),
                Theme = LightThemeRadio.IsChecked == true ? AppTheme.Light : AppTheme.Dark,
                MicrophoneEnabled = MicrophoneCheckBox.IsChecked == true,
                ExportGifEnabled = ExportGifCheckBox.IsChecked == true
            };
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Checks GitHub right now, on demand, instead of waiting for the
        /// next time the app starts. Reports a clear result either way -
        /// found an update, confirmed up to date, or couldn't check at all
        /// (no internet, GitHub unreachable, or no release visible) - since
        /// someone pressing this button is asking a direct question and
        /// deserves a direct, honest answer rather than silence.
        /// </summary>
        private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdatesButton.IsEnabled = false;
            GetUpdateButton.Visibility = Visibility.Collapsed;
            _pendingUpdate = null;
            UpdateStatusText.Text = "Checking for updates...";

            (bool succeeded, UpdateInfo? update) =
                await UpdateChecker.CheckForUpdateWithStatusAsync(UpdateChecker.GetCurrentVersion());

            if (update != null)
            {
                _pendingUpdate = update;
                UpdateStatusText.Text = $"A new version is available: {update.VersionText}.";
                GetUpdateButton.Visibility = Visibility.Visible;
            }
            else if (succeeded)
            {
                UpdateStatusText.Text = "You're using the latest version.";
            }
            else
            {
                UpdateStatusText.Text =
                    "Couldn't check for updates right now. This usually means no internet " +
                    "connection, GitHub is temporarily unavailable, or no release has been " +
                    "published yet - try again later.";
            }

            CheckUpdatesButton.IsEnabled = true;
        }

        private void GetUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pendingUpdate == null)
            {
                return;
            }

            try
            {
                // UseShellExecute=true opens the link in the user's default
                // browser, the same as double-clicking it - it does not run
                // anything on its own.
                Process.Start(new ProcessStartInfo(_pendingUpdate.ReleasePageUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = $"Couldn't open the download page automatically: {ex.Message}";
            }
        }
    }
}
