namespace Novolis.Visual.Definitions;

/// <summary>
/// A typed, directed connection between two node ports.
/// </summary>
public sealed record VisualConnection
{
    public VisualConnection(
        ConnectionId id,
        VisualPortReference source,
        VisualPortReference target,
        VisualTypeContract sourceContract,
        VisualTypeContract targetContract)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException(
                "A connection identifier is required.",
                nameof(id));
        }

        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(sourceContract);
        ArgumentNullException.ThrowIfNull(targetContract);

        Id = id;
        Source = source;
        Target = target;
        SourceContract = sourceContract;
        TargetContract = targetContract;
    }

    public ConnectionId Id { get; }

    public VisualPortReference Source { get; }

    public VisualPortReference Target { get; }

    public VisualTypeContract SourceContract { get; }

    public VisualTypeContract TargetContract { get; }

    public bool IsTypeCompatible =>
        SourceContract.IsCompatibleWith(TargetContract);
}
