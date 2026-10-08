using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimpleRecord.Models;
using ScreenRecorderLib;

namespace SimpleRecord.Services
{
    /// <summary>
    /// Wraps the ScreenRecorderLib recording engine and exposes a simple
    /// Start / Pause / Resume / Stop API plus events the UI can subscribe
    /// to. Keeping all the ScreenRecorderLib-specific code in this one
    /// file means that if we ever need to change how recording works
    /// internally, only this file has to change - MainWindow never talks
    /// to ScreenRecorderLib directly.
    ///
    /// Records to an MP4 (H.264) file. The source can be the whole primary
    /// monitor, one specific window, or a custom pixel area on the primary
    /// monitor - see <see cref="RecordingSourceSelection"/>. Microphone
    /// audio is optional (off unless the caller asks for it); system/
    /// speaker audio is not offered yet.
    /// </summary>
    public class RecordingService
    {
        // How often a still frame is captured, in milliseconds - 200ms is
        // 5 frames per second. This now always runs during every
        // recording (not just when "Also save an animated GIF" is on),
        // since it's also what lets ANY bookmark be turned into a short
        // GIF afterward in the Bookmarks window. GIFs/bookmark-previews
        // don't need anywhere near video's 30fps to look fine, and a
        // lower frame rate keeps file size and build time reasonable.
        // Public because Views/BookmarksWindow.xaml.cs reuses the same
        // value when building a per-bookmark GIF.
        public const int GifFrameIntervalMillis = 200;

        private Recorder? _recorder;
        private bool _exportGifEnabled;
        private string? _gifSnapshotsFolder;
        private DateTime _recordingStartedAtUtc;
        private List<Bookmark> _bookmarks = new();
        private TimeSpan _finalElapsed;

        public RecordingState State { get; private set; } = RecordingState.Idle;

        /// <summary>Full path of the file currently being recorded, if any.</summary>
        public string? CurrentFilePath { get; private set; }

        /// <summary>Raised for non-critical updates the user might want to see.</summary>
        public event EventHandler<string>? StatusMessage;

        /// <summary>Raised once recording has fully stopped and the file is ready.</summary>
        public event EventHandler<string>? RecordingCompleted;

        /// <summary>Raised when recording could not start, or failed while running.</summary>
        public event EventHandler<string>? RecordingFailed;

        /// <summary>
        /// Raised right after <see cref="RecordingCompleted"/> (and after
        /// a whole-recording GIF, if that setting is on, has finished
        /// building), but only when at least one bookmark was set during
        /// the recording. MainWindow listens for this to open the
        /// Bookmarks window automatically.
        /// </summary>
        public event EventHandler<RecordingResult>? BookmarksReady;

        /// <summary>
        /// Lists the windows that can currently be recorded, for the
        /// "choose a window" dialog. Windows with no title (background /
        /// system windows) are filtered out since they're not useful to
        /// pick from and would just clutter the list.
        /// </summary>
        public static List<WindowInfo> GetRecordableWindows()
        {
            return Recorder.GetWindows()
                .Where(window => !string.IsNullOrWhiteSpace(window.Title))
                .Select(window => new WindowInfo(window.Title, window.Handle))
                .ToList();
        }

        /// <summary>
        /// Starts recording to an MP4 file inside the given folder, using
        /// whatever source (whole screen / window / custom area) is
        /// described by <paramref name="source"/>, saved at the given
        /// output size (or the source's own size, if <paramref
        /// name="resolution"/> is Automatic), with or without microphone
        /// audio depending on <paramref name="microphoneEnabled"/>. The
        /// file name is generated automatically from the current date and
        /// time, so recordings never overwrite each other.
        /// </summary>
        public void Start(string outputFolder, RecordingSourceSelection source, VideoResolutionPreset resolution, bool microphoneEnabled, bool exportGifEnabled)
        {
            if (State != RecordingState.Idle)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(outputFolder);
            }
            catch (Exception ex)
            {
                RecordingFailed?.Invoke(this, $"Could not create the output folder: {ex.Message}");
                return;
            }

            string fileName = $"Recording_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.mp4";
            CurrentFilePath = Path.Combine(outputFolder, fileName);

