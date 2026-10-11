namespace Novolis.Visual.Definitions;

/// <summary>
/// A renderer-neutral participating-volume description.
/// </summary>
public sealed record VolumeValue : VisualValue
{
    public VolumeValue(
        string volumeId,
        VolumeKind volumeKind,
        double density,
        VisualUnit densityUnit,
        ColorValue albedo,
        double anisotropy)
    {
        VolumeId = VisualValueValidation.Required(volumeId, nameof(volumeId));
        ArgumentNullException.ThrowIfNull(albedo);

        if (density < 0 || !double.IsFinite(density))
        {
            throw new ArgumentOutOfRangeException(nameof(density));
        }

        if (anisotropy is < -1 or > 1 || !double.IsFinite(anisotropy))
        {
            throw new ArgumentOutOfRangeException(nameof(anisotropy));
        }

        VolumeKind = volumeKind;
        Density = density;
        DensityUnit = densityUnit;
        Albedo = albedo;
        Anisotropy = anisotropy;
    }

    public string VolumeId { get; }

    public VolumeKind VolumeKind { get; }

    public double Density { get; }

    public VisualUnit DensityUnit { get; }

    public ColorValue Albedo { get; }

    public double Anisotropy { get; }

    public override VisualValueKind Kind => VisualValueKind.Volume;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Unitless,
        CoordinateSpace.None,
        ColorSpace.None);
}
