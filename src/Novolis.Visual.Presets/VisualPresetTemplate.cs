using System.Collections.Immutable;
using Novolis.Visual.Assets;
using Novolis.Visual.Materials;

namespace Novolis.Visual.Presets;

/// <summary>
/// A curated no-code material and asset starting point.
/// </summary>
public sealed record VisualPresetTemplate
{
    public VisualPresetTemplate(
        string id,
        string displayName,
        string description,
        AssetRecipe asset,
        IEnumerable<MaterialGraph>? materials = null,
        IEnumerable<string>? tags = null)
    {
        Id = Required(id, nameof(id));
        DisplayName = Required(displayName, nameof(displayName));
        Description = Required(description, nameof(description));
        ArgumentNullException.ThrowIfNull(asset);
        Asset = asset;
        Materials = (materials ?? [])
            .OrderBy(material => material.MaterialId, StringComparer.Ordinal)
            .ToImmutableArray();
        Tags = (tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public AssetRecipe Asset { get; }

    public ImmutableArray<MaterialGraph> Materials { get; }

    public ImmutableArray<string> Tags { get; }

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
}
