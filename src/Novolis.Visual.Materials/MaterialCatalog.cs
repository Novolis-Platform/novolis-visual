using System.Collections.Immutable;

namespace Novolis.Visual.Materials;

/// <summary>
/// Searchable material library used by headless tools and authoring hosts.
/// </summary>
public sealed class MaterialCatalog
{
    private readonly ImmutableArray<MaterialGraph> materials;

    public MaterialCatalog(IEnumerable<MaterialGraph>? materials = null)
    {
        this.materials = (materials ?? [])
            .OrderBy(material => material.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(material => material.MaterialId, StringComparer.Ordinal)
            .ToImmutableArray();
        if (this.materials
            .Select(material => material.MaterialId)
            .Distinct(StringComparer.Ordinal)
            .Count() != this.materials.Length)
        {
            throw new ArgumentException(
                "Material identifiers must be unique.",
                nameof(materials));
        }
    }

    public ImmutableArray<MaterialGraph> Materials => materials;

    public MaterialCatalog Add(MaterialGraph material)
    {
        ArgumentNullException.ThrowIfNull(material);
        return new MaterialCatalog(materials.Append(material));
    }

    public IEnumerable<MaterialGraph> Search(
        string? query = null,
        IEnumerable<string>? tags = null)
    {
        var normalized = query?.Trim();
        var requestedTags = (tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .ToArray();
        return materials
            .Where(material => string.IsNullOrWhiteSpace(normalized) ||
                               Matches(material, normalized!))
            .Where(material => requestedTags.Length == 0 ||
                               requestedTags.All(tag =>
                                   material.Tags.Contains(
                                       tag,
                                       StringComparer.OrdinalIgnoreCase)));
    }

    private static bool Matches(MaterialGraph material, string query) =>
        material.MaterialId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        material.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        material.Tags.Any(tag =>
            tag.Contains(query, StringComparison.OrdinalIgnoreCase));
}
