using VivaldiModManager.Core.Models;

namespace VivaldiModManager.Core.Extensions;

/// <summary>
/// Extension methods for domain models providing useful operations, validation helpers, and conversion utilities.
/// </summary>
public static class DomainModelExtensions
{
    /// <summary>
    /// Gets all enabled mods from the manifest ordered by their load order.
    /// </summary>
    /// <param name="manifest">The manifest data.</param>
    /// <returns>An ordered collection of enabled mods.</returns>
    public static IOrderedEnumerable<ModInfo> GetEnabledModsInOrder(this ManifestData manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        return manifest.Mods.Where(m => m.Enabled).OrderBy(m => m.Order);
    }

    /// <summary>
    /// Gets all disabled mods from the manifest.
    /// </summary>
    /// <param name="manifest">The manifest data.</param>
    /// <returns>A collection of disabled mods.</returns>
    public static IEnumerable<ModInfo> GetDisabledMods(this ManifestData manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        return manifest.Mods.Where(m => !m.Enabled);
    }

    /// <summary>
    /// Gets the active Vivaldi installation from the manifest.
    /// </summary>
    /// <param name="manifest">The manifest data.</param>
    /// <returns>The active installation, or null if none is marked as active.</returns>
    public static VivaldiInstallation? GetActiveInstallation(this ManifestData manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        return manifest.Installations.FirstOrDefault(i => i.IsActive);
    }

    /// <summary>
    /// Gets all managed Vivaldi installations from the manifest.
    /// </summary>
    /// <param name="manifest">The manifest data.</param>
    /// <returns>A collection of managed installations.</returns>
    public static IEnumerable<VivaldiInstallation> GetManagedInstallations(this ManifestData manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        return manifest.Installations.Where(i => i.IsManaged);
    }

    /// <summary>
    /// Finds a mod by its unique identifier.
    /// </summary>
    /// <param name="manifest">The manifest data.</param>
    /// <param name="modId">The mod identifier to search for.</param>
    /// <returns>The mod if found, otherwise null.</returns>
    public static ModInfo? FindModById(this ManifestData manifest, string modId)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        if (string.IsNullOrWhiteSpace(modId))
        {
            return null;
        }

