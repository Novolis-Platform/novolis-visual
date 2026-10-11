using System.Collections.Immutable;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Stable metadata for one sprite frame inside an image or atlas.
/// </summary>
public sealed record VisualSpriteFrame
{
    public VisualSpriteFrame(
        string name,
        int x,
        int y,
        int width,
        int height,
        double pivotX = 0.5,
        double pivotY = 0.5,
        double durationSeconds = 0,
        IEnumerable<string>? tags = null)
    {
        Name = VisualValueValidation.Required(name, nameof(name));
        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (!double.IsFinite(pivotX) || pivotX is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pivotX));
        }

        if (!double.IsFinite(pivotY) || pivotY is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pivotY));
        }

        if (!double.IsFinite(durationSeconds) || durationSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
        PivotX = pivotX;
        PivotY = pivotY;
        DurationSeconds = durationSeconds;
        Tags = (tags ?? [])
            .Select(tag => VisualValueValidation.Required(tag, nameof(tags)))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
    }

    public string Name { get; }

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    public double PivotX { get; }

    public double PivotY { get; }

    public double DurationSeconds { get; }

    public ImmutableArray<string> Tags { get; }
}
