using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace SimpleRecord.Views
{
    /// <summary>
    /// A thin, click-through red border shown on top of the real desktop
    /// while a Custom Area recording is running, drawn exactly around the
    /// pixels being recorded - so it's always obvious on screen which area
    /// is currently being captured, the same way the pulsing dot and timer
    /// already make it obvious that something is recording at all.
    ///
    /// Two Windows-specific tricks make this safe to leave on screen the
    /// whole time a recording runs:
    /// - WS_EX_TRANSPARENT makes every click and mouse movement pass
    ///   straight through this window to whatever is underneath it, so it
    ///   never gets in the way of using other apps while recording.
    /// - SetWindowDisplayAffinity(..., WDA_EXCLUDEFROMCAPTURE) tells
    ///   Windows to leave this specific window out of any screen capture -
    ///   this is the same mechanism apps like password managers and
    ///   meeting apps use to hide sensitive windows from a screen share -
    ///   so the red border itself never ends up baked into the recorded
    ///   video. This needs Windows 10 version 2004 (May 2020 Update) or
    ///   newer; on an older Windows 10, the border would show up in the
    ///   recording too, which is harmless (just not ideal) rather than an
    ///   error, so that case isn't specially detected or reported.
    /// </summary>
    public partial class RegionBoundaryOverlay : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        /// <param name="regionX">Left edge of the recorded area, in physical screen pixels.</param>
        /// <param name="regionY">Top edge of the recorded area, in physical screen pixels.</param>
        /// <param name="regionWidth">Width of the recorded area, in physical screen pixels.</param>
        /// <param name="regionHeight">Height of the recorded area, in physical screen pixels.</param>
        public RegionBoundaryOverlay(int regionX, int regionY, int regionWidth, int regionHeight)
        {
            InitializeComponent();

            // The region coordinates are in physical screen pixels (the
            // same ones ScreenRecorderLib uses), but WPF positions and
            // sizes windows in device-independent pixels - so they need
            // converting using this monitor's current DPI scale, the exact
            // reverse of the conversion RegionSelectWindow does when the
            // area is first selected.
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            Left = regionX / dpi.DpiScaleX;
            Top = regionY / dpi.DpiScaleY;
            Width = regionWidth / dpi.DpiScaleX;
            Height = regionHeight / dpi.DpiScaleY;
        }

        /// <summary>
        /// Fires once this window has a real native Windows handle to work
        /// with - too early in the constructor, since the handle doesn't
        /// exist until the window is actually being created.
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr handle = new WindowInteropHelper(this).Handle;

            int extendedStyle = GetWindowLong(handle, GWL_EXSTYLE);
            SetWindowLong(handle, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);

            SetWindowDisplayAffinity(handle, WDA_EXCLUDEFROMCAPTURE);
        }
    }
}
