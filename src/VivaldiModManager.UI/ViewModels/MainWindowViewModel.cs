using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;
using VivaldiModManager.Core.Models;
using VivaldiModManager.Core.Services;
using VivaldiModManager.UI.Services;

namespace VivaldiModManager.UI.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IVivaldiService _vivaldiService;
    private readonly IInjectionService _injectionService;
    private readonly IManifestService _manifestService;
    private readonly ILoaderService _loaderService;
    private readonly IDialogService _dialogService;
    private readonly ISystemTrayService _systemTrayService;
    private readonly ILogger<MainWindowViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<VivaldiInstallationViewModel> _installations = new();

    [ObservableProperty]
    private ObservableCollection<ModItemViewModel> _mods = new();

    [ObservableProperty]
    private VivaldiInstallationViewModel? _selectedInstallation;

    [ObservableProperty]
    private ModItemViewModel? _selectedMod;

    [ObservableProperty]
    private bool _isSafeModeEnabled;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isInjectionActive;

    public MainWindowViewModel(
        IVivaldiService vivaldiService,
        IInjectionService injectionService,
        IManifestService manifestService,
        ILoaderService loaderService,
        IDialogService dialogService,
        ISystemTrayService systemTrayService,
        ILogger<MainWindowViewModel> logger)
    {
        _vivaldiService = vivaldiService ?? throw new ArgumentNullException(nameof(vivaldiService));
        _injectionService = injectionService ?? throw new ArgumentNullException(nameof(injectionService));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _loaderService = loaderService ?? throw new ArgumentNullException(nameof(loaderService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _systemTrayService = systemTrayService ?? throw new ArgumentNullException(nameof(systemTrayService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeAsync();
    }

    [RelayCommand]
    private async Task RefreshInstallationsAsync()
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                // For demo purposes, create sample installations
                // In real implementation, this would call _vivaldiService.DetectInstallationsAsync()
                Installations.Clear();
                
                var sampleInstallations = new[]
                {
                    new VivaldiInstallation
                    {
                        Id = "vivaldi-stable",
                        Name = "Vivaldi 6.5.3206.63",
                        Version = "6.5.3206.63",
                        InstallationType = VivaldiInstallationType.Standard,
                        InstallationPath = @"C:\Program Files\Vivaldi\Application",
                        IsManaged = true,
                        DetectedAt = DateTimeOffset.Now.AddDays(-10)
                    },
                    new VivaldiInstallation
                    {
                        Id = "vivaldi-snapshot",
                        Name = "Vivaldi Snapshot 6.6.3264.3",
                        Version = "6.6.3264.3",
                        InstallationType = VivaldiInstallationType.Snapshot,
                        InstallationPath = @"C:\Program Files\Vivaldi-Snapshot\Application",
                        IsManaged = false,
                        DetectedAt = DateTimeOffset.Now.AddDays(-2)
                    }
                };

                foreach (var installation in sampleInstallations)
                {
                    Installations.Add(new VivaldiInstallationViewModel(installation));
                }

                if (Installations.Count > 0 && SelectedInstallation == null)
                {
                    SelectedInstallation = Installations.FirstOrDefault(i => i.IsManaged) ?? Installations.First();
                }

                StatusText = $"Found {Installations.Count} Vivaldi installation(s)";
                _logger.LogInformation("Refreshed {Count} Vivaldi installations", Installations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh installations");
                await _dialogService.ShowErrorAsync($"Failed to refresh installations: {ex.Message}");
            }
        }, "Detecting Vivaldi installations...");
    }

    [RelayCommand]
    private async Task LoadModsAsync()
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                // For demo purposes, create some sample mods
                Mods.Clear();
                
                var sampleMods = new[]
                {
                    new ModInfo
                    {
                        Id = "enhanced-tabs",
                        Filename = "enhanced-tabs.js",
                        Enabled = true,
                        Order = 1,
                        Version = "2.1.0",
                        FileSize = 15520,
                        Notes = "Adds advanced tab grouping functionality",
                        LastModified = DateTimeOffset.Now.AddDays(-5)
                    },
                    new ModInfo
                    {
                        Id = "custom-css",
                        Filename = "custom-css-loader.js",
                        Enabled = false,
                        Order = 2,
                        Version = "1.0.3",
                        FileSize = 8192,
                        Notes = "Loads custom CSS modifications",
                        LastModified = DateTimeOffset.Now.AddDays(-12)
                    },
                    new ModInfo
                    {
                        Id = "bookmark-enhancer",
                        Filename = "bookmark-enhancer.js",
                        Enabled = true,
                        Order = 3,
                        Version = "3.2.1",
                        FileSize = 23040,
                        Notes = "Enhanced bookmark management with folders and tags",
                        LastModified = DateTimeOffset.Now.AddDays(-3)
                    }
                };

                foreach (var mod in sampleMods)
                {
                    Mods.Add(new ModItemViewModel(mod));
                }

                StatusText = $"Loaded {Mods.Count} mods successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load mods");
                await _dialogService.ShowErrorAsync($"Failed to load mods: {ex.Message}");
            }
        }, "Loading mods...");
    }

    [RelayCommand]
    private async Task InjectModsAsync()
    {
        if (SelectedInstallation?.Installation == null)
        {
            await _dialogService.ShowWarningAsync("Please select a Vivaldi installation first.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            try
            {
                // TODO: Generate loader and inject
                StatusText = "Mods injected successfully";
                IsInjectionActive = true;
                _systemTrayService.ShowNotification("Vivaldi Mod Manager", "Mods injected successfully", NotificationIcon.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to inject mods");
                await _dialogService.ShowErrorAsync($"Failed to inject mods: {ex.Message}");
            }
        }, "Injecting mods...");
    }

    [RelayCommand]
    private async Task EnableSafeModeAsync()
    {
        if (!await _dialogService.ShowConfirmationAsync(
            "Safe Mode will disable all mods and restore Vivaldi to its original state. Continue?",
            "Enable Safe Mode"))
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            try
            {
                if (SelectedInstallation?.Installation != null)
                {
                    await _injectionService.RemoveInjectionAsync(SelectedInstallation.Installation);
                }

                IsSafeModeEnabled = true;
                IsInjectionActive = false;
                StatusText = "Safe Mode enabled - all mods disabled";
                _systemTrayService.ShowNotification("Vivaldi Mod Manager", "Safe Mode enabled", NotificationIcon.Warning);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable Safe Mode");
                await _dialogService.ShowErrorAsync($"Failed to enable Safe Mode: {ex.Message}");
            }
        }, "Enabling Safe Mode...");
    }

    [RelayCommand]
    private async Task AddModAsync()
    {
        var filePath = await _dialogService.ShowOpenFileDialogAsync(
            "JavaScript Files (*.js)|*.js|All Files (*.*)|*.*",
            "Select Mod File");

        if (string.IsNullOrEmpty(filePath))
            return;

        await AddModFromFileAsync(filePath);
    }

    [RelayCommand]
    private async Task DropFilesAsync(string[] filePaths)
    {
        if (filePaths == null || filePaths.Length == 0)
            return;

        foreach (var filePath in filePaths)
        {
            await AddModFromFileAsync(filePath);
        }
    }

    private async Task AddModFromFileAsync(string filePath)
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                var fileName = System.IO.Path.GetFileName(filePath);
                var fileInfo = new System.IO.FileInfo(filePath);
                
                var newMod = new ModInfo
                {
                    Id = System.IO.Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant(),
                    Filename = fileName,
                    Enabled = true,
                    Order = Mods.Count + 1,
                    Version = "1.0.0",
                    FileSize = fileInfo.Length,
                    Notes = $"Added from {filePath}",
                    LastModified = fileInfo.LastWriteTime
                };

                Mods.Add(new ModItemViewModel(newMod));
                StatusText = $"Added mod '{fileName}' successfully";
                
                _systemTrayService.ShowNotification("Vivaldi Mod Manager", $"Added mod: {fileName}", NotificationIcon.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add mod from {FilePath}", filePath);
                await _dialogService.ShowErrorAsync($"Failed to add mod: {ex.Message}");
            }
        }, "Adding mod...");
    }

    [RelayCommand]
    private async Task ShowSettingsAsync()
    {
        // TODO: Show settings window
        await _dialogService.ShowInformationAsync("Settings window would open here.");
    }

    [RelayCommand]
    private void ShowAbout()
    {
        var aboutWindow = new Views.AboutWindow
        {
            Owner = Application.Current.MainWindow
        };
        aboutWindow.ShowDialog();
    }

    [RelayCommand]
    private async Task ImportModsAsync()
    {
        var filePath = await _dialogService.ShowOpenFileDialogAsync(
            "Mod Configuration (*.json)|*.json|All Files (*.*)|*.*",
            "Import Mod Configuration");

        if (!string.IsNullOrEmpty(filePath))
        {
            await _dialogService.ShowInformationAsync("Import functionality coming soon!");
        }
    }

    [RelayCommand]
    private async Task ExportModsAsync()
    {
        await _dialogService.ShowInformationAsync("Export functionality coming soon!");
    }

    [RelayCommand]
    private async Task EditNotesAsync()
    {
        if (SelectedMod == null)
        {
            await _dialogService.ShowWarningAsync("Please select a mod first.");
            return;
        }

        await _dialogService.ShowInformationAsync("Edit notes functionality coming soon!");
    }

    [RelayCommand]
    private async Task ViewSourceAsync()
    {
        if (SelectedMod == null)
        {
            await _dialogService.ShowWarningAsync("Please select a mod first.");
            return;
        }

        await _dialogService.ShowInformationAsync("View source functionality coming soon!");
    }

    private async void InitializeAsync()
    {
        await RefreshInstallationsAsync();
        await LoadModsAsync();
    }
}

/// <summary>
/// ViewModel wrapper for VivaldiInstallation.
/// </summary>
public partial class VivaldiInstallationViewModel : ViewModelBase
{
    public VivaldiInstallation Installation { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string Name => Installation.Name;
    public string Version => Installation.Version;
    public string InstallationType => Installation.InstallationType.ToString();
    public bool IsManaged => Installation.IsManaged;
    public string StatusText => IsManaged ? "✓ Active" : "○ Available";

    public VivaldiInstallationViewModel(VivaldiInstallation installation)
    {
        Installation = installation ?? throw new ArgumentNullException(nameof(installation));
    }
}

/// <summary>
/// ViewModel wrapper for ModInfo.
/// </summary>
public partial class ModItemViewModel : ViewModelBase
{
    public ModInfo Mod { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string Name => Mod.Filename;
    public string Version => Mod.Version;
    public bool IsEnabled => Mod.Enabled;
    public int Order => Mod.Order;
    public string Notes => Mod.Notes;
    public string SizeText => FormatFileSize(Mod.FileSize);

    public ModItemViewModel(ModInfo mod)
    {
        Mod = mod ?? throw new ArgumentNullException(nameof(mod));
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}