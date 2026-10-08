namespace SimpleRecord.Models
{
    /// <summary>
    /// Which color palette every window in the app is drawn in. Dark is the
    /// app's original look and the default; Light is a bright, high-contrast
    /// alternative for anyone who finds a dark app harder to read.
    ///
    /// Both options keep the same layout, icons, and text everywhere -
    /// only the colors change. See <see cref="Services.ThemeManager"/> for
    /// how the chosen theme is applied, and Resources/Theme.Dark.xaml and
    /// Resources/Theme.Light.xaml for the two color sets themselves.
    /// </summary>
    public enum AppTheme
    {
        Dark,
        Light
    }
}
