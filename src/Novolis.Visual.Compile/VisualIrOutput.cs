using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// A normalized recipe output binding.
/// </summary>
public sealed record VisualIROutput
{
    public VisualIROutput(
        string id,
        string name,
        NodeId sourceNodeId,
        PortId sourcePort,
        VisualTypeContract contract,
        bool required = true)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("An IR output identifier is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An IR output name is required.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(contract);
        Id = id;
        Name = name;
        SourceNodeId = sourceNodeId;
        SourcePort = sourcePort;
        Contract = contract;
        Required = required;
    }

    public string Id { get; }

    public string Name { get; }

    public NodeId SourceNodeId { get; }

    public PortId SourcePort { get; }

    public VisualTypeContract Contract { get; }

    public bool Required { get; }
}
