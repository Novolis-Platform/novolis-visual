using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// Deterministic invalidation information between two recipe revisions.
/// </summary>
public sealed class VisualIncrementalAnalysis
{
    public VisualIncrementalAnalysis(
        IEnumerable<NodeId>? changedNodes = null,
        IEnumerable<NodeId>? affectedNodes = null,
        IEnumerable<NodeId>? removedNodes = null,
        IEnumerable<string>? affectedOutputIds = null,
        IEnumerable<string>? reasons = null)
    {
        ChangedNodes = (changedNodes ?? [])
            .Distinct()
            .OrderBy(id => id)
            .ToImmutableArray();
        AffectedNodes = (affectedNodes ?? [])
            .Distinct()
            .OrderBy(id => id)
            .ToImmutableArray();
        RemovedNodes = (removedNodes ?? [])
            .Distinct()
            .OrderBy(id => id)
            .ToImmutableArray();
        AffectedOutputIds = (affectedOutputIds ?? [])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToImmutableArray();
        Reasons = (reasons ?? [])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reason => reason, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    public ImmutableArray<NodeId> ChangedNodes { get; }

    public ImmutableArray<NodeId> AffectedNodes { get; }

    public ImmutableArray<NodeId> RemovedNodes { get; }

    public ImmutableArray<string> AffectedOutputIds { get; }

    public ImmutableArray<string> AffectedOutputs => AffectedOutputIds;

    public ImmutableArray<string> Reasons { get; }

    public bool IsSemanticChange =>
        ChangedNodes.Length > 0 ||
        RemovedNodes.Length > 0 ||
        AffectedOutputIds.Length > 0 ||
        Reasons.Length > 0;
}
