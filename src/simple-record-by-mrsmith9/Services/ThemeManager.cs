using System;
using System.Windows;
using SimpleRecord.Models;

namespace SimpleRecord.Services
{
    /// <summary>
    /// Switches the app's color palette between the Dark and Light theme
    /// resource dictionaries (Resources/Theme.Dark.xaml and
    /// Resources/Theme.Light.xaml). Every window's colors come from a
    /// shared set of resource keys (BackgroundBrush, SurfaceBrush,
    /// AccentBrush, and so on - see Resources/Styles.xaml) rather than
    /// fixed colors, so swapping which of these two files is merged into
    /// the app's resources changes every window's colors at once.
    ///
    /// Important: this only affects windows created AFTER <see cref="Apply"/>
    /// runs. WPF reads a color resource once, when a window is built, and
    /// does not watch for it to change later - so a window that's already
    /// open keeps its old colors even after this runs. This is called in
    /// two places: once at startup (App.xaml.cs's OnStartup, before
    /// MainWindow is created), and once from MainWindow.xaml.cs's
    /// SettingsButton_Click right after the theme is changed and saved -
    /// that second call is immediately followed by closing the current
    /// MainWindow and opening a brand new one (see
    /// MainWindow.ReopenWithNewTheme), which is how the new theme ends up
    /// showing right away instead of needing a full app restart.
    ///
    /// 2026-10-08 (third pass): this method now ALSO reloads Styles.xaml,
    /// not just the theme colors. Reason: every button, radio button,
    /// field box, heading, and label in the app gets its look from a
    /// <c>Style</c> defined once in Styles.xaml (e.g. "SecondaryButton",
    /// "FieldLabel"), and each Style's Setters point at color keys with
    /// StaticResource (e.g. <c>Foreground="{StaticResource
    /// TextPrimaryBrush}"</c>). WPF only resolves a StaticResource the
    /// FIRST time a given Style is actually used anywhere in the running
    /// app, and then reuses that same already-resolved Style object for
    /// every control that asks for it afterwards - including in a brand
    /// new window. So if, say, the Settings window had already been
    /// opened once while Dark was active, its styles (field boxes,
    /// labels, radio buttons) got "locked in" to Dark's colors right
    /// then - and no amount of swapping the theme dictionary or
    /// reopening MainWindow would undo that, because Styles.xaml itself
    /// was never reloaded, only Theme.Dark.xaml/Theme.Light.xaml was.
    /// That's what caused Kyle's "can't see text in Settings when using
    /// Light mode" report: the window's own background correctly picked
    /// up the new Light color (it's set directly on the window, not
    /// through a cached Style), but labels and boxes drawn through a
    /// Style that had already been resolved under Dark kept Dark's
    /// colors - near-white text baked in, now invisible against the new
    /// light background. Removing and re-adding Styles.xaml below forces
    /// every Style in it to be parsed again from scratch, which makes it
    /// resolve its StaticResource colors fresh against whichever theme
    /// dictionary is active at that moment. Like the theme dictionary
    /// itself, this only affects windows built AFTER this method runs -
    /// an already-open window still needs to be closed and reopened (see
    /// MainWindow.ReopenWithNewTheme) to pick up the change.
    /// </summary>
    public static class ThemeManager
    {
        private const string DarkThemeUri = "Resources/Theme.Dark.xaml";
        private const string LightThemeUri = "Resources/Theme.Light.xaml";
        private const string StylesUri = "Resources/Styles.xaml";

        public static void Apply(AppTheme theme)
        {
            Application? app = Application.Current;
            if (app == null)
            {
                return;
            }

            string themeFile = theme == AppTheme.Light ? LightThemeUri : DarkThemeUri;

            // 2026-10-08 (fourth pass): build the two replacement
            // dictionaries FIRST, before touching anything already merged
            // in - and if that fails for any reason, stop here and leave
            // the current theme/styles completely untouched. This matters
            // because the app has to keep working no matter what: if the
            // OLD dictionaries were removed first and then building the
            // NEW ones failed, the app would be left with no theme colors
            // and no styles at all, and every window from that point on
            // (including the very next one WPF tries to build) would fail
            // to find basic resources like "BackgroundBrush" - which can
            // happen early enough to stop the app from opening at all,
            // with nothing useful shown on screen. Kyle hit exactly this
            // after the third pass added the Styles.xaml reload below
            // ("i cant start it up no error") - wrapping construction in
            // try/catch and only removing the old entries once the new
            // ones are confirmed to exist fixes that risk regardless of
            // the exact underlying cause, which can't be confirmed without
            // being able to run the app directly.
            ResourceDictionary themeDictionary;
            ResourceDictionary stylesDictionary;
            try
            {
                themeDictionary = new ResourceDictionary { Source = new Uri(themeFile, UriKind.Relative) };
                stylesDictionary = new ResourceDictionary { Source = new Uri(StylesUri, UriKind.Relative) };
            }
            catch
            {
                return;
            }

            var mergedDictionaries = app.Resources.MergedDictionaries;

            // Remove whichever theme dictionary (Dark or Light) is
            // currently merged in, AND the shared Styles.xaml dictionary -
            // identified by file name rather than position in the list, so
            // this works correctly no matter which theme App.xaml started
            // with. Both get rebuilt fresh below - see the class comment
            // above for why Styles.xaml has to be included here too, not
            // just the theme colors.
            for (int i = mergedDictionaries.Count - 1; i >= 0; i--)
            {
                string? source = mergedDictionaries[i].Source?.OriginalString;
                if (source != null && (source.EndsWith(DarkThemeUri, StringComparison.OrdinalIgnoreCase)
                                        || source.EndsWith(LightThemeUri, StringComparison.OrdinalIgnoreCase)
                                        || source.EndsWith(StylesUri, StringComparison.OrdinalIgnoreCase)))
                {
                    mergedDictionaries.RemoveAt(i);
                }
            }

            // Theme dictionary first (same position App.xaml originally
            // listed it in), Styles.xaml second - Styles.xaml refers to
            // color keys (AccentBrush, BorderBrush, etc.) that only the
            // theme dictionary defines, so it has to be merged in after it.
            mergedDictionaries.Insert(0, themeDictionary);
            mergedDictionaries.Add(stylesDictionary);
        }
    }
}
