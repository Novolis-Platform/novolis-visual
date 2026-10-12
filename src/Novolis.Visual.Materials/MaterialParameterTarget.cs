using Novolis.Visual.Definitions;

namespace Novolis.Visual.Materials;

/// <summary>
/// Identifies the graph value changed by an exposed material control.
/// </summary>
public sealed record MaterialParameterTarget
{
    public MaterialParameterTarget(NodeId nodeId, string valueName)
    {
        if (nodeId.IsEmpty)
        {
            throw new ArgumentException(
                "A node identifier is required.",
                nameof(nodeId));
        }

        if (string.IsNullOrWhiteSpace(valueName))
        {
            throw new ArgumentException(
                "A node value name is required.",
                nameof(valueName));
        }

        NodeId = nodeId;
        ValueName = valueName.Trim();
    }

    public NodeId NodeId { get; }

    public string ValueName { get; }
}
