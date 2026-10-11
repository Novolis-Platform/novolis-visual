namespace Novolis.Visual.Definitions;

/// <summary>
/// A reference to an image asset and its stable image metadata.
/// </summary>
public sealed record ImageValue : VisualValue
{
    public ImageValue(
        string assetId,
        int width,
        int height,
        ImageFormat format,
        ColorSpace colorSpace,
        AlphaMode alphaMode)
    {
        AssetId = VisualValueValidation.Required(assetId, nameof(assetId));

        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (colorSpace == ColorSpace.None)
        {
            throw new ArgumentException(
                "An image requires an explicit color space.",
                nameof(colorSpace));
        }

        Width = width;
        Height = height;
        Format = format;
        ColorSpace = colorSpace;
        AlphaMode = alphaMode;
    }

    public string AssetId { get; }

    public int Width { get; }

    public int Height { get; }

    public ImageFormat Format { get; }

    public ColorSpace ColorSpace { get; }

    public AlphaMode AlphaMode { get; }

    public override VisualValueKind Kind => VisualValueKind.Image;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Pixels,
        CoordinateSpace.None,
        ColorSpace);
}
