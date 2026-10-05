using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SimpleRecord.Models;
using SimpleRecord.Services;
using SimpleRecord.Views;

namespace SimpleRecord
{
    public partial class MainWindow : Window
    {
        private readonly RecordingService _recordingService = new();
        private readonly DispatcherTimer _elapsedTimer = new() { Interval = TimeSpan.FromSeconds(1) };

        private DateTime _recordingStartedAt;
        private TimeSpan _pausedElapsed = TimeSpan.Zero;
        private RecordingSourceSelection _sourceSelection = RecordingSourceSelection.FullScreen();
        private UpdateInfo? _pendingUpdate;

        // Where to save, and what size to record at - loaded once at
        // startup, and updated/saved whenever the Settings window is used.
        private AppSettings _settings = SettingsService.Load();

        public MainWindow()
        {
            InitializeComponent();

            _recordingService.StatusMessage += (_, message) => Dispatcher.Invoke(() => SetStatus(message));
            _recordingService.RecordingCompleted += (_, filePath) => Dispatcher.Invoke(() => OnRecordingCompleted(filePath));
            _recordingService.RecordingFailed += (_, error) => Dispatcher.Invoke(() => OnRecordingFailed(error));

            _elapsedTimer.Tick += (_, _) => UpdateTimerDisplay();

            Loaded += (_, _) => StartButton.Focus();

            // Checking for an update happens quietly in the background after
            // the window is already shown, so it can never delay the app
            // opening or get in the way if there's no internet connection.
            Loaded += async (_, _) => await CheckForUpdatesAsync();

            ApplyBackgroundImage();
            UpdateSourceSelectionUi();
        }

        /// <summary>
        /// Shows the background image the user chose in Settings (if any)
        /// behind this window, or hides it so the plain dark background
        /// shows instead - which is also what happens if no background has
        /// been chosen, or the saved image can't be loaded for any reason
        /// (moved, deleted, or corrupted file). This never throws or shows
        /// an error - a background image is a visual nice-to-have, not
        /// something the app depends on.
        /// </summary>
        private void ApplyBackgroundImage()
        {
            string? path = _settings.BackgroundImagePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                BackgroundImage.Source = null;
                BackgroundImage.Visibility = Visibility.Collapsed;
                BackgroundScrim.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                // OnLoad reads the whole file into memory immediately and
                // releases the file handle, instead of keeping it open for
                // as long as the image is showing - without this, Settings
                // couldn't replace or delete the stored background file
                // while the main window is still open and using it.
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();

                BackgroundImage.Source = bitmap;
                BackgroundImage.Visibility = Visibility.Visible;
                BackgroundScrim.Visibility = Visibility.Visible;
            }
            catch
            {
                BackgroundImage.Source = null;
                BackgroundImage.Visibility = Visibility.Collapsed;
                BackgroundScrim.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Looks for a newer version on GitHub and, if one is found, shows
        /// the visual notice banner at the top of the window. Does nothing
        /// (no error, no popup) if there's no update, no internet, or the
        /// check isn't set up yet.
        /// </summary>
        private async Task CheckForUpdatesAsync()
        {
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            UpdateInfo? update = await UpdateChecker.CheckForUpdateAsync(currentVersion);
            if (update == null)
            {
                return;
            }

            _pendingUpdate = update;
            UpdateNoticeText.Text =
                $"Version {update.VersionText} is available - you're using {FormatVersion(currentVersion)}.";
            UpdateNoticeBorder.Visibility = Visibility.Visible;
        }

        private static string FormatVersion(Version version) => $"{version.Major}.{version.Minor}.{version.Build}";

        private void UpdateGetItButton_Click(object sender, RoutedEventArgs e)
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
                SetStatus($"Couldn't open the download page automatically: {ex.Message}");
            }
        }