            _exportGifEnabled = exportGifEnabled;
            _bookmarks = new List<Bookmark>();

            // A folder named after the recording itself, right next to
            // where a .gif would end up, so stray leftovers (if cleanup
            // ever fails to run) are easy to recognize and delete by
            // hand. This now always runs (not just when "Also save an
            // animated GIF" is on) - see the GifFrameIntervalMillis
            // comment above for why. Failing to create it is treated as
            // "GIF/bookmark-preview capture isn't going to happen" rather
            // than stopping the whole recording - the video is what
            // matters most.
            _gifSnapshotsFolder = null;
            try
            {
                string snapshotsFolder = Path.Combine(outputFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_gif_frames");
                Directory.CreateDirectory(snapshotsFolder);
                _gifSnapshotsFolder = snapshotsFolder;
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"Couldn't set up snapshot capture (affects the GIF/bookmark-preview features only, video recording is unaffected): {ex.Message}");
            }

            var outputOptions = new OutputOptions
            {
                RecorderMode = RecorderMode.Video
            };

            // Automatic (PixelSize() returns null) leaves OutputFrameSize
            // unset, which tells ScreenRecorderLib to just use whatever
            // size is actually being recorded. For a fixed size, Stretch
            // defaults to Uniform, which keeps the real picture's shape
            // intact and adds black bars rather than distorting it, if the
            // chosen size doesn't match the shape of the source.
            (int Width, int Height)? fixedSize = resolution.PixelSize();
            if (fixedSize != null)
            {
                outputOptions.OutputFrameSize = new ScreenSize(fixedSize.Value.Width, fixedSize.Value.Height);
            }

            var options = new RecorderOptions
            {
                SourceOptions = new SourceOptions
                {
                    RecordingSources = BuildRecordingSources(source)
                },
                OutputOptions = outputOptions,
                // Only set when the snapshots folder above was created
                // successfully. ScreenRecorderLib then writes one PNG
                // still-frame into that folder every GifFrameIntervalMillis
                // *while the normal video recording runs* - this is what
                // lets GifExporter build a .gif afterward (the whole
                // recording, if "Also save an animated GIF" is on, or a
                // short one around a bookmark) without ever having to
                // decode the finished .mp4 itself.
                SnapshotOptions = _gifSnapshotsFolder != null
                    ? new SnapshotOptions
                    {
                        SnapshotsWithVideo = true,
                        SnapshotsIntervalMillis = GifFrameIntervalMillis,
                        SnapshotFormat = ImageFormat.PNG,
                        SnapshotsDirectory = _gifSnapshotsFolder
                    }
                    : null,
                AudioOptions = new AudioOptions
                {
                    // IsAudioEnabled is the master switch for the audio
                    // track as a whole. AudioSources is the actual list of
                    // what gets recorded into it - CaptureAudioSource.Default
                    // is whichever microphone is currently set as default in
                    // Windows. System/speaker audio (LoopbackAudioSource)
                    // isn't offered yet - only microphone was asked for so
                    // far - so the list is left empty when the microphone
                    // is off, meaning no audio at all gets recorded.
                    IsAudioEnabled = microphoneEnabled,
                    AudioSources = microphoneEnabled
                        ? new List<AudioSourceBase> { CaptureAudioSource.Default }
                        : new List<AudioSourceBase>()
                },
                VideoEncoderOptions = new VideoEncoderOptions
                {
                    Bitrate = 8000 * 1000,
                    Framerate = 30,
                    IsFixedFramerate = true,
                    IsHardwareEncodingEnabled = true,
                    Encoder = new H264VideoEncoder
                    {
                        BitrateMode = H264BitrateControlMode.CBR,
                        EncoderProfile = H264Profile.High
                    }
                }
            };

