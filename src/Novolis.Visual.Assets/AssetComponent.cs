using Novolis.Visual.Definitions;

namespace Novolis.Visual.Assets;

/// <summary>
/// A typed component output in an asset assembly.
/// </summary>
public sealed record AssetComponent
{
    public AssetComponent(
        string id,
        string name,
        AssetComponentKind kind,
        VisualPortReference source,
        string? materialSlotId = null,
        AssetGeometryDefinition? geometry = null)
    {
        Id = Required(id, nameof(id));
        Name = Required(name, nameof(name));
        ArgumentNullException.ThrowIfNull(source);
        if (kind == AssetComponentKind.MaterialBinding &&
            string.IsNullOrWhiteSpace(materialSlotId))
        {
            throw new ArgumentException(
                "Material bindings require a material slot.",
                nameof(materialSlotId));
        }

        Kind = kind;
        Source = source;
        MaterialSlotId = string.IsNullOrWhiteSpace(materialSlotId)
            ? null
            : materialSlotId.Trim();
        Geometry = geometry;
    }

    public string Id { get; }

    public string Name { get; }

    public AssetComponentKind Kind { get; }

    public VisualPortReference Source { get; }

    public string? MaterialSlotId { get; }

    /// <summary>
    /// Optional portable geometry metadata for geometry components. The
    /// metadata is consumed by a Modeling adapter; it is not a renderer or
    /// host-specific scene object.
    /// </summary>
    public AssetGeometryDefinition? Geometry { get; }

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
