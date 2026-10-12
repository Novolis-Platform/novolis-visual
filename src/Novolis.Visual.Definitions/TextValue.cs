namespace Novolis.Visual.Definitions;

/// <summary>
/// A deterministic user-authored string such as sign lettering or a resource
/// name. Text is semantic data, not executable content.
/// </summary>
public sealed record TextValue : VisualValue
{
    public TextValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    public string Value { get; }

    public override VisualValueKind Kind => VisualValueKind.Text;

    public override VisualTypeContract Contract { get; } =
        new(
            VisualValueKind.Text,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.None);
}
