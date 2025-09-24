using System.Text.Json;
using FluentAssertions;
using VivaldiModManager.Core.Models;
using Xunit;

namespace VivaldiModManager.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="ModInfo"/> class.
/// </summary>
public class ModInfoTests
{
    [Fact]
    public void ModInfo_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var modInfo = new ModInfo();

        // Assert
        modInfo.Id.Should().BeEmpty();
        modInfo.Filename.Should().BeEmpty();
        modInfo.Enabled.Should().BeTrue();
        modInfo.Order.Should().Be(0);
        modInfo.Notes.Should().BeEmpty();
        modInfo.Checksum.Should().BeEmpty();
        modInfo.Version.Should().BeEmpty();
        modInfo.UrlScopes.Should().NotBeNull().And.BeEmpty();
        modInfo.LastKnownCompatibleVivaldi.Should().BeNull();
        modInfo.FileSize.Should().Be(0);
        modInfo.IsValidated.Should().BeFalse();
        modInfo.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        modInfo.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ModInfo_SerializesToJson_Successfully()
    {
        // Arrange
        var modInfo = new ModInfo
        {
            Id = "test-mod-id",
            Filename = "test-mod.js",
            Enabled = true,
            Order = 1,
            Notes = "Test mod notes",
            Checksum = "abcd1234",
            Version = "1.0.0",
            UrlScopes = new List<string> { "*://example.com/*" },
            LastKnownCompatibleVivaldi = "6.5.0",
            FileSize = 1024,
            IsValidated = true
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var json = JsonSerializer.Serialize(modInfo, options);
        var deserializedModInfo = JsonSerializer.Deserialize<ModInfo>(json, options);

        // Assert
        deserializedModInfo.Should().NotBeNull();
        deserializedModInfo!.Id.Should().Be(modInfo.Id);
        deserializedModInfo.Filename.Should().Be(modInfo.Filename);
        deserializedModInfo.Enabled.Should().Be(modInfo.Enabled);
        deserializedModInfo.Order.Should().Be(modInfo.Order);
        deserializedModInfo.Notes.Should().Be(modInfo.Notes);
        deserializedModInfo.Checksum.Should().Be(modInfo.Checksum);
        deserializedModInfo.Version.Should().Be(modInfo.Version);
        deserializedModInfo.UrlScopes.Should().BeEquivalentTo(modInfo.UrlScopes);
        deserializedModInfo.LastKnownCompatibleVivaldi.Should().Be(modInfo.LastKnownCompatibleVivaldi);
        deserializedModInfo.FileSize.Should().Be(modInfo.FileSize);
        deserializedModInfo.IsValidated.Should().Be(modInfo.IsValidated);
    }

    [Fact]
    public void ModInfo_JsonSerialization_UsesCorrectPropertyNames()
    {
        // Arrange
        var modInfo = new ModInfo
        {
            Id = "test-id",
            Filename = "test.js",
            LastKnownCompatibleVivaldi = "6.5.0"
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var json = JsonSerializer.Serialize(modInfo, options);

        // Assert
        json.Should().Contain("\"id\":");
        json.Should().Contain("\"filename\":");
        json.Should().Contain("\"lastKnownCompatibleVivaldi\":");
        json.Should().Contain("\"urlScopes\":");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ModInfo_WithEmptyOrNullId_ShouldStillSerialize(string? id)
    {
        // Arrange
        var modInfo = new ModInfo { Id = id ?? string.Empty };
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var json = JsonSerializer.Serialize(modInfo, options);
        var deserializedModInfo = JsonSerializer.Deserialize<ModInfo>(json, options);

        // Assert
        deserializedModInfo.Should().NotBeNull();
        deserializedModInfo!.Id.Should().Be(id ?? string.Empty);
    }

    [Fact]
    public void ModInfo_WithComplexUrlScopes_SerializesCorrectly()
    {
        // Arrange
        var modInfo = new ModInfo
        {
            Id = "test-id",
            UrlScopes = new List<string>
            {
                "*://example.com/*",
                "https://test.com/path/*",
                "*://*.domain.org/*"
            }
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var json = JsonSerializer.Serialize(modInfo, options);
        var deserializedModInfo = JsonSerializer.Deserialize<ModInfo>(json, options);

        // Assert
        deserializedModInfo.Should().NotBeNull();
        deserializedModInfo!.UrlScopes.Should().BeEquivalentTo(modInfo.UrlScopes);
        deserializedModInfo.UrlScopes.Should().HaveCount(3);
    }
}