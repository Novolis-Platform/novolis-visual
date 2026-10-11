namespace Novolis.Visual.Definitions;

/// <summary>
/// Stable identity for a connection in a recipe.
/// </summary>
public readonly record struct ConnectionId(Guid Value) : IComparable<ConnectionId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static ConnectionId New() => new(Guid.NewGuid());

    public static ConnectionId Parse(string value) => new(Guid.Parse(value));

    public int CompareTo(ConnectionId other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString("N");
}
