using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace SimpleRecord.Services
{
    /// <summary>
    /// Builds a short trimmed .mp4 "clip" from a section of a finished
    /// recording, centered on a bookmarked moment. Used by the Bookmarks
    /// window's "Export Clip" button.
    ///
    /// Uses Windows' own built-in media-editing feature
    /// (Windows.Media.Editing) - part of Windows itself, the same
    /// category as the Media Foundation/Desktop Duplication components
    /// ScreenRecorderLib already relies on (see THIRD_PARTY_NOTICES.md),
    /// never any third-party or FFmpeg-based tool. This is also why the
    /// project's TargetFramework had to change to a more specific
    /// "windows10.0.19041.0"-style version (see the comment in the
    /// .csproj) - that's what lets C# call this particular Windows
    /// feature directly.
    ///
    /// Trade-off worth knowing: this RE-ENCODES the trimmed section - it
    /// is not a byte-for-byte "lossless" cut - so building a clip takes a
    /// few seconds and its quality/size depends on the fixed encoding
    /// profile below, rather than being an instant, exact slice of the
    /// original file. Output is capped at 1080p regardless of the
    /// original recording's chosen quality - a reasonable default for
    /// typical screen recordings, but a clip made from a 4K/8K recording
    /// would be downscaled to 1080p.
    /// </summary>
    public static class ClipExporter
    {
        /// <summary>
        /// Trims <paramref name="videoFilePath"/> down to the section
        /// between <paramref name="start"/> and <paramref name="end"/>
        /// (both measured from the very start of the video) and saves the
        /// result to <paramref name="outputClipPath"/>.
        /// </summary>
        public static async Task CreateClipAsync(string videoFilePath, string outputClipPath, TimeSpan start, TimeSpan end)
        {
            StorageFile inputFile = await StorageFile.GetFileFromPathAsync(videoFilePath).AsTask();
            MediaClip clip = await MediaClip.CreateFromFileAsync(inputFile).AsTask();

            if (start > TimeSpan.Zero)
            {
                clip.TrimTimeFromStart = start;
            }

            TimeSpan trimFromEnd = clip.OriginalDuration - end;
            if (trimFromEnd > TimeSpan.Zero)
            {
                clip.TrimTimeFromEnd = trimFromEnd;
            }

            var composition = new MediaComposition();
            composition.Clips.Add(clip);

            // StorageFile.GetFileFromPathAsync only opens a file that
            // already exists, so an empty placeholder is created first -
            // RenderToFileAsync below then overwrites it with the real
            // clip content.
            File.Create(outputClipPath).Dispose();
            StorageFile outputFile = await StorageFile.GetFileFromPathAsync(outputClipPath).AsTask();

            MediaEncodingProfile profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);

            await composition.RenderToFileAsync(outputFile, MediaTrimmingPreference.Precise, profile).AsTask();
        }
    }
}
