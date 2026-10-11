namespace Novolis.Visual.Definitions;

/// <summary>
/// A stable reference to one node port.
/// </summary>
public sealed record VisualPortReference
{
    public VisualPortReference(NodeId nodeId, PortId portId)
    {
        if (nodeId.IsEmpty)
        {
            throw new ArgumentException(
                "A port reference requires a node identifier.",
                nameof(nodeId));
        }

        if (string.IsNullOrWhiteSpace(portId.Value))
        {
            throw new ArgumentException(
                "A port reference requires a port identifier.",
                nameof(portId));
        }

        NodeId = nodeId;
        PortId = portId;
    }

    public NodeId NodeId { get; }

    public PortId PortId { get; }

    public override string ToString() => $"{NodeId}:{PortId}";
}
