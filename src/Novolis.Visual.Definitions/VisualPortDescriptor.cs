namespace Novolis.Visual.Definitions;

/// <summary>
/// An allowlisted node port and its complete field contract.
/// </summary>
public sealed record VisualPortDescriptor
{
    public VisualPortDescriptor(
        PortId id,
        PortDirection direction,
        VisualTypeContract contract,
        bool required = true,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException(
                "A port identifier is required.",
                nameof(id));
        }

        ArgumentNullException.ThrowIfNull(contract);
        Id = id;
        Direction = direction;
        Contract = contract;
        Required = required;
        Description = description;
    }

    public PortId Id { get; }

    public PortDirection Direction { get; }

    public VisualTypeContract Contract { get; }

    public bool Required { get; }

    public string? Description { get; }
}
