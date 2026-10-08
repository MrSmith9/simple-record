using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace SimpleRecord.Services
{
    /// <summary>
    /// Takes an instant screenshot of the primary monitor - independent
    /// of whether a recording is in progress, and independent of
    /// whatever source (whole screen / a window / a custom area) is
    /// currently selected for recording. Always captures the whole
    /// primary screen - same "primary monitor only" scope the rest of
    /// the app already has for window/area selection.
    ///
    /// Uses a plain Win32 call (GetSystemMetrics) to get the screen's
    /// real pixel size, rather than pulling in a reference to Windows
    /// Forms (System.Windows.Forms.Screen) just for that one value.
    /// </summary>
    public static class ScreenshotService
    {
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        /// <summary>
        /// Saves a .png of the current primary screen into
        /// <paramref name="outputFolder"/> and returns the full path to
        /// the saved file.
        /// </summary>
        public static string CaptureWholeScreen(string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);

            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);

            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, new Size(width, height));
            }

            string fileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string path = Path.Combine(outputFolder, fileName);
            bitmap.Save(path, ImageFormat.Png);
            return path;
        }
    }
}
