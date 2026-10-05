using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RecorderByKyleSmith.Models;

namespace RecorderByKyleSmith.Views
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

        public SettingsWindow(AppSettings currentSettings)
        {
            InitializeComponent();

            ResultSettings = currentSettings;
            _selectedOutputFolder = currentSettings.OutputFolder;
            OutputFolderText.Text = _selectedOutputFolder;

            RadioButtonFor(currentSettings.Resolution).IsChecked = true;
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
                Resolution = SelectedResolution()
            };
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
