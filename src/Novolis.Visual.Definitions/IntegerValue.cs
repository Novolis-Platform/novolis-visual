namespace Novolis.Visual.Definitions;

/// <summary>
/// An integer value with an explicit unit.
/// </summary>
public sealed record IntegerValue : VisualValue
{
    public IntegerValue(long value, VisualUnit unit = VisualUnit.Unitless)
    {
        Value = value;
        Unit = unit;
    }

    public long Value { get; }

    public VisualUnit Unit { get; }

    public override VisualValueKind Kind => VisualValueKind.Integer;

    public override VisualTypeContract Contract => new(
        Kind,
        Unit,
        CoordinateSpace.None,
        ColorSpace.None);
}
