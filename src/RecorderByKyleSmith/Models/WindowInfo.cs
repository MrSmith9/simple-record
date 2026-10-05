using System;

namespace RecorderByKyleSmith.Models
{
    /// <summary>
    /// A lightweight, app-level stand-in for a recordable window, so that
    /// the UI and dialogs never need to reference ScreenRecorderLib types
    /// directly - only RecordingService does that.
    /// </summary>
    public class WindowInfo
    {
        public string Title { get; }
        public IntPtr Handle { get; }

        public WindowInfo(string title, IntPtr handle)
        {
            Title = title;
            Handle = handle;
        }

        /// <summary>
        /// WPF's ListBox shows this automatically for each item when no
        /// other display template is set, so the window list just shows
        /// readable titles.
        /// </summary>
        public override string ToString() => Title;
    }
}
