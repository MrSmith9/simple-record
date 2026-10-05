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
        private string? _selectedBackgroundPath;

        public SettingsWindow(AppSettings currentSettings)
        {
            InitializeComponent();

            ResultSettings = currentSettings;
            _selectedOutputFolder = currentSettings.OutputFolder;
            OutputFolderText.Text = _selectedOutputFolder;

            RadioButtonFor(currentSettings.Resolution).IsChecked = true;

            _selectedBackgroundPath = currentSettings.BackgroundImagePath;
            UpdateBackgroundDisplay();

            MicrophoneCheckBox.IsChecked = currentSettings.MicrophoneEnabled;
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

        /// <summary>
        /// Opens a picture picker and, if the user chooses a file, copies it
        /// into the app's own AppData folder (via
        /// <see cref="SettingsService.SaveBackgroundImage"/>) so it keeps
        /// working even if the original file is later moved or deleted.
        /// </summary>
        private void ChooseBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose a background image",
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string? storedPath = SettingsService.SaveBackgroundImage(dialog.FileName);
            if (storedPath == null)
            {
                MessageBox.Show(this,
                    "That picture couldn't be copied into the app's settings folder, so it wasn't set as the background. Your previous background (if any) is unchanged.",
                    "Couldn't set background", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _selectedBackgroundPath = storedPath;
            UpdateBackgroundDisplay();
        }

        private void RemoveBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.DeleteBackgroundImage();
            _selectedBackgroundPath = null;
            UpdateBackgroundDisplay();
        }

        private void UpdateBackgroundDisplay()
        {
            BackgroundStatusText.Text = _selectedBackgroundPath == null
                ? "No custom background (using the plain dark background)"
                : Path.GetFileName(_selectedBackgroundPath);
            RemoveBackgroundButton.IsEnabled = _selectedBackgroundPath != null;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ResultSettings = new AppSettings
            {
                OutputFolder = _selectedOutputFolder,
                Resolution = SelectedResolution(),
                BackgroundImagePath = _selectedBackgroundPath,
                MicrophoneEnabled = MicrophoneCheckBox.IsChecked == true
            };
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
