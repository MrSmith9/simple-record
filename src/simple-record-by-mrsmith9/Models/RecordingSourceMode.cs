namespace SimpleRecord.Models
{
    /// <summary>What part of the screen a recording should capture.</summary>
    public enum RecordingSourceMode
    {
        /// <summary>The whole primary monitor.</summary>
        FullScreen,

        /// <summary>One specific open window.</summary>
        Window,

        /// <summary>A custom rectangular area the user dragged out on the primary monitor.</summary>
        Region
    }
}
