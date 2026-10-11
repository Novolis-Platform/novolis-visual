namespace Novolis.Visual.Definitions;

/// <summary>
/// A compact physically based surface description.
/// </summary>
public sealed record SurfaceValue : VisualValue
{
    public SurfaceValue(
        string materialId,
        ColorValue baseColor,
        double roughness,
        double metallic)
    {
        MaterialId = VisualValueValidation.Required(materialId, nameof(materialId));
        ArgumentNullException.ThrowIfNull(baseColor);

        if (roughness is < 0 or > 1 || !double.IsFinite(roughness))
        {
            throw new ArgumentOutOfRangeException(nameof(roughness));
        }

        if (metallic is < 0 or > 1 || !double.IsFinite(metallic))
        {
            throw new ArgumentOutOfRangeException(nameof(metallic));
        }

        BaseColor = baseColor;
        Roughness = roughness;
        Metallic = metallic;
    }

    public string MaterialId { get; }

    public ColorValue BaseColor { get; }

    public double Roughness { get; }

    public double Metallic { get; }

    public override VisualValueKind Kind => VisualValueKind.Surface;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Unitless,
        CoordinateSpace.None,
        ColorSpace.None);
}
