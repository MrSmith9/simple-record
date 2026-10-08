using System;
using System.Collections.Generic;

namespace SimpleRecord.Models
{
    /// <summary>
    /// Everything the Bookmarks window needs about a just-finished
    /// recording that had at least one bookmark set during it. Handed
    /// from <see cref="Services.RecordingService"/> to MainWindow (via
    /// <see cref="Services.RecordingService.BookmarksReady"/>) the moment
    /// recording finishes, and from there straight into a new
    /// <see cref="Views.BookmarksWindow"/>.
    /// </summary>
    public class RecordingResult
    {
        /// <summary>Full path of the finished .mp4 recording.</summary>
        public string VideoFilePath { get; }

        /// <summary>
        /// Full path of the temporary folder holding this recording's
        /// periodic PNG snapshots, or null if that folder couldn't be
        /// created (see RecordingService.Start) - in that case, "Export
        /// GIF" isn't possible for this recording's bookmarks, only
        /// "Export Clip". Whoever finishes using this folder (the
        /// Bookmarks window, when closed) is responsible for deleting it -
        /// it's temporary working data, never something the user needs to
        /// see or clean up themselves.
        /// </summary>
        public string? SnapshotsFolder { get; }

        /// <summary>Total length of the recording, used to keep a clip/GIF from running past the end.</summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// The real wall-clock moment (UTC) recording started - needed to
        /// match a snapshot file's own last-write time back to "how far
        /// into the recording" it was taken, since ScreenRecorderLib
        /// doesn't label each snapshot file with that directly.
        /// </summary>
        public DateTime RecordingStartedAtUtc { get; }

        public List<Bookmark> Bookmarks { get; }

        public RecordingResult(string videoFilePath, string? snapshotsFolder, TimeSpan duration,
            DateTime recordingStartedAtUtc, List<Bookmark> bookmarks)
        {
            VideoFilePath = videoFilePath;
            SnapshotsFolder = snapshotsFolder;
            Duration = duration;
            RecordingStartedAtUtc = recordingStartedAtUtc;
            Bookmarks = bookmarks;
        }
    }
}
