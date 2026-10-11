using System.Collections.Immutable;

namespace Novolis.Visual.Baking;

/// <summary>
/// Deterministic atlas image and frame manifest.
/// </summary>
public sealed record VisualSpriteAtlasBakeResult
{
    public required VisualBakeImage Image { get; init; }

    public required ImmutableArray<VisualSpriteAtlasFrame> Frames { get; init; }

    public byte[] PngBytes => Image.ToPng();
}
