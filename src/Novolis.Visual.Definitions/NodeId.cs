using System.Globalization;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Stable identity for a node instance in a recipe.
/// </summary>
public readonly record struct NodeId(Guid Value) : IComparable<NodeId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static NodeId New() => new(Guid.NewGuid());

    public static NodeId Parse(string value) =>
        new(Guid.Parse(value, CultureInfo.InvariantCulture));

    public int CompareTo(NodeId other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString("N");
}
