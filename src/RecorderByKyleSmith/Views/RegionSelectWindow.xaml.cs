using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RecorderByKyleSmith.Views
{
    /// <summary>
    /// A full-screen, semi-transparent overlay (covering the primary
    /// monitor) that lets the user drag out a rectangle to record. Works
    /// with the mouse (click and drag) or the keyboard alone (arrow keys
    /// to move a default-sized box, Shift + arrow keys to resize it).
    ///
    /// Only the primary monitor is supported for now - selecting an area
    /// that spans more than one monitor is a possible future improvement.
    /// </summary>
    public partial class RegionSelectWindow : Window
    {
        private const double MinimumSize = 40;
        private const double KeyboardStep = 10;

        private Point? _dragStart;
        private Rect _selection = Rect.Empty;

        /// <summary>The confirmed selection, in physical screen pixels (what ScreenRecorderLib expects).</summary>
        public int SelectedX { get; private set; }
        public int SelectedY { get; private set; }
        public int SelectedWidth { get; private set; }
        public int SelectedHeight { get; private set; }

        public RegionSelectWindow()
        {
            InitializeComponent();
            // Cover exactly the primary monitor. SystemParameters here
            // gives values already adjusted for the primary monitor's own
            // scaling, matching how WPF positions windows.
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
        }

        private void RegionSelectWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(this);
            _selection = new Rect(_dragStart.Value, new Size(0, 0));
            UpdateSelectionVisual();
            CaptureMouse();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragStart == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point current = e.GetPosition(this);
            _selection = new Rect(_dragStart.Value, current);
            UpdateSelectionVisual();
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dragStart = null;
            ReleaseMouseCapture();
            RefreshConfirmEnabled();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                return;
            }

            if (e.Key == Key.Enter)
            {
                if (ConfirmButton.IsEnabled)
                {
                    ConfirmSelection();
                }
                return;
            }

            bool isArrowKey = e.Key is Key.Left or Key.Right or Key.Up or Key.Down;
            if (!isArrowKey)
            {
                return;
            }

            if (_selection.IsEmpty)
            {
                // No selection yet - start a reasonably large default box
                // centered on screen, so keyboard-only users have
                // something to work with right away.
                const double defaultWidth = 800;
                const double defaultHeight = 600;
                _selection = new Rect((Width - defaultWidth) / 2, (Height - defaultHeight) / 2, defaultWidth, defaultHeight);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                ResizeSelection(e.Key);
            }
            else
            {
                MoveSelection(e.Key);
            }

            ClampSelectionToScreen();
            UpdateSelectionVisual();
            RefreshConfirmEnabled();
            e.Handled = true;
        }

        private void ResizeSelection(Key key)
        {
            double width = _selection.Width;
            double height = _selection.Height;

            switch (key)
            {
                case Key.Right: width += KeyboardStep; break;
                case Key.Left: width = Math.Max(MinimumSize, width - KeyboardStep); break;
                case Key.Down: height += KeyboardStep; break;
                case Key.Up: height = Math.Max(MinimumSize, height - KeyboardStep); break;
            }

            _selection = new Rect(_selection.X, _selection.Y, width, height);
        }

        private void MoveSelection(Key key)
        {
            double x = _selection.X;
            double y = _selection.Y;

            switch (key)
            {
                case Key.Right: x += KeyboardStep; break;
                case Key.Left: x -= KeyboardStep; break;
                case Key.Down: y += KeyboardStep; break;
                case Key.Up: y -= KeyboardStep; break;
            }

            _selection = new Rect(x, y, _selection.Width, _selection.Height);
        }

        private void ClampSelectionToScreen()
        {
            double maxX = Math.Max(0, Width - _selection.Width);
            double maxY = Math.Max(0, Height - _selection.Height);
            double x = Math.Min(Math.Max(0, _selection.X), maxX);
            double y = Math.Min(Math.Max(0, _selection.Y), maxY);
            _selection = new Rect(x, y, _selection.Width, _selection.Height);
        }

        private void UpdateSelectionVisual()
        {
            if (_selection.Width < 1 || _selection.Height < 1)
            {
                SelectionRectangle.Visibility = Visibility.Collapsed;
                SizeReadoutText.Text = "No area selected yet.";
                return;
            }

            SelectionRectangle.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRectangle, _selection.X);
            Canvas.SetTop(SelectionRectangle, _selection.Y);
            SelectionRectangle.Width = _selection.Width;
            SelectionRectangle.Height = _selection.Height;

            SizeReadoutText.Text = $"Selected area: {(int)_selection.Width} x {(int)_selection.Height} pixels";
        }

        private void RefreshConfirmEnabled()
        {
            ConfirmButton.IsEnabled = _selection.Width >= MinimumSize && _selection.Height >= MinimumSize;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmSelection();
        }

        private void ConfirmSelection()
        {
            if (_selection.Width < MinimumSize || _selection.Height < MinimumSize)
            {
                return;
            }

            // WPF works in device-independent pixels; ScreenRecorderLib
            // wants physical screen pixels. This converts using this
            // window's actual DPI, so the recorded area matches the
            // selection correctly even on a scaled ("125%", "150%"...) display.
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            SelectedX = (int)Math.Round(_selection.X * dpi.DpiScaleX);
            SelectedY = (int)Math.Round(_selection.Y * dpi.DpiScaleY);
            SelectedWidth = (int)Math.Round(_selection.Width * dpi.DpiScaleX);
            SelectedHeight = (int)Math.Round(_selection.Height * dpi.DpiScaleY);

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
