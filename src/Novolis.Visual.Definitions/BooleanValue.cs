namespace Novolis.Visual.Definitions;

/// <summary>
/// A boolean value.
/// </summary>
public sealed record BooleanValue(bool Value) : VisualValue
{
    public override VisualValueKind Kind => VisualValueKind.Boolean;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Unitless,
        CoordinateSpace.None,
        ColorSpace.None);
}
