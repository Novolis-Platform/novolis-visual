using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Baking;

/// <summary>
/// Packs decoded image frames using a deterministic, non-rotating shelf layout.
/// </summary>
public sealed class VisualSpriteAtlasPacker
{
    public VisualSpriteAtlasBakeResult Bake(
        IEnumerable<VisualImageAsset> assets,
        VisualSpriteAtlasOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(assets);
        options ??= new VisualSpriteAtlasOptions();
        options.Validate();

        var sources = assets
            .SelectMany(
                asset =>
                {
                    ArgumentNullException.ThrowIfNull(asset);
                    return asset.Frames.Select(
                        frame => new SourceFrame(asset, frame));
                })
            .OrderBy(source => source.Asset.AssetId, StringComparer.Ordinal)
            .ThenBy(source => source.Frame.Name, StringComparer.Ordinal)
            .ToArray();
        if (sources.Length == 0)
        {
            throw new ArgumentException(
                "At least one image frame is required.",
                nameof(assets));
        }

        var placements = new List<Placement>(sources.Length);
        var x = options.Padding;
        var y = options.Padding;
        var rowHeight = 0;
        var usedWidth = options.Padding;
        foreach (var source in sources)
        {
            if (source.Frame.Width + options.Padding * 2 > options.MaxWidth)
            {
                throw new InvalidOperationException(
                    $"Frame '{source.Frame.Name}' is wider than the atlas limit.");
            }

            if (x + source.Frame.Width + options.Padding > options.MaxWidth)
            {
                x = options.Padding;
                y = checked(y + rowHeight + options.Padding);
                rowHeight = 0;
            }

            placements.Add(new Placement(source, x, y));
            x = checked(x + source.Frame.Width + options.Padding);
            rowHeight = Math.Max(rowHeight, source.Frame.Height);
            usedWidth = Math.Max(usedWidth, x);
        }

        var width = Math.Max(1, Math.Min(options.MaxWidth, usedWidth));
        var height = checked(y + rowHeight + options.Padding);
        var pixels = new VisualRgba32[checked(width * height)];
        var frameManifest = ImmutableArray.CreateBuilder<VisualSpriteAtlasFrame>(
            placements.Count);
        foreach (var placement in placements)
        {
            CopyFrame(placement, pixels, width);
            frameManifest.Add(
                new VisualSpriteAtlasFrame
                {
                    AssetId = placement.Source.Asset.AssetId,
                    SourceFrame = placement.Source.Frame.Name,
                    X = placement.X,
                    Y = placement.Y,
                    Width = placement.Source.Frame.Width,
                    Height = placement.Source.Frame.Height,
                    PivotX = placement.Source.Frame.PivotX,
                    PivotY = placement.Source.Frame.PivotY,
                    DurationSeconds = placement.Source.Frame.DurationSeconds,
                });
        }

        return new VisualSpriteAtlasBakeResult
        {
            Image = new VisualBakeImage
            {
                Width = width,
                Height = height,
                Pixels = pixels.ToImmutableArray(),
            },
            Frames = frameManifest.ToImmutable(),
        };
    }

    private static void CopyFrame(
        Placement placement,
        Span<VisualRgba32> destination,
        int destinationWidth)
    {
        var source = placement.Source;
        for (var row = 0; row < source.Frame.Height; row++)
        {
            var sourceOffset = checked(
                ((source.Frame.Y + row) * source.Asset.Width + source.Frame.X) * 4);
            var destinationOffset = checked(
                (placement.Y + row) * destinationWidth + placement.X);
            for (var column = 0; column < source.Frame.Width; column++)
            {
                var pixelOffset = sourceOffset + column * 4;
                destination[destinationOffset + column] = new VisualRgba32(
                    source.Asset.RgbaPixels[pixelOffset],
                    source.Asset.RgbaPixels[pixelOffset + 1],
                    source.Asset.RgbaPixels[pixelOffset + 2],
                    source.Asset.RgbaPixels[pixelOffset + 3]);
            }
        }
    }

    private sealed record SourceFrame(
        VisualImageAsset Asset,
        VisualSpriteFrame Frame);

    private sealed record Placement(
        SourceFrame Source,
        int X,
        int Y);
}
