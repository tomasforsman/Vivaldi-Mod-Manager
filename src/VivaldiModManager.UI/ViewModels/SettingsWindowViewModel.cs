using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VivaldiModManager.UI.Services;

namespace VivaldiModManager.UI.ViewModels;

/// <summary>
/// ViewModel for the settings window.
/// </summary>
public partial class SettingsWindowViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private AppTheme _selectedTheme;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private bool _enableNotifications;

    [ObservableProperty]
    private bool _enableAutoUpdates;

    public List<AppTheme> AvailableThemes { get; } = Enum.GetValues<AppTheme>().ToList();

    public SettingsWindowViewModel(IThemeService themeService, IDialogService dialogService)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        LoadSettings();
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                // Apply theme if changed
                if (SelectedTheme != _themeService.CurrentTheme)
                {
                    _themeService.ApplyTheme(SelectedTheme);
                }

                // TODO: Save other settings to configuration
                // This would involve saving to registry or config file

                await _dialogService.ShowInformationAsync("Settings saved successfully.", "Settings");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"Failed to save settings: {ex.Message}", "Error");
            }
        }, "Saving settings...");
    }

    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        if (!await _dialogService.ShowConfirmationAsync(
            "This will reset all settings to their default values. Continue?",
            "Reset Settings"))
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            try
            {
                LoadDefaultSettings();
                await _dialogService.ShowInformationAsync("Settings have been reset to defaults.", "Settings");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"Failed to reset settings: {ex.Message}", "Error");
            }
        }, "Resetting settings...");
    }

    private void LoadSettings()
    {
        // Load current settings
        SelectedTheme = _themeService.CurrentTheme;
        StartWithWindows = false; // TODO: Load from registry
        MinimizeToTray = true;
        EnableNotifications = true;
        EnableAutoUpdates = true;
    }

    private void LoadDefaultSettings()
    {
        SelectedTheme = AppTheme.System;
        StartWithWindows = false;
        MinimizeToTray = true;
        EnableNotifications = true;
        EnableAutoUpdates = true;
    }
}