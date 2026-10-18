using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Assets;

/// <summary>
/// A complete reusable procedural asset assembly.
/// </summary>
public sealed record AssetRecipe
{
    public AssetRecipe(
        string assetId,
        string displayName,
        VisualRecipe graph,
        IEnumerable<AssetComponent>? components = null,
        IEnumerable<AssetMaterialSlot>? materialSlots = null,
        IEnumerable<AssetParameterDefinition>? parameters = null,
        IEnumerable<string>? tags = null,
        int moduleVersion = 1,
        IEnumerable<VisualDependencyFingerprint>? dependencies = null)
    {
        AssetId = Required(assetId, nameof(assetId));
        DisplayName = Required(displayName, nameof(displayName));
        ArgumentNullException.ThrowIfNull(graph);
        if (moduleVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(moduleVersion));
        }

        Graph = graph;
        ModuleVersion = moduleVersion;
        Components = (components ?? [])
            .OrderBy(component => component.Id, StringComparer.Ordinal)
            .ToImmutableArray();
        MaterialSlots = (materialSlots ?? [])
            .OrderBy(slot => slot.Id, StringComparer.Ordinal)
            .ToImmutableArray();
        Parameters = (parameters ?? [])
            .OrderBy(parameter => parameter.Id, StringComparer.Ordinal)
            .ToImmutableArray();
        Tags = (tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
        Dependencies = (dependencies ?? [])
            .Select(dependency =>
            {
                ArgumentNullException.ThrowIfNull(dependency);
                return dependency;
            })
            .OrderBy(dependency => dependency.Id, StringComparer.Ordinal)
            .ToImmutableArray();

        EnsureUnique(Components.Select(component => component.Id), "component");
        EnsureUnique(MaterialSlots.Select(slot => slot.Id), "material slot");
        EnsureUnique(Parameters.Select(parameter => parameter.Id), "asset parameter");
        EnsureUnique(
            Dependencies.Select(dependency => dependency.Id),
            "asset dependency");
    }

    public string AssetId { get; }

    public string DisplayName { get; }

    public VisualRecipe Graph { get; }

    public int ModuleVersion { get; }

    public ImmutableArray<VisualDependencyFingerprint> Dependencies { get; }

    public ImmutableArray<AssetComponent> Components { get; }

    public ImmutableArray<AssetMaterialSlot> MaterialSlots { get; }

    public ImmutableArray<AssetParameterDefinition> Parameters { get; }

    public ImmutableArray<string> Tags { get; }

    /// <summary>
    /// Stable fingerprint of the complete reusable asset module.
    /// </summary>
    public string Fingerprint =>
        Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(
                        AssetRecipeSerializer.Serialize(this))))
            .ToLowerInvariant();

    public AssetParameterDefinition? FindParameter(string id) =>
        Parameters.FirstOrDefault(
            parameter => string.Equals(
                parameter.Id,
                id,
                StringComparison.Ordinal));

    public AssetMaterialSlot? FindSlot(string id) =>
        MaterialSlots.FirstOrDefault(
            slot => string.Equals(
                slot.Id,
                id,
                StringComparison.Ordinal));

    private static string Required(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A non-empty value is required.",
                name);
        }

        return value.Trim();
    }

    private static void EnsureUnique(
        IEnumerable<string> values,
        string description)
    {
        var array = values.ToArray();
        if (array.Distinct(StringComparer.Ordinal).Count() != array.Length)
        {
            throw new ArgumentException(
                $"Duplicate {description} identifiers are not allowed.");
        }
    }
}
