using System.Numerics;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Renderer-neutral projected decal definition.
/// </summary>
public sealed record DecalValue : VisualValue
{
    public DecalValue(
        string decalId,
        DecalProjection projection,
        Vector3 size,
        ColorValue color,
        double opacity,
        string? imageAssetId = null)
    {
        DecalId = VisualValueValidation.Required(decalId, nameof(decalId));
        ArgumentNullException.ThrowIfNull(color);
        if (!double.IsFinite(opacity) || opacity is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }

        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0 ||
            !float.IsFinite(size.X) ||
            !float.IsFinite(size.Y) ||
            !float.IsFinite(size.Z))
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        if (imageAssetId is not null)
        {
            VisualValueValidation.Required(imageAssetId, nameof(imageAssetId));
        }

        Projection = projection;
        Size = size;
        Color = color;
        Opacity = opacity;
        ImageAssetId = imageAssetId;
    }

    public string DecalId { get; }

    public DecalProjection Projection { get; }

    public Vector3 Size { get; }

    public ColorValue Color { get; }

    public double Opacity { get; }

    public string? ImageAssetId { get; }

    public override VisualValueKind Kind => VisualValueKind.Decal;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Unitless,
        CoordinateSpace.World,
        ColorSpace.None,
        FieldFrequency.Uniform,
        FieldPurity.ContextDependent);
}
