#if false
using System.Collections.Immutable;

namespace Novolis.Visual.Compile;

/// <summary>
/// Canonically ordered VisualIR produced from a recipe.
/// </summary>
public sealed record VisualIrRecipe
{
    public required string RecipeId { get; init; }

    public required ImmutableArray<VisualIrNode> Nodes { get; init; }

    public required ImmutableArray<VisualIrOutput> Outputs { get; init; }

    public required ImmutableSortedDictionary<
        string,
        VisualSourceMapEntry> SourceMap { get; init; }

    public required string Fingerprint { get; init; }
}
#endif
