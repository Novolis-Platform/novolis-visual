namespace Novolis.Visual.Definitions;

/// <summary>
/// One deterministic frame effect request.
/// </summary>
public sealed record FrameEffectValue : VisualValue
{
    public FrameEffectValue(
        FrameEffectKind effectKind,
        double amount = 1,
        double threshold = 1,
        double radius = 1)
    {
        if (!double.IsFinite(amount) || amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (!double.IsFinite(threshold) || threshold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold));
        }

        if (!double.IsFinite(radius) || radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }

        EffectKind = effectKind;
        Amount = amount;
        Threshold = threshold;
        Radius = radius;
    }

    public FrameEffectKind EffectKind { get; }

    public double Amount { get; }

    public double Threshold { get; }

    public double Radius { get; }

    public override VisualValueKind Kind => VisualValueKind.FrameEffect;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Unitless,
        CoordinateSpace.Screen,
        ColorSpace.LinearSrgb,
        FieldFrequency.Frame,
        FieldPurity.ContextDependent);
}
