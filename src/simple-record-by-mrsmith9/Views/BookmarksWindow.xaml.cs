using System;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using SimpleRecord.Models;
using SimpleRecord.Services;

namespace SimpleRecord.Views
{
    public partial class BookmarksWindow : Window
    {
        // How many seconds before/after a bookmarked moment a clip or GIF
        // export covers - fixed for now, not user-configurable.
        private static readonly TimeSpan Before = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan After = TimeSpan.FromSeconds(3);

        private readonly RecordingResult _result;

        public BookmarksWindow(RecordingResult result)
        {
            InitializeComponent();
            _result = result;
            BuildBookmarkRows();

            // The snapshot-picture folder (if there is one) is only ever
            // kept around for this window's sake - RecordingService hands
            // ownership of deleting it to whoever opens this window, so it
            // comes down whenever this window closes, whether or not
            // anything was actually exported.
            Closed += (_, _) => CleanUpSnapshotsFolder();
        }

        private void BuildBookmarkRows()
        {
            for (int i = 0; i < _result.Bookmarks.Count; i++)
            {
                Bookmark bookmark = _result.Bookmarks[i];
                int bookmarkNumber = i + 1;

                var row = new Border
                {
                    Style = (Style)FindResource("FieldBox"),
                    Padding = new Thickness(14, 12, 14, 12),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                var rowStack = new StackPanel();

                var titleText = new TextBlock
                {
                    Text = $"Bookmark {bookmarkNumber} - {bookmark.DisplayText}",
                    Style = (Style)FindResource("BodyText"),
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                rowStack.Children.Add(titleText);

                var buttonsStack = new StackPanel { Orientation = Orientation.Horizontal };
                var clipButton = new Button
                {
                    Content = "Export Clip",
                    Style = (Style)FindResource("SecondaryButton"),
                    Margin = new Thickness(0, 0, 10, 0)
                };
                var gifButton = new Button
                {
                    Content = "Export GIF",
                    Style = (Style)FindResource("SecondaryButton")
                };
                AutomationProperties.SetName(clipButton, $"Export Bookmark {bookmarkNumber} as Clip");
                AutomationProperties.SetHelpText(clipButton,
                    "Saves a short trimmed video (a few seconds around this moment) next to your recording.");
                AutomationProperties.SetName(gifButton, $"Export Bookmark {bookmarkNumber} as GIF");
                AutomationProperties.SetHelpText(gifButton,
                    "Saves a short animated GIF (a few seconds around this moment) next to your recording.");

                var statusText = new TextBlock
                {
                    Style = (Style)FindResource("BodyText"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 10, 0, 0),
                    Opacity = 0.85
                };

                clipButton.Click += async (_, _) => await ExportClipAsync(bookmark, bookmarkNumber, clipButton, gifButton, statusText);
                gifButton.Click += (_, _) => ExportGif(bookmark, bookmarkNumber, clipButton, gifButton, statusText);

                buttonsStack.Children.Add(clipButton);
                buttonsStack.Children.Add(gifButton);
                rowStack.Children.Add(buttonsStack);
                rowStack.Children.Add(statusText);
                row.Child = rowStack;
                BookmarksPanel.Children.Add(row);
            }
        }

        /// <summary>
        /// Builds and saves a short trimmed .mp4 "clip" centered on this
        /// bookmark's moment, via Windows' own built-in video-editing
        /// feature (see Services/ClipExporter.cs). Re-encodes, so this
        /// takes a few seconds - both buttons for this row are disabled
        /// while it runs so it can't be started twice at once.
        /// </summary>
        private async System.Threading.Tasks.Task ExportClipAsync(Bookmark bookmark, int bookmarkNumber,
            Button clipButton, Button gifButton, TextBlock statusText)
        {
            clipButton.IsEnabled = false;
            gifButton.IsEnabled = false;
            statusText.Text = "Building clip...";

            try
            {
                string outputFolder = Path.GetDirectoryName(_result.VideoFilePath) ?? AppSettings.DefaultOutputFolder;
                string baseName = Path.GetFileNameWithoutExtension(_result.VideoFilePath);
                string clipPath = Path.Combine(outputFolder, $"{baseName}_bookmark{bookmarkNumber}_clip.mp4");

                TimeSpan start = bookmark.Elapsed > Before ? bookmark.Elapsed - Before : TimeSpan.Zero;
                TimeSpan end = bookmark.Elapsed + After < _result.Duration ? bookmark.Elapsed + After : _result.Duration;

                await ClipExporter.CreateClipAsync(_result.VideoFilePath, clipPath, start, end);
                statusText.Text = $"Clip saved: {clipPath}";
            }
            catch (Exception ex)
            {
                statusText.Text = $"Couldn't build the clip: {ex.Message}";
            }
            finally
            {
                clipButton.IsEnabled = true;
                gifButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Builds and saves a short animated .gif centered on this
        /// bookmark's moment, from the recording's already-captured
        /// snapshot pictures (see Services/GifExporter.cs). Does nothing
        /// but show a clear message if those pictures aren't available
        /// for this recording (snapshot capture can fail to set up, same
        /// as any other disk operation - see RecordingService.Start).
        /// </summary>
        private void ExportGif(Bookmark bookmark, int bookmarkNumber, Button clipButton, Button gifButton, TextBlock statusText)
        {
            if (_result.SnapshotsFolder == null)
            {
                statusText.Text = "Couldn't build a GIF - snapshot capture wasn't available for this recording.";
                return;
            }

            clipButton.IsEnabled = false;
            gifButton.IsEnabled = false;
            statusText.Text = "Building GIF...";

            try
            {
                string outputFolder = Path.GetDirectoryName(_result.VideoFilePath) ?? AppSettings.DefaultOutputFolder;
                string baseName = Path.GetFileNameWithoutExtension(_result.VideoFilePath);
                string gifPath = Path.Combine(outputFolder, $"{baseName}_bookmark{bookmarkNumber}.gif");

                GifExporter.CreateFromSnapshotWindow(
                    _result.SnapshotsFolder, gifPath, RecordingService.GifFrameIntervalMillis,
                    _result.RecordingStartedAtUtc, bookmark.Elapsed, Before, After);

                statusText.Text = $"GIF saved: {gifPath}";
            }
            catch (Exception ex)
            {
                statusText.Text = $"Couldn't build the GIF: {ex.Message}";
            }
            finally
            {
                clipButton.IsEnabled = true;
                gifButton.IsEnabled = true;
            }
        }

        private void CleanUpSnapshotsFolder()
        {
            if (_result.SnapshotsFolder == null)
            {
                return;
            }

            try
            {
                Directory.Delete(_result.SnapshotsFolder, recursive: true);
            }
            catch
            {
                // Leftover temp folder, not worth bothering the user about.
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
