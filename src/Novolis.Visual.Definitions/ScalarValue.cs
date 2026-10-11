namespace Novolis.Visual.Definitions;

/// <summary>
/// A finite scalar with an explicit unit.
/// </summary>
public sealed record ScalarValue : VisualValue
{
    public ScalarValue(double value, VisualUnit unit = VisualUnit.Unitless)
    {
        Value = VisualValueValidation.Finite(value, nameof(value));
        Unit = unit;
    }

    public double Value { get; }

    public VisualUnit Unit { get; }

    public override VisualValueKind Kind => VisualValueKind.Scalar;

    public override VisualTypeContract Contract => new(
        Kind,
        Unit,
        CoordinateSpace.None,
        ColorSpace.None);
}
