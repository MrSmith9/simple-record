using System;

namespace SimpleRecord.Models
{
    /// <summary>
    /// One moment marked during a recording (via the Bookmark button, or
    /// the F8 shortcut) so it can be turned into a short clip or GIF
    /// afterward, in the Bookmarks window. <see cref="Elapsed"/> is the
    /// time *into the recording* (matching what the on-screen timer
    /// showed at that moment), not a real wall-clock time - that's what
    /// <see cref="Services.ClipExporter"/> and <see cref="Services.GifExporter"/>
    /// need to find the right few seconds in the finished video/snapshots.
    /// </summary>
    public class Bookmark
    {
        public TimeSpan Elapsed { get; }

        public Bookmark(TimeSpan elapsed)
        {
            Elapsed = elapsed;
        }

        /// <summary>Shown in the Bookmarks window, e.g. "00:01:23".</summary>
        public string DisplayText => Elapsed.ToString(@"hh\:mm\:ss");
    }
}
