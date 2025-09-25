namespace VivaldiModManager.UI.Services;

/// <summary>
/// Provides services for theme management.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme.
    /// </summary>
    AppTheme CurrentTheme { get; }

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Applies the specified theme.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    void ApplyTheme(AppTheme theme);

    /// <summary>
    /// Detects and applies the system theme.
    /// </summary>
    void DetectAndApplySystemTheme();
}

/// <summary>
/// Application theme options.
/// </summary>
public enum AppTheme
{
    Light,
    Dark,
    System
}

/// <summary>
/// Event arguments for theme changed events.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    public AppTheme OldTheme { get; }
    public AppTheme NewTheme { get; }

    public ThemeChangedEventArgs(AppTheme oldTheme, AppTheme newTheme)
    {
        OldTheme = oldTheme;
        NewTheme = newTheme;
    }
}