namespace Novolis.Visual.Baking;

/// <summary>
/// Atlas coordinates and source animation metadata for one frame.
/// </summary>
public sealed record VisualSpriteAtlasFrame
{
    public required string AssetId { get; init; }

    public required string SourceFrame { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required double PivotX { get; init; }

    public required double PivotY { get; init; }

    public required double DurationSeconds { get; init; }
}
