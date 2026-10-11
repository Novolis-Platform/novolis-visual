using System.Collections.Immutable;

namespace Novolis.Visual.CodeGen;

/// <summary>
/// Result of checking generated source for runtime authoring dependencies.
/// </summary>
public sealed record CodeOnlyClosureReport
{
    public required bool IsClosed { get; init; }

    public required ImmutableArray<string> Violations { get; init; }
}