        private void UpdateDismissButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateNoticeBorder.Visibility = Visibility.Collapsed;
        }

        private void FullScreenSourceButton_Click(object sender, RoutedEventArgs e)
        {
            _sourceSelection = RecordingSourceSelection.FullScreen();
            UpdateSourceSelectionUi();
        }

        private void WindowSourceButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WindowSelectDialog { Owner = this };
            bool? result = dialog.ShowDialog();
            if (result == true && dialog.SelectedWindow != null)
            {
                _sourceSelection = RecordingSourceSelection.ForWindow(dialog.SelectedWindow);
                UpdateSourceSelectionUi();
            }
        }

        private void RegionSourceButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the main window while selecting, so it's never in the
            // way of the area you're trying to select, then bring it back.
            WindowState previousState = WindowState;
            Hide();
            try
            {
                var picker = new RegionSelectWindow();
                bool? result = picker.ShowDialog();
                if (result == true)
                {
                    _sourceSelection = RecordingSourceSelection.ForRegion(
                        picker.SelectedX, picker.SelectedY, picker.SelectedWidth, picker.SelectedHeight);
                    UpdateSourceSelectionUi();
                }
            }
            finally
            {
                Show();
                WindowState = previousState;
            }
        }

        private void UpdateSourceSelectionUi()
        {
            SourceDescriptionText.Text = $"Recording: {_sourceSelection.DisplayText}";

            Brush selectedBrush = (Brush)FindResource("AccentBrush");
            Brush unselectedBrush = (Brush)FindResource("SurfaceBrush");

            FullScreenSourceButton.Background = _sourceSelection.Mode == RecordingSourceMode.FullScreen ? selectedBrush : unselectedBrush;
            WindowSourceButton.Background = _sourceSelection.Mode == RecordingSourceMode.Window ? selectedBrush : unselectedBrush;
            RegionSourceButton.Background = _sourceSelection.Mode == RecordingSourceMode.Region ? selectedBrush : unselectedBrush;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Always ask for confirmation before recording starts - this is
            // the "ask for permission" step, and it also doubles as a
            // reminder to check what's visible on screen first.
            var confirm = new ConfirmStartDialog(_sourceSelection.DisplayText) { Owner = this };
            bool? result = confirm.ShowDialog();
            if (result != true)
            {
                SetStatus("Recording was not started.");
                return;
            }

            _recordingService.Start(_settings.OutputFolder, _sourceSelection, _settings.Resolution);

            if (_recordingService.State == RecordingState.Recording)
            {
                _recordingStartedAt = DateTime.Now;
                _pausedElapsed = TimeSpan.Zero;
                _elapsedTimer.Start();
                UpdateTimerDisplay();
                SetRecordingUiState(isRecording: true, isPaused: false);
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_recordingService.State == RecordingState.Recording)
            {
                _recordingService.Pause();
                _pausedElapsed += DateTime.Now - _recordingStartedAt;
                _elapsedTimer.Stop();
                SetRecordingUiState(isRecording: true, isPaused: true);
            }
            else if (_recordingService.State == RecordingState.Paused)
            {
                _recordingService.Resume();
                _recordingStartedAt = DateTime.Now;
                _elapsedTimer.Start();
                SetRecordingUiState(isRecording: true, isPaused: false);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _elapsedTimer.Stop();
            StopButton.IsEnabled = false;
            PauseButton.IsEnabled = false;
            SetStatus("Finishing up and saving your recording...");
            _recordingService.Stop();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow { Owner = this };
            about.ShowDialog();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_settings) { Owner = this };
            bool? result = settingsWindow.ShowDialog();
            if (result != true)
            {
                return;
            }

            _settings = settingsWindow.ResultSettings;
            ApplyBackgroundImage();
            bool saved = SettingsService.Save(_settings);
            SetStatus(saved
                ? "Settings saved."
                : "Settings are being used for now, but couldn't be saved to disk - they may reset next time you open the app.");
        }

        private void OnRecordingCompleted(string filePath)
        {
            SetRecordingUiState(isRecording: false, isPaused: false);
            SetStatus($"Recording saved: {filePath}");
        }

        private void OnRecordingFailed(string error)
        {
            _elapsedTimer.Stop();
            SetRecordingUiState(isRecording: false, isPaused: false);
            SetStatus($"Something went wrong: {error}");
        }

        /// <summary>
        /// Updates every visible piece of UI that reflects the current
        /// state, in one place, so the indicator dot, the indicator text,
        /// the window title, and the enabled buttons can never disagree
        /// with each other.
        /// </summary>
        private void SetRecordingUiState(bool isRecording, bool isPaused)
        {
            StartButton.IsEnabled = !isRecording;
            PauseButton.IsEnabled = isRecording;
            StopButton.IsEnabled = isRecording;
            PauseButton.Content = isPaused ? "_Resume Recording" : "_Pause Recording";

            // The source can't be changed mid-recording.
            FullScreenSourceButton.IsEnabled = !isRecording;
            WindowSourceButton.IsEnabled = !isRecording;
            RegionSourceButton.IsEnabled = !isRecording;

            if (!isRecording)
            {
                IndicatorDot.Fill = (Brush)FindResource("TextSecondaryBrush");
                IndicatorStateText.Text = "Not recording";
                IndicatorStateText.Foreground = (Brush)FindResource("TextSecondaryBrush");
                Title = "Simple Record";
                StopPulse();
            }
            else if (isPaused)
            {
                IndicatorDot.Fill = (Brush)FindResource("AccentBrush");
                IndicatorStateText.Text = "Paused";
                IndicatorStateText.Foreground = (Brush)FindResource("AccentBrush");
                Title = "Paused - Simple Record";
                StopPulse();
            }
            else
            {
                IndicatorDot.Fill = (Brush)FindResource("RecordBrush");
                IndicatorStateText.Text = "Recording";
                IndicatorStateText.Foreground = (Brush)FindResource("RecordBrush");
                // "REC" (short for "recording") keeps the title bar compact
                // and matches the common red-dot "recording" convention.
                Title = "● REC - Simple Record";
                StartPulse();
            }
        }

        /// <summary>A slow, gentle fade used as a purely visual "recording is live" cue.</summary>
        private void StartPulse()
        {
            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.25,
                Duration = TimeSpan.FromMilliseconds(700),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            IndicatorDot.BeginAnimation(OpacityProperty, animation);
        }

        private void StopPulse()
        {
            IndicatorDot.BeginAnimation(OpacityProperty, null);
            IndicatorDot.Opacity = 1.0;
        }

        private void UpdateTimerDisplay()
        {
            TimeSpan elapsed = _pausedElapsed + (DateTime.Now - _recordingStartedAt);
            ElapsedTimeText.Text = elapsed.ToString(@"hh\:mm\:ss");
        }

        private void SetStatus(string message)
        {
            StatusMessageText.Text = message;
        }
    }
}
