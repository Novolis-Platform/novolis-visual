using System.Collections.Immutable;

namespace Novolis.Visual.Baking;

/// <summary>
/// Build metadata for a baked artifact.
/// </summary>
public sealed record VisualBakeManifest
{
    public required string RecipeFingerprint { get; init; }

    public required VisualBakeTarget Target { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required string ContentSha256 { get; init; }

    public required ImmutableSortedDictionary<string, string> Metadata { get; init; }
}
