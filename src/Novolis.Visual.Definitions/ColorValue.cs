namespace Novolis.Visual.Definitions;

/// <summary>
/// A four-channel color with an explicit encoding.
/// </summary>
public sealed record ColorValue : VisualValue
{
    public ColorValue(
        double red,
        double green,
        double blue,
        double alpha = 1d,
        ColorSpace colorSpace = ColorSpace.LinearSrgb)
    {
        Red = VisualValueValidation.Finite(red, nameof(red));
        Green = VisualValueValidation.Finite(green, nameof(green));
        Blue = VisualValueValidation.Finite(blue, nameof(blue));
        Alpha = VisualValueValidation.Finite(alpha, nameof(alpha));

        if (colorSpace == ColorSpace.None)
        {
            throw new ArgumentException(
                "A color requires an explicit color space.",
                nameof(colorSpace));
        }

        ColorSpace = colorSpace;
    }

    public double Red { get; }

    public double Green { get; }

    public double Blue { get; }

    public double Alpha { get; }

    public ColorSpace ColorSpace { get; }

    public override VisualValueKind Kind => VisualValueKind.Color;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Unitless,
        CoordinateSpace.None,
        ColorSpace);
}
