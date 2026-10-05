namespace RecorderByKyleSmith.Models
{
    /// <summary>
    /// The current state of the recorder. The main window uses this to
    /// decide which buttons are enabled and what the on-screen indicator
    /// shows. Keeping this as a simple enum (rather than scattered bool
    /// flags) makes it hard to represent an invalid combination, like
    /// "paused" and "not recording" at the same time.
    /// </summary>
    public enum RecordingState
    {
        Idle,
        Recording,
        Paused
    }
}
