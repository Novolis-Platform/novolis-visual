using Novolis.Visual.Definitions;

namespace Novolis.Visual.Assets;

/// <summary>
/// Identifies a graph value controlled by an exposed asset parameter.
/// </summary>
public sealed record AssetParameterTarget
{
    public AssetParameterTarget(NodeId nodeId, string valueName)
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
