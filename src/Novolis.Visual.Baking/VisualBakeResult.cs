namespace Novolis.Visual.Baking;

/// <summary>
/// Image and manifest returned by a deterministic bake.
/// </summary>
public sealed record VisualBakeResult
{
    public required VisualBakeImage Image { get; init; }

    public required VisualBakeManifest Manifest { get; init; }

    public required byte[] PngBytes { get; init; }
}
