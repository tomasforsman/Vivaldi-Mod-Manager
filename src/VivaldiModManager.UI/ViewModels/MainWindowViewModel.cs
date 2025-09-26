using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using VivaldiModManager.Core.Constants;
using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Extensions;
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
    private readonly IHashService _hashService;
    private readonly IDialogService _dialogService;
    private readonly ISystemTrayService _systemTrayService;
    private readonly ILogger<MainWindowViewModel> _logger;

    private readonly string _dataDirectory;
    private readonly string _manifestPath;
    private readonly string _defaultModsRootPath;

    private ManifestData? _manifest;

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
        IHashService hashService,
        IDialogService dialogService,
        ISystemTrayService systemTrayService,
        ILogger<MainWindowViewModel> logger,
        bool autoInitialize = true,
        string? dataDirectory = null)
    {
        _vivaldiService = vivaldiService ?? throw new ArgumentNullException(nameof(vivaldiService));
        _injectionService = injectionService ?? throw new ArgumentNullException(nameof(injectionService));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _loaderService = loaderService ?? throw new ArgumentNullException(nameof(loaderService));
        _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _systemTrayService = systemTrayService ?? throw new ArgumentNullException(nameof(systemTrayService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _dataDirectory = string.IsNullOrWhiteSpace(dataDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VivaldiModManager")
            : Path.GetFullPath(dataDirectory);
        _manifestPath = Path.Combine(_dataDirectory, ManifestConstants.DefaultManifestFilename);
        _defaultModsRootPath = Path.Combine(_dataDirectory, "mods");

        if (autoInitialize)
        {
            InitializeAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshInstallationsAsync()
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                await EnsureManifestLoadedAsync();

                var detected = await _vivaldiService.DetectInstallationsAsync();
                var merged = MergeInstallationsWithManifest(detected);

                Installations.Clear();
                foreach (var installation in merged.OrderBy(i => i.Name))
                {
                    Installations.Add(new VivaldiInstallationViewModel(installation));
                }

                SetInitialSelection();

                StatusText = merged.Count == 0
                    ? "No Vivaldi installations found"
                    : $"Found {merged.Count} Vivaldi installation(s)";

                await SaveManifestAsync();
                _logger.LogInformation("Refreshed {Count} Vivaldi installations", merged.Count);
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
                await EnsureManifestLoadedAsync();

                RebuildModCollection();

                IsSafeModeEnabled = _manifest?.Settings.SafeModeActive ?? false;
                StatusText = Mods.Count == 0
                    ? "No mods configured yet"
                    : $"Loaded {Mods.Count} mod(s) from manifest";
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
                await EnsureManifestLoadedAsync();

                var installation = SelectedInstallation.Installation;
                var targets = await _vivaldiService.FindInjectionTargetsAsync(installation);
                var baseDirectory = GetInjectionBaseDirectory(targets);

                var loaderDirectory = Path.Combine(baseDirectory, "vivaldi-mods");
                var modsDirectory = Path.Combine(loaderDirectory, "mods");
                Directory.CreateDirectory(modsDirectory);

                var sourceModsDirectory = ResolveModsRootPath();
                var enabledMods = _manifest!.GetEnabledModsInOrder().ToList();

                foreach (var mod in enabledMods)
                {
                    var sourcePath = Path.Combine(sourceModsDirectory, mod.Filename);
                    if (!File.Exists(sourcePath))
                    {
                        _logger.LogWarning("Mod file missing from library: {ModFile}", sourcePath);
                        continue;
                    }

                    var destinationPath = Path.Combine(modsDirectory, mod.Filename);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    File.Copy(sourcePath, destinationPath, overwrite: true);
                }

                var loaderPath = Path.Combine(loaderDirectory, ManifestConstants.DefaultLoaderFilename);
                await _loaderService.GenerateLoaderAsync(_manifest, loaderPath);
                await _injectionService.InjectAsync(installation, loaderPath);

                installation.IsManaged = true;
                installation.IsActive = true;
                _manifest.Settings.SafeModeActive = false;

                await SaveManifestAsync();

                SelectedInstallation.Refresh();
                IsInjectionActive = true;
                IsSafeModeEnabled = false;
                StatusText = "Mods injected successfully";
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

                if (_manifest != null)
                {
                    _manifest.Settings.SafeModeActive = true;
                    if (SelectedInstallation?.Installation != null)
                    {
                        SelectedInstallation.Installation.LastInjectionStatus = "Safe Mode";
                        SelectedInstallation.Refresh();
                    }

                    await SaveManifestAsync();
                }

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
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await AddModFromFileInternalAsync(filePath);
            if (!string.IsNullOrEmpty(result))
            {
                StatusText = $"Added mod '{result}' successfully";
                _systemTrayService.ShowNotification("Vivaldi Mod Manager", $"Added mod: {result}", NotificationIcon.Info);
            }
        }, "Adding mod...");
    }

    [RelayCommand]
    private async Task DropFilesAsync(string[] filePaths)
    {
        if (filePaths == null || filePaths.Length == 0)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var added = 0;
            foreach (var filePath in filePaths)
            {
                var result = await AddModFromFileInternalAsync(filePath);
                if (!string.IsNullOrEmpty(result))
                {
                    added++;
                }
            }

            if (added > 0)
            {
                StatusText = $"Added {added} mod(s)";
                _systemTrayService.ShowNotification("Vivaldi Mod Manager", $"Added {added} mod(s)", NotificationIcon.Info);
            }
        }, "Adding mods...");
    }

    [RelayCommand]
    private async Task ShowSettingsAsync()
    {
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
        try
        {
            await EnsureManifestLoadedAsync();
            await RefreshInstallationsAsync();
            await LoadModsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize main window view model");
        }
    }

    private async Task EnsureManifestLoadedAsync()
    {
        if (_manifest != null)
        {
            return;
        }

        Directory.CreateDirectory(_dataDirectory);

        try
        {
            if (_manifestService.ManifestExists(_manifestPath))
            {
                _manifest = await _manifestService.LoadManifestAsync(_manifestPath);
            }
            else
            {
                _manifest = _manifestService.CreateDefaultManifest();
                _manifest.Settings.ModsRootPath = _defaultModsRootPath;
                await _manifestService.SaveManifestAsync(_manifest, _manifestPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manifest could not be loaded - creating a new one");
            await _dialogService.ShowErrorAsync($"Manifest could not be loaded: {ex.Message}. A new manifest will be created.");
            _manifest = _manifestService.CreateDefaultManifest();
            _manifest.Settings.ModsRootPath = _defaultModsRootPath;
            await _manifestService.SaveManifestAsync(_manifest, _manifestPath);
        }

        if (_manifest == null)
        {
            throw new InvalidOperationException("Manifest initialization failed.");
        }

        if (string.IsNullOrWhiteSpace(_manifest.Settings.ModsRootPath))
        {
            _manifest.Settings.ModsRootPath = _defaultModsRootPath;
            await _manifestService.SaveManifestAsync(_manifest, _manifestPath);
        }

        Directory.CreateDirectory(ResolveModsRootPath());
        IsSafeModeEnabled = _manifest.Settings.SafeModeActive;
    }

    private async Task SaveManifestAsync()
    {
        if (_manifest == null)
        {
            return;
        }

        await _manifestService.SaveManifestAsync(_manifest, _manifestPath);
    }

    private List<VivaldiInstallation> MergeInstallationsWithManifest(IReadOnlyList<VivaldiInstallation> detected)
    {
        if (_manifest == null)
        {
            return detected.ToList();
        }

        var existingByPath = _manifest.Installations
            .ToDictionary(i => NormalizePath(i.InstallationPath), i => i, StringComparer.OrdinalIgnoreCase);

        var updated = new List<VivaldiInstallation>();

        foreach (var installation in detected)
        {
            var key = NormalizePath(installation.InstallationPath);
            if (existingByPath.TryGetValue(key, out var existing))
            {
                installation.Id = existing.Id;
                installation.IsManaged = existing.IsManaged;
                installation.IsActive = existing.IsActive;
                installation.DetectedAt = existing.DetectedAt;
                installation.LastInjectionAt = existing.LastInjectionAt;
                installation.LastInjectionStatus = existing.LastInjectionStatus;
                installation.InjectionFingerprint = existing.InjectionFingerprint;
                installation.Metadata = existing.Metadata;
            }
            else if (!updated.Any(i => i.IsManaged))
            {
                installation.IsManaged = true;
                installation.IsActive = true;
            }

            updated.Add(installation);
        }

        foreach (var kvp in existingByPath)
        {
            var stillPresent = updated.Any(i => string.Equals(NormalizePath(i.InstallationPath), kvp.Key, StringComparison.OrdinalIgnoreCase));
            if (!stillPresent)
            {
                var missing = kvp.Value;
                missing.IsManaged = false;
                missing.IsActive = false;
                missing.LastInjectionStatus = "Not detected";
                updated.Add(missing);
            }
        }

        _manifest.Installations = updated;
        return updated;
    }

    private void SetInitialSelection()
    {
        if (_manifest == null)
        {
            SelectedInstallation = null;
            return;
        }

        var active = _manifest.Installations.FirstOrDefault(i => i.IsActive) ??
                    _manifest.Installations.FirstOrDefault(i => i.IsManaged) ??
                    _manifest.Installations.FirstOrDefault();

        if (active == null)
        {
            SelectedInstallation = null;
            return;
        }

        foreach (var installation in _manifest.Installations)
        {
            installation.IsActive = installation.Id == active.Id;
        }

        SelectedInstallation = Installations.FirstOrDefault(i => i.Installation.Id == active.Id);
        IsInjectionActive = string.Equals(active.LastInjectionStatus, "Success", StringComparison.OrdinalIgnoreCase);
    }

    private void RebuildModCollection()
    {
        Mods.Clear();

        if (_manifest == null)
        {
            return;
        }

        foreach (var mod in _manifest.Mods.OrderBy(m => m.Order))
        {
            Mods.Add(new ModItemViewModel(mod));
        }
    }

    private string ResolveModsRootPath()
    {
        return _manifest?.Settings.ModsRootPath ?? _defaultModsRootPath;
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string GetInjectionBaseDirectory(IReadOnlyDictionary<string, string> targets)
    {
        if (targets.Count == 0)
        {
            throw new InjectionException("No injection targets available");
        }

        var firstPath = targets.First().Value;
        var directory = Path.GetDirectoryName(firstPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InjectionException("Unable to resolve injection directory");
        }

        return directory;
    }

    private static string GenerateUniqueFileName(string directory, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var candidate = fileName;
        var counter = 1;

        while (File.Exists(Path.Combine(directory, candidate)))
        {
            candidate = $"{baseName}_{counter}{extension}";
            counter++;
        }

        return candidate;
    }

    private async Task<string?> AddModFromFileInternalAsync(string filePath)
    {
        try
        {
            await EnsureManifestLoadedAsync();

            if (!File.Exists(filePath))
            {
                await _dialogService.ShowErrorAsync($"File not found: {filePath}");
                return null;
            }

            var sourceInfo = new FileInfo(filePath);
            if (!string.Equals(sourceInfo.Extension, ManifestConstants.ModFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                await _dialogService.ShowErrorAsync("Only JavaScript (.js) files can be imported as mods.");
                return null;
            }

            Directory.CreateDirectory(_dataDirectory);

            var modsRoot = ResolveModsRootPath();
            Directory.CreateDirectory(modsRoot);

            var targetFileName = GenerateUniqueFileName(modsRoot, sourceInfo.Name);
            var destinationPath = Path.Combine(modsRoot, targetFileName);

            File.Copy(filePath, destinationPath, overwrite: false);

            var checksum = await _hashService.ComputeFileHashAsync(destinationPath);
            var nextOrder = _manifest!.Mods.Any() ? _manifest.Mods.Max(m => m.Order) + 1 : 1;

            var newMod = new ModInfo
            {
                Id = Guid.NewGuid().ToString("N"),
                Filename = targetFileName,
                Enabled = true,
                Order = nextOrder,
                Version = string.Empty,
                FileSize = new FileInfo(destinationPath).Length,
                Notes = $"Imported on {DateTimeOffset.Now:yyyy-MM-dd}",
                LastModified = DateTimeOffset.UtcNow,
                Checksum = checksum,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _manifest.Mods.Add(newMod);
            await SaveManifestAsync();

            RebuildModCollection();

            return newMod.Filename;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add mod from {FilePath}", filePath);
            await _dialogService.ShowErrorAsync($"Failed to add mod: {ex.Message}");
            return null;
        }
    }

    partial void OnSelectedInstallationChanged(VivaldiInstallationViewModel? value)
    {
        IsInjectionActive = value?.Installation.LastInjectionStatus != null &&
            string.Equals(value.Installation.LastInjectionStatus, "Success", StringComparison.OrdinalIgnoreCase);
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
    public string StatusText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Installation.LastInjectionStatus))
            {
                return Installation.LastInjectionStatus;
            }

            return Installation.IsManaged ? "✓ Managed" : "○ Available";
        }
    }

    public VivaldiInstallationViewModel(VivaldiInstallation installation)
    {
        Installation = installation ?? throw new ArgumentNullException(nameof(installation));
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Version));
        OnPropertyChanged(nameof(InstallationType));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(IsManaged));
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

    public string Name
    {
        get => Mod.Filename;
        set
        {
            if (!string.Equals(Mod.Filename, value, StringComparison.Ordinal))
            {
                Mod.Filename = value;
                OnPropertyChanged();
            }
        }
    }

    public string Version
    {
        get => Mod.Version;
        set
        {
            if (!string.Equals(Mod.Version, value, StringComparison.Ordinal))
            {
                Mod.Version = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsEnabled
    {
        get => Mod.Enabled;
        set
        {
            if (Mod.Enabled != value)
            {
                Mod.Enabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public int Order
    {
        get => Mod.Order;
        set
        {
            if (Mod.Order != value)
            {
                Mod.Order = value;
                OnPropertyChanged();
            }
        }
    }

    public string Notes
    {
        get => Mod.Notes;
        set
        {
            if (!string.Equals(Mod.Notes, value, StringComparison.Ordinal))
            {
                Mod.Notes = value;
                OnPropertyChanged();
            }
        }
    }

    public string SizeText => FormatFileSize(Mod.FileSize);

    public string StatusText => Mod.Enabled ? "Enabled" : "Disabled";

    public ModItemViewModel(ModInfo mod)
    {
        Mod = mod ?? throw new ArgumentNullException(nameof(mod));
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Version));
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(Order));
        OnPropertyChanged(nameof(Notes));
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(StatusText));
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F1} KB";
        }

        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
