using System.Windows;
using SimpleRecord.Models;
using SimpleRecord.Services;

namespace SimpleRecord
{
    public partial class App : Application
    {
        /// <summary>
        /// Runs once, right when the app is launched - before any window
        /// exists. Loads the user's saved settings just far enough to read
        /// which theme (Dark or Light) they last chose, applies it (see
        /// <see cref="ThemeManager"/>), and only then creates and shows
        /// MainWindow, so it opens already looking correct instead of
        /// flashing the default theme first and then switching.
        ///
        /// 2026-10-08: applying the saved theme is wrapped in try/catch -
        /// this must never be able to stop the app from opening. App.xaml
        /// already merges in a safe, always-valid default theme (Dark) on
        /// its own, so if anything goes wrong loading the saved theme here,
        /// the app still opens normally using that default instead of
        /// failing silently before any window appears.
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppSettings settings = SettingsService.Load();
            try
            {
                ThemeManager.Apply(settings.Theme);
            }
            catch
            {
                // Fall back to App.xaml's built-in default theme (Dark).
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
