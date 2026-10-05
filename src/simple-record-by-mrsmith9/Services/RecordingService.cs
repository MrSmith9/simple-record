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
    /// monitor - see <see cref="RecordingSourceSelection"/>. No audio yet;
    /// that's added in a later part.
    /// </summary>
    public class RecordingService
    {
        private Recorder? _recorder;

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
        /// name="resolution"/> is Automatic). The file name is generated
        /// automatically from the current date and time, so recordings
        /// never overwrite each other.
        /// </summary>
        public void Start(string outputFolder, RecordingSourceSelection source, VideoResolutionPreset resolution)
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
                AudioOptions = new AudioOptions
                {
                    // Audio is switched on in a later part, once we add the
                    // system audio / microphone toggles to the UI.
                    IsAudioEnabled = false
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

                _recorder.Record(CurrentFilePath);
                State = RecordingState.Recording;
                StatusMessage?.Invoke(this, $"Recording started ({source.DisplayText}). Saving to: {CurrentFilePath}");
            }
            catch (Exception ex)
            {
                State = RecordingState.Idle;
                CleanUpRecorder();
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

        public void Stop()
        {
            if (State == RecordingState.Idle || _recorder == null)
            {
                return;
            }

            _recorder.Stop();
            // State goes back to Idle inside OnRecordingComplete, once
            // ScreenRecorderLib confirms the file has been finalized and
            // closed - stopping and finishing writing the file is not
            // instant, so we wait for that event rather than assuming.
        }

        private void OnRecordingComplete(object? sender, RecordingCompleteEventArgs e)
        {
            State = RecordingState.Idle;
            CleanUpRecorder();
            RecordingCompleted?.Invoke(this, e.FilePath);
        }

        private void OnRecordingFailedInternal(object? sender, RecordingFailedEventArgs e)
        {
            State = RecordingState.Idle;
            CleanUpRecorder();
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
