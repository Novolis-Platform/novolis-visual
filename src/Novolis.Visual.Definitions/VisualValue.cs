namespace Novolis.Visual.Definitions;

/// <summary>
/// Base type for immutable values stored in recipes and node parameters.
/// </summary>
public abstract record VisualValue
{
    public abstract VisualValueKind Kind { get; }

    public abstract VisualTypeContract Contract { get; }
}
