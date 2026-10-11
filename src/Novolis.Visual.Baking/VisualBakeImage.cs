using System.Collections.Immutable;

namespace Novolis.Visual.Baking;

/// <summary>
/// Immutable CPU image produced by a visual bake.
/// </summary>
public sealed record VisualBakeImage
{
    public required int Width { get; init; }

    public required int Height { get; init; }

    public required ImmutableArray<VisualRgba32> Pixels { get; init; }

    public VisualRgba32 this[int x, int y] => Pixels[y * Width + x];

    public byte[] ToPng() => PngEncoder.Encode(this);
}
