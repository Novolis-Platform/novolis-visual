using System.Collections.Immutable;
using Novolis.Visual.Assets;
using Novolis.Visual.Definitions;
using Novolis.Visual.Materials;

namespace Novolis.AssetStudio.Core;

/// <summary>
/// Host-neutral content catalog shared by the UI and headless workflows.
/// </summary>
public sealed class AssetStudioCatalog
{
    public AssetStudioCatalog(
        VisualNodeCatalog? nodes = null,
        MaterialCatalog? materials = null,
        IEnumerable<AssetRecipe>? assets = null)
    {
        Nodes = nodes ?? VisualNodeCatalog.CreateDefault();
        Materials = materials ?? new MaterialCatalog();
        Assets = (assets ?? [])
            .OrderBy(asset => asset.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(asset => asset.AssetId, StringComparer.Ordinal)
            .ToImmutableArray();
        if (Assets
            .Select(asset => asset.AssetId)
            .Distinct(StringComparer.Ordinal)
            .Count() != Assets.Length)
        {
            throw new ArgumentException(
                "Asset identifiers must be unique.",
                nameof(assets));
        }
    }

    public VisualNodeCatalog Nodes { get; }

    public MaterialCatalog Materials { get; }

    public ImmutableArray<AssetRecipe> Assets { get; }

    public IEnumerable<MaterialGraph> SearchMaterials(
        string? query = null,
        IEnumerable<string>? tags = null) =>
        Materials.Search(query, tags);

    public IEnumerable<AssetRecipe> SearchAssets(string? query = null)
    {
        var normalized = query?.Trim();
        return Assets.Where(
            asset => string.IsNullOrWhiteSpace(normalized) ||
                      asset.DisplayName.Contains(
                          normalized!,
                          StringComparison.OrdinalIgnoreCase) ||
                      asset.AssetId.Contains(
                          normalized!,
                          StringComparison.OrdinalIgnoreCase) ||
                      asset.Tags.Any(
                          tag => tag.Contains(
                              normalized!,
                              StringComparison.OrdinalIgnoreCase)));
    }

    public MaterialGraph? FindMaterial(string materialId) =>
        Materials.Materials.FirstOrDefault(
            material => string.Equals(
                material.MaterialId,
                materialId,
                StringComparison.Ordinal));

    public AssetRecipe? FindAsset(string assetId) =>
        Assets.FirstOrDefault(
            asset => string.Equals(
                asset.AssetId,
                assetId,
                StringComparison.Ordinal));

    public AssetStudioCatalog AddMaterial(MaterialGraph material) =>
        new(Nodes, Materials.Add(material), Assets);

    public AssetStudioCatalog AddAsset(AssetRecipe asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        return new(Nodes, Materials, Assets.Append(asset));
    }
}
