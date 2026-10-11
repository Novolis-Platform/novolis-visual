namespace Novolis.Visual.Definitions;

/// <summary>
/// A renderer-neutral light description.
/// </summary>
public sealed record LightValue : VisualValue
{
    public LightValue(
        LightKind lightKind,
        ColorValue color,
        double intensity,
        VisualUnit intensityUnit,
        TransformValue transform)
    {
        ArgumentNullException.ThrowIfNull(color);
        ArgumentNullException.ThrowIfNull(transform);

        if (intensity < 0 || !double.IsFinite(intensity))
        {
            throw new ArgumentOutOfRangeException(nameof(intensity));
        }

        LightKind = lightKind;
        Color = color;
        Intensity = intensity;
        IntensityUnit = intensityUnit;
        Transform = transform;
    }

    public LightKind LightKind { get; }

    public ColorValue Color { get; }

    public double Intensity { get; }

    public VisualUnit IntensityUnit { get; }

    public TransformValue Transform { get; }

    public override VisualValueKind Kind => VisualValueKind.Light;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Unitless,
        CoordinateSpace.None,
        ColorSpace.None);
}
