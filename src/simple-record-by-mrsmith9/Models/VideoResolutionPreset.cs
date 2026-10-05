namespace SimpleRecord.Models
{
    /// <summary>
    /// The size (in pixels) a recording is saved at. "Automatic" means the
    /// video is saved at exactly whatever size is being recorded (the
    /// screen, the window, or the dragged area) - every other option forces
    /// the output to a specific, fixed size.
    /// </summary>
    public enum VideoResolutionPreset
    {
        Automatic,
        R720p,
        R1080p,
        R1440p,
        R2160p,
        R4320p
    }

    public static class VideoResolutionPresetExtensions
    {
        /// <summary>Label shown in the Settings window, including the exact pixel size.</summary>
        public static string DisplayText(this VideoResolutionPreset preset) => preset switch
        {
            VideoResolutionPreset.R720p => "720p HD (1280 x 720)",
            VideoResolutionPreset.R1080p => "1080p Full HD (1920 x 1080)",
            VideoResolutionPreset.R1440p => "2K / 1440p (2560 x 1440)",
            VideoResolutionPreset.R2160p => "4K UHD (3840 x 2160)",
            VideoResolutionPreset.R4320p => "8K UHD (7680 x 4320)",
            _ => "Automatic (matches what you're recording)"
        };

        /// <summary>
        /// The exact pixel size to record at, or null for Automatic (meaning
        /// "don't force a size - use whatever is actually being recorded").
        /// </summary>
        public static (int Width, int Height)? PixelSize(this VideoResolutionPreset preset) => preset switch
        {
            VideoResolutionPreset.R720p => (1280, 720),
            VideoResolutionPreset.R1080p => (1920, 1080),
            VideoResolutionPreset.R1440p => (2560, 1440),
            VideoResolutionPreset.R2160p => (3840, 2160),
            VideoResolutionPreset.R4320p => (7680, 4320),
            _ => null
        };
    }
}
