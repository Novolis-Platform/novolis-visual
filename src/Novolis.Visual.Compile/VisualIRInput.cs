using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// One normalized input edge of an IR node.
/// </summary>
public sealed record VisualIRInput
{
    public VisualIRInput(
        PortId targetPort,
        NodeId sourceNodeId,
        PortId sourcePort,
        VisualTypeContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        TargetPort = targetPort;
        SourceNodeId = sourceNodeId;
        SourcePort = sourcePort;
        Contract = contract;
    }

    public PortId TargetPort { get; }

    public PortId Port => TargetPort;

    public NodeId SourceNodeId { get; }

    public PortId SourcePort { get; }

    public VisualTypeContract Contract { get; }
}
