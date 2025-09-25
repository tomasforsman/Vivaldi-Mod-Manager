using Microsoft.Win32;
using System.Windows;

namespace VivaldiModManager.UI.Services;

/// <summary>
/// Implementation of IThemeService for WPF theme management.
/// </summary>
public class ThemeService : IThemeService
{
    private AppTheme _currentTheme = AppTheme.System;

    /// <inheritdoc />
    public AppTheme CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <inheritdoc />
    public void ApplyTheme(AppTheme theme)
    {
        var oldTheme = _currentTheme;
        _currentTheme = theme;

        // Apply the actual theme resources based on the theme
        var actualTheme = theme == AppTheme.System ? GetSystemTheme() : theme;
        ApplyThemeResources(actualTheme);

        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));
    }

    /// <inheritdoc />
    public void DetectAndApplySystemTheme()
    {
        var systemTheme = GetSystemTheme();
        ApplyThemeResources(systemTheme);
    }

    private AppTheme GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            
            if (value is int intValue)
            {
                return intValue == 0 ? AppTheme.Dark : AppTheme.Light;
            }
        }
        catch
        {
            // Fall back to light theme if we can't detect
        }

        return AppTheme.Light;
    }

    private void ApplyThemeResources(AppTheme theme)
    {
        var app = Application.Current;
        if (app?.Resources == null)
            return;

        // Clear existing theme resources
        var resourcesToRemove = app.Resources.MergedDictionaries
            .Where(rd => rd.Source?.OriginalString?.Contains("Theme") == true)
            .ToList();

        foreach (var resource in resourcesToRemove)
        {
            app.Resources.MergedDictionaries.Remove(resource);
        }

        // Add new theme resources
        var themeUri = theme switch
        {
            AppTheme.Dark => new Uri("Themes/DarkTheme.xaml", UriKind.Relative),
            _ => new Uri("Themes/LightTheme.xaml", UriKind.Relative)
        };

        try
        {
            var themeDict = new ResourceDictionary { Source = themeUri };
            app.Resources.MergedDictionaries.Add(themeDict);
        }
        catch
        {
            // Fall back to default theme if theme files don't exist
        }
    }
}