            try
            {
                _recorder = Recorder.CreateRecorder(options);
                _recorder.OnRecordingComplete += OnRecordingComplete;
                _recorder.OnRecordingFailed += OnRecordingFailedInternal;
                _recorder.OnStatusChanged += OnStatusChanged;

                _recordingStartedAtUtc = DateTime.UtcNow;
                _recorder.Record(CurrentFilePath);
                State = RecordingState.Recording;
                StatusMessage?.Invoke(this, $"Recording started ({source.DisplayText}). Saving to: {CurrentFilePath}");
            }
            catch (Exception ex)
            {
                State = RecordingState.Idle;
                CleanUpRecorder();
                CleanUpSnapshotsFolderIfAny();
                RecordingFailed?.Invoke(this, $"Could not start recording: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds the ScreenRecorderLib recording source list for the
        /// chosen mode. This is the one place that translates our own
        /// simple "what to record" model into ScreenRecorderLib's types.
        /// </summary>
        private static List<RecordingSourceBase> BuildRecordingSources(RecordingSourceSelection source)
        {
            switch (source.Mode)
            {
                case RecordingSourceMode.Window:
                    return new List<RecordingSourceBase>
                    {
                        new WindowRecordingSource(source.WindowHandle)
                    };

                case RecordingSourceMode.Region:
                    // Start from the main monitor, then crop it down to the
                    // exact area the user dragged out. ScreenRecorderLib
                    // automatically sizes the output video to match this
                    // cropped area, so the saved file is exactly that size.
                    var croppedMonitor = new DisplayRecordingSource(DisplayRecordingSource.MainMonitor)
                    {
                        SourceRect = new ScreenRect(source.RegionX, source.RegionY, source.RegionWidth, source.RegionHeight)
                    };
                    return new List<RecordingSourceBase> { croppedMonitor };

                case RecordingSourceMode.FullScreen:
                default:
                    return new List<RecordingSourceBase>
                    {
                        new DisplayRecordingSource(DisplayRecordingSource.MainMonitor)
                    };
            }
        }

        public void Pause()
        {
            if (State != RecordingState.Recording || _recorder == null)
            {
                return;
            }

            _recorder.Pause();
            State = RecordingState.Paused;
            StatusMessage?.Invoke(this, "Recording paused.");
        }

        public void Resume()
        {
            if (State != RecordingState.Paused || _recorder == null)
            {
                return;
            }

            _recorder.Resume();
            State = RecordingState.Recording;
            StatusMessage?.Invoke(this, "Recording resumed.");
        }

        /// <summary>
        /// Marks the current moment (while Recording or Paused) so it can
        /// be turned into a short clip or GIF after the recording
        /// finishes, in the Bookmarks window. Does nothing (with a status
        /// message explaining why) if called while not actually
        /// recording - <paramref name="elapsed"/> is the time into the
        /// recording the caller has already computed (MainWindow already
        /// tracks this for the on-screen timer), not a value this service
        /// works out itself.
        /// </summary>
        public void AddBookmark(TimeSpan elapsed)
        {
            if (State != RecordingState.Recording && State != RecordingState.Paused)
            {
                StatusMessage?.Invoke(this, "Bookmark needs an active recording - start recording first.");
                return;
            }

            _bookmarks.Add(new Bookmark(elapsed));
            StatusMessage?.Invoke(this, $"Bookmark added at {elapsed:hh\\:mm\\:ss}.");
        }

        /// <summary>
        /// Stops recording. <paramref name="finalElapsed"/> is the total
        /// recording length as the caller has already computed it (same
        /// value the on-screen timer was showing) - needed so any
        /// bookmarks' clip/GIF exports can be kept from running past the
        /// end of the recording.
        /// </summary>
        public void Stop(TimeSpan finalElapsed)
        {
            if (State == RecordingState.Idle || _recorder == null)
            {
                return;
            }

            _finalElapsed = finalElapsed;
            _recorder.Stop();
            // State goes back to Idle inside OnRecordingComplete, once
            // ScreenRecorderLib confirms the file has been finalized and
            // closed - stopping and finishing writing the file is not
            // instant, so we wait for that event rather than assuming.
        }

        private void OnRecordingComplete(object? sender, RecordingCompleteEventArgs e)
        {
            State = RecordingState.Idle;
            bool shouldExportGif = _exportGifEnabled;
            string? snapshotsFolder = _gifSnapshotsFolder;
            List<Bookmark> bookmarks = _bookmarks;
            DateTime recordingStartedAtUtc = _recordingStartedAtUtc;
            TimeSpan duration = _finalElapsed;
            CleanUpRecorder();
            RecordingCompleted?.Invoke(this, e.FilePath);

            // Done after RecordingCompleted fires, not before - the video
            // file itself is already finished and ready to use at this
            // point, so there's no reason to make the user wait on the GIF
            // (which can take a few seconds for a longer recording) before
            // the app tells them the recording is done.
            //
            // The snapshots folder is only deleted here if nothing else
            // needs it - if there are bookmarks, the Bookmarks window
            // needs those same snapshot pictures for "Export GIF", so it
            // takes ownership of deleting the folder itself once closed
            // (see BookmarksReady below and Views/BookmarksWindow.xaml.cs).
            bool bookmarksExist = bookmarks.Count > 0;

            if (shouldExportGif && snapshotsFolder != null)
            {
                ExportGifFromSnapshots(e.FilePath, snapshotsFolder, deleteFolderAfter: !bookmarksExist);
            }
            else if (snapshotsFolder != null && !bookmarksExist)
            {
                CleanUpSnapshotsFolder(snapshotsFolder);
            }

            if (bookmarksExist)
            {
                BookmarksReady?.Invoke(this,
                    new RecordingResult(e.FilePath, snapshotsFolder, duration, recordingStartedAtUtc, bookmarks));
            }
        }

        /// <summary>
        /// Builds the companion .gif for a just-finished recording from
        /// its folder of PNG snapshots. When <paramref
        /// name="deleteFolderAfter"/> is true, the snapshot folder is
        /// also deleted afterward either way - it's temporary working
        /// data, never something the user needs to see or clean up
        /// themselves. When false, the caller (OnRecordingComplete, when
        /// bookmarks also exist) keeps the folder around for the
        /// Bookmarks window to use. Failure here only loses the GIF; the
        /// video recording the user actually asked to make is already
        /// safe on disk by the time this method runs.
        /// </summary>
        private void ExportGifFromSnapshots(string videoFilePath, string snapshotsFolder, bool deleteFolderAfter)
        {
            try
            {
                string gifPath = Path.ChangeExtension(videoFilePath, ".gif");
                StatusMessage?.Invoke(this, "Building animated GIF...");
                GifExporter.CreateFromSnapshotFolder(snapshotsFolder, gifPath, GifFrameIntervalMillis);
                StatusMessage?.Invoke(this, $"GIF saved to: {gifPath}");
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"Couldn't build the GIF, but your video is fine: {ex.Message}");
            }
            finally
            {
                if (deleteFolderAfter)
                {
                    CleanUpSnapshotsFolder(snapshotsFolder);
                }
            }
        }

        private static void CleanUpSnapshotsFolder(string snapshotsFolder)
        {
            try
            {
                Directory.Delete(snapshotsFolder, recursive: true);
            }
            catch
            {
                // Not worth bothering the user about a leftover temp
                // folder - at worst it just sits there unused.
            }
        }

        private void CleanUpSnapshotsFolderIfAny()
        {
            if (_gifSnapshotsFolder != null)
            {
                CleanUpSnapshotsFolder(_gifSnapshotsFolder);
                _gifSnapshotsFolder = null;
            }
        }

        private void OnRecordingFailedInternal(object? sender, RecordingFailedEventArgs e)
        {
            State = RecordingState.Idle;
            CleanUpRecorder();
            CleanUpSnapshotsFolderIfAny();
            RecordingFailed?.Invoke(this, e.Error);
        }

        private void OnStatusChanged(object? sender, RecordingStatusEventArgs e)
        {
            StatusMessage?.Invoke(this, $"Status: {e.Status}");
        }

        private void CleanUpRecorder()
        {
            if (_recorder != null)
            {
                _recorder.OnRecordingComplete -= OnRecordingComplete;
                _recorder.OnRecordingFailed -= OnRecordingFailedInternal;
                _recorder.OnStatusChanged -= OnStatusChanged;
                _recorder = null;
            }
        }
    }
}
