using FluentAssertions;
using VivaldiModManager.Core.Extensions;
using VivaldiModManager.Core.Models;
using Xunit;

namespace VivaldiModManager.Core.Tests.Extensions;

/// <summary>
/// Unit tests for the <see cref="DomainModelExtensions"/> class.
/// </summary>
public class DomainModelExtensionsTests
{
    [Fact]
    public void GetEnabledModsInOrder_WithNullManifest_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ((ManifestData)null!).GetEnabledModsInOrder());
    }

    [Fact]
    public void GetEnabledModsInOrder_WithEnabledMods_ReturnsOrderedMods()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Mods = new List<ModInfo>
            {
                new() { Id = "mod3", Enabled = true, Order = 3 },
                new() { Id = "mod1", Enabled = true, Order = 1 },
                new() { Id = "mod2", Enabled = false, Order = 2 },
                new() { Id = "mod4", Enabled = true, Order = 4 }
            }
        };

        // Act
        var enabledMods = manifest.GetEnabledModsInOrder().ToList();

        // Assert
        enabledMods.Should().HaveCount(3);
        enabledMods[0].Id.Should().Be("mod1");
        enabledMods[1].Id.Should().Be("mod3");
        enabledMods[2].Id.Should().Be("mod4");
    }

    [Fact]
    public void GetDisabledMods_ReturnsOnlyDisabledMods()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Mods = new List<ModInfo>
            {
                new() { Id = "enabled1", Enabled = true },
                new() { Id = "disabled1", Enabled = false },
                new() { Id = "enabled2", Enabled = true },
                new() { Id = "disabled2", Enabled = false }
            }
        };

        // Act
        var disabledMods = manifest.GetDisabledMods().ToList();

        // Assert
        disabledMods.Should().HaveCount(2);
        disabledMods.Should().Contain(m => m.Id == "disabled1");
        disabledMods.Should().Contain(m => m.Id == "disabled2");
    }

    [Fact]
    public void GetActiveInstallation_WithActiveInstallation_ReturnsActiveInstallation()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Installations = new List<VivaldiInstallation>
            {
                new() { Id = "install1", IsActive = false },
                new() { Id = "install2", IsActive = true },
                new() { Id = "install3", IsActive = false }
            }
        };

        // Act
        var activeInstallation = manifest.GetActiveInstallation();

        // Assert
        activeInstallation.Should().NotBeNull();
        activeInstallation!.Id.Should().Be("install2");
    }

    [Fact]
    public void GetActiveInstallation_WithNoActiveInstallation_ReturnsNull()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Installations = new List<VivaldiInstallation>
            {
                new() { Id = "install1", IsActive = false },
                new() { Id = "install2", IsActive = false }
            }
        };

        // Act
        var activeInstallation = manifest.GetActiveInstallation();

        // Assert
        activeInstallation.Should().BeNull();
    }

    [Fact]
    public void GetManagedInstallations_ReturnsOnlyManagedInstallations()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Installations = new List<VivaldiInstallation>
            {
                new() { Id = "managed1", IsManaged = true },
                new() { Id = "unmanaged1", IsManaged = false },
                new() { Id = "managed2", IsManaged = true }
            }
        };

        // Act
        var managedInstallations = manifest.GetManagedInstallations().ToList();

        // Assert
        managedInstallations.Should().HaveCount(2);
        managedInstallations.Should().Contain(i => i.Id == "managed1");
        managedInstallations.Should().Contain(i => i.Id == "managed2");
    }

    [Fact]
    public void FindModById_WithExistingId_ReturnsMod()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Mods = new List<ModInfo>
            {
                new() { Id = "mod1", Filename = "mod1.js" },
                new() { Id = "mod2", Filename = "mod2.js" }
            }
        };

        // Act
        var mod = manifest.FindModById("mod2");

        // Assert
        mod.Should().NotBeNull();
        mod!.Filename.Should().Be("mod2.js");
    }

    [Fact]
    public void FindModById_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Mods = new List<ModInfo>
            {
                new() { Id = "mod1", Filename = "mod1.js" }
            }
        };

        // Act
        var mod = manifest.FindModById("nonexistent");

        // Assert
        mod.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FindModById_WithInvalidId_ReturnsNull(string? modId)
    {
        // Arrange
        var manifest = new ManifestData
        {
            Mods = new List<ModInfo>
            {
                new() { Id = "mod1", Filename = "mod1.js" }
            }
        };

        // Act
        var mod = manifest.FindModById(modId!);

        // Assert
        mod.Should().BeNull();
    }

    [Fact]
    public void FindInstallationById_WithExistingId_ReturnsInstallation()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Installations = new List<VivaldiInstallation>
            {
                new() { Id = "install1", Name = "Installation 1" },
                new() { Id = "install2", Name = "Installation 2" }
            }
        };

        // Act
        var installation = manifest.FindInstallationById("install2");

        // Assert
        installation.Should().NotBeNull();
        installation!.Name.Should().Be("Installation 2");
    }

    [Fact]
    public void Touch_UpdatesModTimestamp()
    {
        // Arrange
        var mod = new ModInfo { Id = "test-mod" };
        var originalTimestamp = mod.UpdatedAt;

        // Act
        var result = mod.Touch();

        // Assert
        result.Should().BeSameAs(mod);
        mod.UpdatedAt.Should().BeAfter(originalTimestamp);
    }

    [Fact]
    public void Touch_WithNullMod_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ((ModInfo)null!).Touch());
    }

    [Fact]
    public void MarkAsVerified_UpdatesInstallationTimestamp()
    {
        // Arrange
        var installation = new VivaldiInstallation { Id = "test-installation" };
        var originalTimestamp = installation.LastVerifiedAt;

        // Act
        var result = installation.MarkAsVerified();

        // Assert
        result.Should().BeSameAs(installation);
        installation.LastVerifiedAt.Should().BeAfter(originalTimestamp);
    }

    [Fact]
    public void IsStale_WithOldMod_ReturnsTrue()
    {
        // Arrange
        var mod = new ModInfo 
        { 
            Id = "test-mod",
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-35) // 35 days old
        };

        // Act
        var isStale = mod.IsStale();

        // Assert
        isStale.Should().BeTrue();
    }

    [Fact]
    public void IsStale_WithRecentMod_ReturnsFalse()
    {
        // Arrange
        var mod = new ModInfo 
        { 
            Id = "test-mod",
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-5) // 5 days old
        };

        // Act
        var isStale = mod.IsStale();

        // Assert
        isStale.Should().BeFalse();
    }

    [Fact]
    public void IsStale_WithCustomThreshold_UsesCustomThreshold()
    {
        // Arrange
        var mod = new ModInfo 
        { 
            Id = "test-mod",
            UpdatedAt = DateTimeOffset.UtcNow.AddHours(-2) // 2 hours old
        };

        // Act
        var isStale = mod.IsStale(TimeSpan.FromHours(1)); // 1 hour threshold

        // Assert
        isStale.Should().BeTrue();
    }

    [Fact]
    public void NeedsVerification_WithOldVerification_ReturnsTrue()
    {
        // Arrange
        var installation = new VivaldiInstallation 
        { 
            Id = "test-installation",
            LastVerifiedAt = DateTimeOffset.UtcNow.AddHours(-2) // 2 hours ago
        };

        // Act
        var needsVerification = installation.NeedsVerification();

        // Assert
        needsVerification.Should().BeTrue();
    }

    [Fact]
    public void NeedsVerification_WithRecentVerification_ReturnsFalse()
    {
        // Arrange
        var installation = new VivaldiInstallation 
        { 
            Id = "test-installation",
            LastVerifiedAt = DateTimeOffset.UtcNow.AddMinutes(-30) // 30 minutes ago
        };

        // Act
        var needsVerification = installation.NeedsVerification();

        // Assert
        needsVerification.Should().BeFalse();
    }

    [Fact]
    public void Clone_CreatesNewModWithSameProperties()
    {
        // Arrange
        var originalMod = new ModInfo
        {
            Id = "original-id",
            Filename = "test.js",
            Enabled = true,
            Order = 5,
            Notes = "Test notes",
            Checksum = "abc123",
            Version = "1.0.0",
            UrlScopes = new List<string> { "*://example.com/*" },
            FileSize = 1024,
            IsValidated = true
        };

        // Act
        var clonedMod = originalMod.Clone();

        // Assert
        clonedMod.Should().NotBeSameAs(originalMod);
        clonedMod.Id.Should().NotBe(originalMod.Id);
        clonedMod.Filename.Should().Be(originalMod.Filename);
        clonedMod.Enabled.Should().Be(originalMod.Enabled);
        clonedMod.Order.Should().Be(originalMod.Order);
        clonedMod.Notes.Should().Be(originalMod.Notes);
        clonedMod.Checksum.Should().Be(originalMod.Checksum);
        clonedMod.Version.Should().Be(originalMod.Version);
        clonedMod.UrlScopes.Should().BeEquivalentTo(originalMod.UrlScopes);
        clonedMod.UrlScopes.Should().NotBeSameAs(originalMod.UrlScopes);
        clonedMod.FileSize.Should().Be(originalMod.FileSize);
        clonedMod.IsValidated.Should().Be(originalMod.IsValidated);
    }

    [Fact]
    public void Clone_WithSpecificId_UsesSpecifiedId()
    {
        // Arrange
        var originalMod = new ModInfo { Id = "original-id" };
        var newId = "custom-new-id";

        // Act
        var clonedMod = originalMod.Clone(newId);

        // Assert
        clonedMod.Id.Should().Be(newId);
    }

    [Fact]
    public void IsValid_WithValidMod_ReturnsTrue()
    {
        // Arrange
        var mod = new ModInfo
        {
            Id = "valid-id",
            Filename = "valid.js",
            Order = 1,
            FileSize = 1024,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        // Act
        var isValid = mod.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "valid.js", 1, 1024)] // Empty ID
    [InlineData("valid-id", "", 1, 1024)] // Empty filename
    [InlineData("valid-id", "valid.js", -1, 1024)] // Negative order
    [InlineData("valid-id", "valid.js", 1, -1)] // Negative file size
    public void IsValid_WithInvalidMod_ReturnsFalse(string id, string filename, int order, long fileSize)
    {
        // Arrange
        var mod = new ModInfo
        {
            Id = id,
            Filename = filename,
            Order = order,
            FileSize = fileSize,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        // Act
        var isValid = mod.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithValidInstallation_ReturnsTrue()
    {
        // Arrange
        var installation = new VivaldiInstallation
        {
            Id = "valid-id",
            Name = "Valid Installation",
            InstallationPath = @"C:\Vivaldi\Installation",
            ApplicationPath = @"C:\Vivaldi\Application",
            DetectedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            LastVerifiedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        // Act
        var isValid = installation.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void GetNextModOrder_WithEmptyMods_ReturnsOne()
    {
        // Arrange
        var manifest = new ManifestData { Mods = new List<ModInfo>() };

        // Act
        var nextOrder = manifest.GetNextModOrder();

        // Assert
        nextOrder.Should().Be(1);
    }

    [Fact]
    public void GetNextModOrder_WithExistingMods_ReturnsMaxPlusOne()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Mods = new List<ModInfo>
            {
                new() { Order = 3 },
                new() { Order = 1 },
                new() { Order = 5 }
            }
        };

        // Act
        var nextOrder = manifest.GetNextModOrder();

        // Assert
        nextOrder.Should().Be(6);
    }

    [Fact]
    public void ReorderMods_ResequencesEnabledMods()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Mods = new List<ModInfo>
            {
                new() { Id = "mod1", Enabled = true, Order = 5 },
                new() { Id = "mod2", Enabled = false, Order = 3 },
                new() { Id = "mod3", Enabled = true, Order = 10 },
                new() { Id = "mod4", Enabled = true, Order = 1 }
            }
        };

        // Act
        var result = manifest.ReorderMods();

        // Assert
        result.Should().BeSameAs(manifest);
        
        var enabledMods = manifest.Mods.Where(m => m.Enabled).OrderBy(m => m.Order).ToList();
        enabledMods[0].Id.Should().Be("mod4"); // Originally order 1
        enabledMods[0].Order.Should().Be(1);
        enabledMods[1].Id.Should().Be("mod1"); // Originally order 5
        enabledMods[1].Order.Should().Be(2);
        enabledMods[2].Id.Should().Be("mod3"); // Originally order 10
        enabledMods[2].Order.Should().Be(3);

        // Disabled mod should retain original order
        var disabledMod = manifest.Mods.First(m => !m.Enabled);
        disabledMod.Order.Should().Be(3); // Unchanged
    }
}