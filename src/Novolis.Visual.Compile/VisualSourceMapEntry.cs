using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// A deterministic mapping from an IR item back to recipe source.
/// </summary>
public sealed record VisualSourceMapEntry
{
    public VisualSourceMapEntry(
        string path,
        NodeId? nodeId = null,
        string? outputId = null,
        string? portId = null,
        string? typeId = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A source-map path is required.", nameof(path));
        }

        if (nodeId is null && string.IsNullOrWhiteSpace(outputId))
        {
            throw new ArgumentException(
                "A source-map entry must identify a node or output.",
                nameof(nodeId));
        }

        Path = path;
        NodeId = nodeId;
        OutputId = outputId;
        PortId = portId;
        TypeId = typeId;
    }

    public string Path { get; }

    public NodeId? NodeId { get; }

    public string? OutputId { get; }

    public string? PortId { get; }

    public string? TypeId { get; }
}
