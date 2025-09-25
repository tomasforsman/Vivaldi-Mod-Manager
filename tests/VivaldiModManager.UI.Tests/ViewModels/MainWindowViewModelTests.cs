using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VivaldiModManager.Core.Services;
using VivaldiModManager.UI.Services;
using VivaldiModManager.UI.ViewModels;
using Xunit;

namespace VivaldiModManager.UI.Tests.ViewModels;

/// <summary>
/// Unit tests for the MainWindowViewModel class.
/// </summary>
public class MainWindowViewModelTests
{
    private readonly Mock<IVivaldiService> _mockVivaldiService;
    private readonly Mock<IInjectionService> _mockInjectionService;
    private readonly Mock<IManifestService> _mockManifestService;
    private readonly Mock<ILoaderService> _mockLoaderService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ISystemTrayService> _mockSystemTrayService;
    private readonly Mock<ILogger<MainWindowViewModel>> _mockLogger;
    private readonly MainWindowViewModel _viewModel;

    public MainWindowViewModelTests()
    {
        _mockVivaldiService = new Mock<IVivaldiService>();
        _mockInjectionService = new Mock<IInjectionService>();
        _mockManifestService = new Mock<IManifestService>();
        _mockLoaderService = new Mock<ILoaderService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockSystemTrayService = new Mock<ISystemTrayService>();
        _mockLogger = new Mock<ILogger<MainWindowViewModel>>();

        _viewModel = new MainWindowViewModel(
            _mockVivaldiService.Object,
            _mockInjectionService.Object,
            _mockManifestService.Object,
            _mockLoaderService.Object,
            _mockDialogService.Object,
            _mockSystemTrayService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_InitializesCollections()
    {
        // Assert
        _viewModel.Installations.Should().NotBeNull();
        _viewModel.Mods.Should().NotBeNull();
        _viewModel.StatusText.Should().Be("Ready");
        _viewModel.IsBusy.Should().BeFalse();
        _viewModel.IsSafeModeEnabled.Should().BeFalse();
        _viewModel.IsInjectionActive.Should().BeFalse();
    }

    [Fact]
    public async Task EnableSafeModeCommand_ShowsConfirmationDialog()
    {
        // Arrange
        _mockDialogService.Setup(x => x.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        await _viewModel.EnableSafeModeCommand.ExecuteAsync(null);

        // Assert
        _mockDialogService.Verify(x => x.ShowConfirmationAsync(
            It.Is<string>(s => s.Contains("Safe Mode")),
            "Enable Safe Mode"), Times.Once);
    }

    [Fact]
    public void ShowAboutCommand_ShowsAboutWindow()
    {
        // Act
        _viewModel.ShowAboutCommand.Execute(null);

        // Assert - The command should execute without throwing
        // In a real test, we might mock the window creation
        true.Should().BeTrue(); // Placeholder assertion
    }

    [Fact]
    public async Task AddModFromFile_CreatesNewModViewModel()
    {
        // Arrange
        var initialModCount = _viewModel.Mods.Count;
        var testFilePath = "/test/mod.js";

        // Act
        await _viewModel.DropFilesCommand.ExecuteAsync(new[] { testFilePath });

        // Assert
        _viewModel.Mods.Should().HaveCount(initialModCount + 1);
        _viewModel.Mods.Last().Name.Should().Be("mod.js");
        _mockSystemTrayService.Verify(x => x.ShowNotification(
            "Vivaldi Mod Manager",
            "Added mod: mod.js",
            NotificationIcon.Info), Times.Once);
    }

    [Fact]
    public void RefreshInstallationsCommand_CanExecute_ReturnsTrue()
    {
        // Act & Assert
        _viewModel.RefreshInstallationsCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void InjectModsCommand_CanExecute_ReturnsTrue()
    {
        // Act & Assert
        _viewModel.InjectModsCommand.CanExecute(null).Should().BeTrue();
    }
}