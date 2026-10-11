namespace Novolis.Visual.Definitions;

/// <summary>
/// Stable, case-sensitive identifier for a node port.
/// </summary>
public readonly record struct PortId
{
    public PortId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A port identifier is required.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
