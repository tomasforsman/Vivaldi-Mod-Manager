using System.Runtime.Versioning;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VivaldiModManager.Core.Models;
using VivaldiModManager.Core.Services;
using Xunit;

namespace VivaldiModManager.Core.Tests.Services;

/// <summary>
/// Integration tests for the <see cref="InjectionService"/> class with real dependencies.
/// Tests the interaction between InjectionService and HashService.
/// </summary>
[SupportedOSPlatform("windows")]
public class InjectionServiceIntegrationTests : IDisposable
{
    private readonly Mock<ILogger<InjectionService>> _injectionLoggerMock;
    private readonly Mock<ILogger<HashService>> _hashLoggerMock;
    private readonly Mock<IVivaldiService> _vivaldiServiceMock;
    private readonly Mock<ILoaderService> _loaderServiceMock;
    private readonly InjectionService _injectionService;
    private readonly HashService _hashService;
    private readonly string _tempDirectory;

    public InjectionServiceIntegrationTests()
    {
        _injectionLoggerMock = new Mock<ILogger<InjectionService>>();
        _hashLoggerMock = new Mock<ILogger<HashService>>();
        _vivaldiServiceMock = new Mock<IVivaldiService>();
        _loaderServiceMock = new Mock<ILoaderService>();

        // Use real HashService for integration testing
        _hashService = new HashService(_hashLoggerMock.Object);
        
        _injectionService = new InjectionService(
            _injectionLoggerMock.Object,
            _vivaldiServiceMock.Object,
            _loaderServiceMock.Object,
            _hashService);

        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task InjectAsync_WithRealHashService_GeneratesConsistentFingerprints()
    {
        // Arrange
        var installation = CreateTestInstallation();
        var loaderPath = Path.Combine(_tempDirectory, "vivaldi-mods", "loader.js");
        var windowHtmlPath = Path.Combine(_tempDirectory, "window.html");
        var browserHtmlPath = Path.Combine(_tempDirectory, "browser.html");
        
        // Create test files
        Directory.CreateDirectory(Path.GetDirectoryName(loaderPath)!);
        await File.WriteAllTextAsync(loaderPath, "// Test loader content");
        await File.WriteAllTextAsync(windowHtmlPath, "<html><head></head><body></body></html>");
        await File.WriteAllTextAsync(browserHtmlPath, "<html><head></head><body></body></html>");

        var targets = new Dictionary<string, string>
        {
            ["window.html"] = windowHtmlPath,
            ["browser.html"] = browserHtmlPath
        }.AsReadOnly();

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        // Act - Perform injection
        await _injectionService.InjectAsync(installation, loaderPath);

        // Assert - Verify real hash service generated consistent fingerprint
        installation.InjectionFingerprint.Should().NotBeNullOrEmpty();
        installation.InjectionFingerprint.Should().HaveLength(16);
        
        // Verify that both files contain the same fingerprint
        var windowContent = await File.ReadAllTextAsync(windowHtmlPath);
        var browserContent = await File.ReadAllTextAsync(browserHtmlPath);

        windowContent.Should().Contain($"Fingerprint: {installation.InjectionFingerprint}");
        browserContent.Should().Contain($"Fingerprint: {installation.InjectionFingerprint}");

        // Verify injection content
        windowContent.Should().Contain("Vivaldi Mod Manager - Injection Stub");
        browserContent.Should().Contain("Vivaldi Mod Manager - Injection Stub");
        windowContent.Should().Contain("vivaldi-mods/loader.js");
        browserContent.Should().Contain("vivaldi-mods/loader.js");
    }

    [Fact]
    public async Task ValidateInjectionAsync_WithRealHashService_ValidatesFingerprints()
    {
        // Arrange
        var installation = CreateTestInstallation();
        var loaderPath = Path.Combine(_tempDirectory, "vivaldi-mods", "loader.js");
        var windowHtmlPath = Path.Combine(_tempDirectory, "window.html");
        
        Directory.CreateDirectory(Path.GetDirectoryName(loaderPath)!);
        await File.WriteAllTextAsync(loaderPath, "// Test loader content");
        await File.WriteAllTextAsync(windowHtmlPath, "<html><head></head><body></body></html>");

        var targets = new Dictionary<string, string>
        {
            ["window.html"] = windowHtmlPath
        }.AsReadOnly();

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        // First inject
        await _injectionService.InjectAsync(installation, loaderPath);
        var originalFingerprint = installation.InjectionFingerprint;

        // Act - Validate injection
        var status = await _injectionService.GetInjectionStatusAsync(installation);

        // Assert - Validation should succeed with matching fingerprints
        status.IsFullyIntact.Should().BeTrue();
        status.ValidationStatus.Should().Be(InjectionValidationStatus.Valid);
        status.TargetFiles["window.html"].Fingerprint.Should().Be(originalFingerprint);
        status.TargetFiles["window.html"].ValidationStatus.Should().Be(InjectionValidationStatus.Valid);
    }

    [Fact]
    public async Task ValidateInjectionAsync_WithModifiedContent_DetectsFingerprintMismatch()
    {
        // Arrange
        var installation = CreateTestInstallation();
        var loaderPath = Path.Combine(_tempDirectory, "vivaldi-mods", "loader.js");
        var windowHtmlPath = Path.Combine(_tempDirectory, "window.html");
        
        Directory.CreateDirectory(Path.GetDirectoryName(loaderPath)!);
        await File.WriteAllTextAsync(loaderPath, "// Test loader content");
        await File.WriteAllTextAsync(windowHtmlPath, "<html><head></head><body></body></html>");

        var targets = new Dictionary<string, string>
        {
            ["window.html"] = windowHtmlPath
        }.AsReadOnly();

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        // Inject first
        await _injectionService.InjectAsync(installation, loaderPath);

        // Manually modify the fingerprint in the file (simulate tampering)
        var content = await File.ReadAllTextAsync(windowHtmlPath);
        var modifiedContent = content.Replace(installation.InjectionFingerprint!, "tampered123456");
        await File.WriteAllTextAsync(windowHtmlPath, modifiedContent);

        // Act - Validate injection
        var status = await _injectionService.GetInjectionStatusAsync(installation);

        // Assert - Should detect fingerprint mismatch
        status.IsFullyIntact.Should().BeFalse();
        status.ValidationStatus.Should().Be(InjectionValidationStatus.FingerprintMismatch);
        status.TargetFiles["window.html"].ValidationStatus.Should().Be(InjectionValidationStatus.FingerprintMismatch);
        status.TargetFiles["window.html"].ValidationErrors.Should().Contain(error => error.Contains("Fingerprint mismatch"));
    }

    [Fact]
    public async Task BackupAndRestoreWorkflow_WithFileOperations_WorksCorrectly()
    {
        // Arrange
        var installation = CreateTestInstallation();
        var windowHtmlPath = Path.Combine(_tempDirectory, "window.html");
        var originalContent = "<html><head><title>Original</title></head><body>Original Content</body></html>";
        
        await File.WriteAllTextAsync(windowHtmlPath, originalContent);

        var targets = new Dictionary<string, string>
        {
            ["window.html"] = windowHtmlPath
        }.AsReadOnly();

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        // Act 1 - Create backup
        var backups = await _injectionService.BackupTargetFilesAsync(installation);

        // Assert 1 - Backup was created
        backups.Should().ContainKey("window.html");
        File.Exists(backups["window.html"]).Should().BeTrue();
        
        var backupContent = await File.ReadAllTextAsync(backups["window.html"]);
        backupContent.Should().Be(originalContent);

        // Act 2 - Modify the original file
        var modifiedContent = "<html><head><title>Modified</title></head><body>Modified Content</body></html>";
        await File.WriteAllTextAsync(windowHtmlPath, modifiedContent);

        // Verify modification
        var currentContent = await File.ReadAllTextAsync(windowHtmlPath);
        currentContent.Should().Be(modifiedContent);

        // Act 3 - Restore from backup
        await _injectionService.RestoreTargetFilesAsync(installation);

        // Assert 3 - File was restored
        var restoredContent = await File.ReadAllTextAsync(windowHtmlPath);
        restoredContent.Should().Be(originalContent);

        // Installation metadata should be updated
        installation.LastInjectionAt.Should().BeNull();
        installation.InjectionFingerprint.Should().BeNull();
        installation.LastInjectionStatus.Should().Be("Restored");
    }

    /// <summary>
    /// Creates a test installation for testing purposes.
    /// </summary>
    /// <returns>A test VivaldiInstallation instance.</returns>
    private static VivaldiInstallation CreateTestInstallation()
    {
        return new VivaldiInstallation
        {
            Id = "integration-test-installation",
            Name = "Integration Test Vivaldi",
            InstallationPath = "/test/vivaldi",
            UserDataPath = "/test/vivaldi/userData",
            Version = "6.0.0",
            InstallationType = VivaldiInstallationType.Standard,
            IsActive = true,
            IsManaged = true
        };
    }
}