using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        private RegionBoundaryOverlay? _regionBoundaryOverlay;

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

            UpdateSourceSelectionUi();

            // Safety net: if the app is closed while a Custom Area
            // recording is still running, make sure the red border doesn't
            // get left behind as an orphaned window on the desktop.
            Closing += (_, _) => CloseRegionBoundaryOverlay();
        }

        /// <summary>
        /// Lets the user drag the window by clicking and holding anywhere
        /// on the custom title bar row - with WindowStyle="None" there's no
        /// normal Windows title bar to drag by default, so this replaces
        /// that. The menu/close buttons sit on top of this same row but
        /// still receive their own clicks normally; this only fires when
        /// the click lands on the title bar itself.
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// A hand-wired Alt+F4 as a safety net. WPF windows are normally
        /// expected to still close on Alt+F4 even with a custom title bar
        /// (WindowStyle="None"), but that's hard to verify without being
        /// able to run the app here - this guarantees it either way.
        /// </summary>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F4 && Keyboard.Modifiers == ModifierKeys.Alt)
            {
                Close();
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            MenuButton.ContextMenu.IsOpen = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

            // The selected source button gets a subtle accent-colored tint
            // behind it plus a solid accent-colored border - two light
            // touches rather than one solid fill. A solid accent fill
            // would hide each button's own colored icon (e.g. the blue
            // "Whole Screen" icon would vanish against a solid blue
            // background), so this keeps every icon visible no matter
            // which source is currently selected.
            //
            // 2026-10-08: the tint color is now read from the active
            // theme's AccentBrush at the moment this runs, instead of
            // being a fixed blue - previously this was hardcoded to the
            // Dark theme's exact accent blue, so after switching to the
            // Light theme (a different, deeper blue) the tint and the
            // border around it would have been two slightly different
            // blues instead of matching.
            Brush selectedBorder = (Brush)FindResource("AccentBrush");
            Color accentColor = selectedBorder is SolidColorBrush accentBrush ? accentBrush.Color : Colors.DodgerBlue;
            var selectedBackground = new SolidColorBrush(Color.FromArgb(0x26, accentColor.R, accentColor.G, accentColor.B));
            Brush unselectedBackground = Brushes.Transparent;
            Brush unselectedBorder = Brushes.Transparent;

            SetSourceButtonSelected(FullScreenSourceButton, _sourceSelection.Mode == RecordingSourceMode.FullScreen,
                selectedBackground, selectedBorder, unselectedBackground, unselectedBorder);
            SetSourceButtonSelected(WindowSourceButton, _sourceSelection.Mode == RecordingSourceMode.Window,
                selectedBackground, selectedBorder, unselectedBackground, unselectedBorder);
            SetSourceButtonSelected(RegionSourceButton, _sourceSelection.Mode == RecordingSourceMode.Region,
                selectedBackground, selectedBorder, unselectedBackground, unselectedBorder);
        }

        private static void SetSourceButtonSelected(System.Windows.Controls.Button button, bool isSelected,
            Brush selectedBackground, Brush selectedBorder, Brush unselectedBackground, Brush unselectedBorder)
        {
            button.Background = isSelected ? selectedBackground : unselectedBackground;
            button.BorderBrush = isSelected ? selectedBorder : unselectedBorder;
            button.BorderThickness = new Thickness(isSelected ? 1.5 : 0);
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

            _recordingService.Start(_settings.OutputFolder, _sourceSelection, _settings.Resolution, _settings.MicrophoneEnabled);

            if (_recordingService.State == RecordingState.Recording)
            {
                _recordingStartedAt = DateTime.Now;
                _pausedElapsed = TimeSpan.Zero;
                _elapsedTimer.Start();
                UpdateTimerDisplay();
                SetRecordingUiState(isRecording: true, isPaused: false);
                ShowRegionBoundaryIfNeeded();
            }
        }

        /// <summary>
        /// Shows a thin red click-through border around the exact area
        /// being recorded, but only for Custom Area recordings - Whole
        /// Screen and Window recordings don't need it, since their
        /// boundary (the whole monitor, or the window's own edges) is
        /// already obvious without any extra help.
        /// </summary>
        private void ShowRegionBoundaryIfNeeded()
        {
            if (_sourceSelection.Mode != RecordingSourceMode.Region)
            {
                return;
            }

            _regionBoundaryOverlay = new RegionBoundaryOverlay(
                _sourceSelection.RegionX, _sourceSelection.RegionY,
                _sourceSelection.RegionWidth, _sourceSelection.RegionHeight);
            _regionBoundaryOverlay.Show();
        }

        private void CloseRegionBoundaryOverlay()
        {
            _regionBoundaryOverlay?.Close();
            _regionBoundaryOverlay = null;
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

            AppTheme previousTheme = _settings.Theme;
            _settings = settingsWindow.ResultSettings;
            bool saved = SettingsService.Save(_settings);

            // 2026-10-08: switching the theme used to need a full app
            // restart to actually show (StaticResource colors are only
            // read once, when a window is built - the open MainWindow
            // couldn't re-color itself on its own). Kyle reported this as
            // "click light, not change to light." Fixed below: if the
            // theme changed and nothing is recording, apply it right away
            // and replace this window with a freshly-built one, so the
            // new theme shows immediately without closing the whole app.
            bool themeChanged = _settings.Theme != previousTheme;
            if (themeChanged && _recordingService.State == RecordingState.Idle)
            {
                ThemeManager.Apply(_settings.Theme);
                ReopenWithNewTheme();
                return;
            }

            SetStatus(saved
                ? (themeChanged
                    ? "Settings saved. The new theme will show the next time you open Simple Record (it can't switch while a recording is in progress)."
                    : "Settings saved.")
                : "Settings are being used for now, but couldn't be saved to disk - they may reset next time you open the app.");
        }

        /// <summary>
        /// Replaces this window with a brand new one, so the just-applied
        /// theme's colors actually show without needing to restart the
        /// whole app. Safe to call only while idle (not recording) - see
        /// the caller, SettingsButton_Click.
        /// </summary>
        private void ReopenWithNewTheme()
        {
            var freshWindow = new MainWindow();
            Application.Current.MainWindow = freshWindow;
            freshWindow.Show();
            Close();
        }

        private void OnRecordingCompleted(string filePath)
        {
            CloseRegionBoundaryOverlay();
            SetRecordingUiState(isRecording: false, isPaused: false);
            SetStatus($"Recording saved: {filePath}");
        }

        private void OnRecordingFailed(string error)
        {
            _elapsedTimer.Stop();
            CloseRegionBoundaryOverlay();
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
            // The idle "choose a source + Record" row and the live
            // "recording status + Pause/Stop" row occupy the same spot in
            // the compact window and are swapped based on state, rather
            // than both being visible at once.
            SourceSelectionPanel.Visibility = isRecording ? Visibility.Collapsed : Visibility.Visible;
            RecordingPanel.Visibility = isRecording ? Visibility.Visible : Visibility.Collapsed;

            StartButton.IsEnabled = !isRecording;
            PauseButton.IsEnabled = isRecording;
            StopButton.IsEnabled = isRecording;

            // PauseButton's Content is an icon+label layout, not plain
            // text, so only the label TextBlock's Text is updated here -
            // overwriting PauseButton.Content directly would wipe out the
            // icon. AutomationProperties.Name is updated alongside it so
            // screen readers still announce "Resume Recording"/"Pause
            // Recording" correctly (that name isn't read from Content
            // automatically once Content stops being a plain string).
            PauseButtonLabel.Text = isPaused ? "Resume" : "Pause";
            AutomationProperties.SetName(PauseButton, isPaused ? "Resume Recording" : "Pause Recording");

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
