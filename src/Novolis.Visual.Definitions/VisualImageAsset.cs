using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Immutable decoded RGBA image payload suitable for deterministic baking or
/// embedded code-only output.
/// </summary>
public sealed record VisualImageAsset
{
    public VisualImageAsset(
        string assetId,
        int width,
        int height,
        VisualImageSourceFormat sourceFormat,
        ColorSpace colorSpace,
        AlphaMode alphaMode,
        ReadOnlyMemory<byte> rgbaPixels,
        IEnumerable<VisualSpriteFrame>? frames = null,
        string? sourceSha256 = null)
    {
        AssetId = VisualValueValidation.Required(assetId, nameof(assetId));
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (colorSpace == ColorSpace.None)
        {
            throw new ArgumentException(
                "An image requires an explicit color space.",
                nameof(colorSpace));
        }

        var expectedBytes = checked(width * height * 4);
        if (rgbaPixels.Length != expectedBytes)
        {
            throw new ArgumentException(
                $"RGBA payload must contain exactly {expectedBytes} bytes.",
                nameof(rgbaPixels));
        }

        var frameArray = (frames ?? [new VisualSpriteFrame(
                "default",
                0,
                0,
                width,
                height)])
            .ToImmutableArray();
        if (frameArray.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                "At least one image frame is required.",
                nameof(frames));
        }

        if (frameArray.Select(frame => frame.Name)
            .Distinct(StringComparer.Ordinal)
            .Count() != frameArray.Length)
        {
            throw new ArgumentException(
                "Image frame names must be unique.",
                nameof(frames));
        }

        foreach (var frame in frameArray)
        {
            ArgumentNullException.ThrowIfNull(frame);
            if (frame.X + frame.Width > width ||
                frame.Y + frame.Height > height)
            {
                throw new ArgumentException(
                    $"Frame '{frame.Name}' lies outside the image bounds.",
                    nameof(frames));
            }
        }

        var pixels = rgbaPixels.ToArray();
        var calculatedHash = Convert.ToHexString(
                SHA256.HashData(pixels))
            .ToLowerInvariant();
        var stableSourceHash = string.IsNullOrWhiteSpace(sourceSha256)
            ? calculatedHash
            : sourceSha256.Trim().ToLowerInvariant();
        if (stableSourceHash.Length != 64 ||
            stableSourceHash.Any(character =>
                !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "The supplied image hash must be a SHA-256 hexadecimal value.",
                nameof(sourceSha256));
        }

        Width = width;
        Height = height;
        SourceFormat = sourceFormat;
        ColorSpace = colorSpace;
        AlphaMode = alphaMode;
        RgbaPixels = ImmutableArray.CreateRange(pixels);
        Frames = frameArray;
        PixelSha256 = calculatedHash;
        SourceSha256 = stableSourceHash;
    }

    public string AssetId { get; }

    public int Width { get; }

    public int Height { get; }

    public VisualImageSourceFormat SourceFormat { get; }

    public ColorSpace ColorSpace { get; }

    public AlphaMode AlphaMode { get; }

    public ImmutableArray<byte> RgbaPixels { get; }

    public ImmutableArray<VisualSpriteFrame> Frames { get; }

    public string PixelSha256 { get; }

    public string SourceSha256 { get; }
}
