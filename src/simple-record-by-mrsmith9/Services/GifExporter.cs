using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Openize.Animated.GIF;

namespace SimpleRecord.Services
{
    /// <summary>
    /// Builds a single animated .gif file from a folder of PNG snapshot
    /// images. RecordingService captures those PNGs automatically (via
    /// ScreenRecorderLib's SnapshotOptions) at a fixed interval WHILE a
    /// normal video recording runs, whenever the "Also save an animated
    /// GIF" setting is turned on - this class only handles turning that
    /// folder of still images into one .gif afterward.
    ///
    /// Uses the Openize.Animated-GIF library (Apache 2.0 license - see
    /// THIRD_PARTY_NOTICES.md) rather than FFmpeg or any GPL-licensed tool,
    /// matching this project's existing "no copyleft dependencies" rule
    /// (see the Tech stack notes in README.md).
    /// </summary>
    public static class GifExporter
    {
        /// <summary>
        /// Reads every PNG file in <paramref name="snapshotFolder"/>, oldest
        /// first (by last-write time, not file name - the exact naming
        /// ScreenRecorderLib uses for snapshot files isn't documented, so
        /// sorting by timestamp is the safe way to get them back in
        /// recording order), and encodes them into one looping animated GIF
        /// at <paramref name="outputGifPath"/> with <paramref
        /// name="frameDelayMillis"/> between frames.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the folder has no PNG files in it (nothing to build a
        /// GIF from) - the caller is expected to catch this, same as any
        /// other failure, and report it without crashing the app.
        /// </exception>
        public static void CreateFromSnapshotFolder(string snapshotFolder, string outputGifPath, int frameDelayMillis)
        {
            string[] frameFiles = Directory.GetFiles(snapshotFolder, "*.png")
                .OrderBy(File.GetLastWriteTimeUtc)
                .ToArray();

            if (frameFiles.Length == 0)
            {
                throw new InvalidOperationException("No snapshot images were captured during the recording.");
            }

            var encoder = new AnimatedGifEncoder();
            encoder.Start(outputGifPath);
            encoder.SetDelay(frameDelayMillis);
            encoder.SetRepeat(0); // 0 = loop forever, matching how GIFs normally behave.

            foreach (string frameFile in frameFiles)
            {
                // Bitmap is disposed after each AddFrame call so only one
                // decoded frame is held in memory at a time - a long
                // recording can have hundreds of snapshot files, and
                // keeping them all open at once would use a lot of memory
                // for no benefit.
                using var frame = new Bitmap(frameFile);
                encoder.AddFrame(frame);
            }

            encoder.Finish();
        }

        /// <summary>
        /// Same idea as <see cref="CreateFromSnapshotFolder"/>, but builds
        /// a short GIF from only the few frames captured AROUND one
        /// bookmarked moment, rather than the whole recording - used by
        /// the Bookmarks window's "Export GIF" button.
        ///
        /// Snapshot files aren't individually labeled with "how far into
        /// the recording" they were taken, so this estimates it from each
        /// file's own last-write time, compared against
        /// <paramref name="recordingStartedAtUtc"/> (when the recording
        /// itself began). This is an approximation, not an exact frame
        /// index - it's a close enough match for a quick preview GIF, but
        /// could drift slightly if the recording was paused and resumed
        /// (snapshot capture pauses too, but exactly how that lines up
        /// with file timestamps isn't something ScreenRecorderLib
        /// documents precisely).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no snapshot files fall within the requested time
        /// window - the caller is expected to catch this and report it
        /// without crashing the app, same as any other failure here.
        /// </exception>
        public static void CreateFromSnapshotWindow(string snapshotFolder, string outputGifPath, int frameDelayMillis,
            DateTime recordingStartedAtUtc, TimeSpan centerElapsed, TimeSpan before, TimeSpan after)
        {
            TimeSpan windowStart = centerElapsed > before ? centerElapsed - before : TimeSpan.Zero;
            TimeSpan windowEnd = centerElapsed + after;

            string[] frameFiles = Directory.GetFiles(snapshotFolder, "*.png")
                .Select(path => new { Path = path, Elapsed = File.GetLastWriteTimeUtc(path) - recordingStartedAtUtc })
                .Where(frame => frame.Elapsed >= windowStart && frame.Elapsed <= windowEnd)
                .OrderBy(frame => frame.Elapsed)
                .Select(frame => frame.Path)
                .ToArray();

            if (frameFiles.Length == 0)
            {
                throw new InvalidOperationException("No snapshot pictures were captured close enough to that moment.");
            }

            var encoder = new AnimatedGifEncoder();
            encoder.Start(outputGifPath);
            encoder.SetDelay(frameDelayMillis);
            encoder.SetRepeat(0);

            foreach (string frameFile in frameFiles)
            {
                using var frame = new Bitmap(frameFile);
                encoder.AddFrame(frame);
            }

            encoder.Finish();
        }
    }
}
