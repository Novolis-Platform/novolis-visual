using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// Immutable source locations for normalized nodes and outputs.
/// </summary>
public sealed class VisualSourceMap
{
    private readonly ImmutableDictionary<NodeId, VisualSourceMapEntry> nodes;
    private readonly ImmutableDictionary<string, VisualSourceMapEntry> outputs;

    public VisualSourceMap(
        IEnumerable<VisualSourceMapEntry>? entries = null)
    {
        var nodeBuilder = ImmutableDictionary.CreateBuilder<NodeId, VisualSourceMapEntry>();
        var outputBuilder = ImmutableDictionary.CreateBuilder<
            string,
            VisualSourceMapEntry>(StringComparer.Ordinal);

        foreach (var entry in entries ?? [])
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (entry.NodeId is NodeId nodeId)
            {
                nodeBuilder[nodeId] = entry;
            }

            if (!string.IsNullOrWhiteSpace(entry.OutputId))
            {
                outputBuilder[entry.OutputId] = entry;
            }
        }

        nodes = nodeBuilder.ToImmutable();
        outputs = outputBuilder.ToImmutable();
    }

    public ImmutableArray<VisualSourceMapEntry> Entries =>
        nodes.Values
            .Concat(outputs.Values)
            .OrderBy(entry => entry.Path, StringComparer.Ordinal)
            .ToImmutableArray();

    public bool TryGetNode(NodeId id, out VisualSourceMapEntry? entry) =>
        nodes.TryGetValue(id, out entry);

    public bool TryGetOutput(string id, out VisualSourceMapEntry? entry) =>
        outputs.TryGetValue(id, out entry);
}
