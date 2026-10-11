using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// Stable result of downstream invalidation analysis.
/// </summary>
public sealed record VisualInvalidationResult
{
    public required ImmutableArray<NodeId> ChangedNodes { get; init; }

    public required ImmutableArray<NodeId> AffectedNodes { get; init; }

    public required ImmutableArray<string> AffectedOutputIds { get; init; }
}