        return manifest.Mods.FirstOrDefault(m => string.Equals(m.Id, modId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds an installation by its unique identifier.
    /// </summary>
    /// <param name="manifest">The manifest data.</param>
    /// <param name="installationId">The installation identifier to search for.</param>
    /// <returns>The installation if found, otherwise null.</returns>
    public static VivaldiInstallation? FindInstallationById(this ManifestData manifest, string installationId)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        if (string.IsNullOrWhiteSpace(installationId))
        {
            return null;
        }

        return manifest.Installations.FirstOrDefault(i => string.Equals(i.Id, installationId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Updates the timestamps on a mod to indicate it has been modified.
    /// </summary>
    /// <param name="mod">The mod to update.</param>
    /// <returns>The updated mod for method chaining.</returns>
    public static ModInfo Touch(this ModInfo mod)
    {
        if (mod == null)
        {
            throw new ArgumentNullException(nameof(mod));
        }

        mod.UpdatedAt = DateTimeOffset.UtcNow;
        return mod;
    }

    /// <summary>
    /// Updates the last verified timestamp on an installation.
    /// </summary>
    /// <param name="installation">The installation to update.</param>
    /// <returns>The updated installation for method chaining.</returns>
    public static VivaldiInstallation MarkAsVerified(this VivaldiInstallation installation)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        installation.LastVerifiedAt = DateTimeOffset.UtcNow;
        return installation;
    }

    /// <summary>
    /// Determines if a mod is stale based on its last modified timestamp.
    /// </summary>
    /// <param name="mod">The mod to check.</param>
    /// <param name="stalenessThreshold">The timespan after which a mod is considered stale. Defaults to 30 days.</param>
    /// <returns>True if the mod is stale, otherwise false.</returns>
    public static bool IsStale(this ModInfo mod, TimeSpan? stalenessThreshold = null)
    {
        if (mod == null)
        {
            throw new ArgumentNullException(nameof(mod));
        }

        stalenessThreshold ??= TimeSpan.FromDays(30);
        return DateTimeOffset.UtcNow - mod.UpdatedAt > stalenessThreshold;
    }

    /// <summary>
    /// Determines if an installation needs verification based on its last verified timestamp.
    /// </summary>
    /// <param name="installation">The installation to check.</param>
    /// <param name="verificationInterval">The interval after which verification is needed. Defaults to 1 hour.</param>
    /// <returns>True if verification is needed, otherwise false.</returns>
    public static bool NeedsVerification(this VivaldiInstallation installation, TimeSpan? verificationInterval = null)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        verificationInterval ??= TimeSpan.FromHours(1);
        return DateTimeOffset.UtcNow - installation.LastVerifiedAt > verificationInterval;
    }

    /// <summary>
    /// Creates a deep copy of a mod with a new unique identifier.
    /// </summary>
    /// <param name="mod">The mod to clone.</param>
    /// <param name="newId">The new unique identifier. If null, a new GUID is generated.</param>
    /// <returns>A new mod instance with the same properties but different ID.</returns>
    public static ModInfo Clone(this ModInfo mod, string? newId = null)
    {
        if (mod == null)
        {
            throw new ArgumentNullException(nameof(mod));
        }

        return new ModInfo
        {
            Id = newId ?? Guid.NewGuid().ToString(),
            Filename = mod.Filename,
            Enabled = mod.Enabled,
            Order = mod.Order,
            Notes = mod.Notes,
            Checksum = mod.Checksum,
            LastModified = mod.LastModified,
            Version = mod.Version,
            UrlScopes = new List<string>(mod.UrlScopes),
            LastKnownCompatibleVivaldi = mod.LastKnownCompatibleVivaldi,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            FileSize = mod.FileSize,
            IsValidated = mod.IsValidated
        };
    }

    /// <summary>
    /// Validates that a mod has all required properties set correctly.
    /// </summary>
    /// <param name="mod">The mod to validate.</param>
    /// <returns>True if the mod is valid, otherwise false.</returns>
    public static bool IsValid(this ModInfo mod)
    {
        if (mod == null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(mod.Id) &&
               !string.IsNullOrWhiteSpace(mod.Filename) &&
               mod.Order >= 0 &&
               mod.FileSize >= 0 &&
               mod.CreatedAt <= DateTimeOffset.UtcNow &&
               mod.UpdatedAt <= DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Validates that an installation has all required properties set correctly.
    /// </summary>
    /// <param name="installation">The installation to validate.</param>
    /// <returns>True if the installation is valid, otherwise false.</returns>
    public static bool IsValid(this VivaldiInstallation installation)
    {
        if (installation == null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(installation.Id) &&
               !string.IsNullOrWhiteSpace(installation.Name) &&
               !string.IsNullOrWhiteSpace(installation.InstallationPath) &&
               !string.IsNullOrWhiteSpace(installation.ApplicationPath) &&
               installation.DetectedAt <= DateTimeOffset.UtcNow &&
               installation.LastVerifiedAt <= DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the next available order number for a new mod.
    /// </summary>
    /// <param name="manifest">The manifest data.</param>
    /// <returns>The next available order number.</returns>
    public static int GetNextModOrder(this ManifestData manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        if (!manifest.Mods.Any())
        {
            return 1;
        }

        return manifest.Mods.Max(m => m.Order) + 1;
    }

    /// <summary>
    /// Reorders the mods to ensure sequential ordering starting from 1.
    /// </summary>
    /// <param name="manifest">The manifest data.</param>
    /// <returns>The manifest for method chaining.</returns>
    public static ManifestData ReorderMods(this ManifestData manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        var enabledMods = manifest.Mods.Where(m => m.Enabled).OrderBy(m => m.Order).ToList();
        for (int i = 0; i < enabledMods.Count; i++)
        {
            enabledMods[i].Order = i + 1;
        }

        return manifest;
    }
}