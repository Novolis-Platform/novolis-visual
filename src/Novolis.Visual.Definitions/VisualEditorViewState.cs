using System.Collections.Immutable;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Optional editor-only layout state. It is stored separately from the semantic
/// recipe and therefore cannot affect a recipe fingerprint.
/// </summary>
public sealed record VisualEditorViewState
{
    public VisualEditorViewState(
        IEnumerable<KeyValuePair<NodeId, VisualPoint>>? nodePositions = null,
        IEnumerable<NodeId>? selectedNodes = null,
        double zoom = 1d)
    {
        if (!double.IsFinite(zoom) || zoom <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zoom));
        }

        var positions = ImmutableSortedDictionary.CreateBuilder<NodeId, VisualPoint>();
        foreach (var position in nodePositions ?? [])
        {
            if (position.Key.IsEmpty)
            {
                throw new ArgumentException(
                    "A view position requires a node identifier.",
                    nameof(nodePositions));
            }

            positions[position.Key] = position.Value;
        }

        NodePositions = positions.ToImmutable();
        SelectedNodes = (selectedNodes ?? [])
            .Where(nodeId => !nodeId.IsEmpty)
            .ToImmutableHashSet();
        Zoom = zoom;
    }

    public ImmutableSortedDictionary<NodeId, VisualPoint> NodePositions { get; }

    public ImmutableHashSet<NodeId> SelectedNodes { get; }

    public double Zoom { get; }
}
