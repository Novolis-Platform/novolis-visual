namespace Novolis.Visual.Assets;

/// <summary>
/// Describes deterministic repetition without introducing a second scene
/// hierarchy into the visual asset schema.
/// </summary>
public sealed record AssetGeometryRepeat
{
    public AssetGeometryRepeat(
        int fixedCount = 1,
        string? countParameterId = null,
        int minimumCount = 1,
        int maximumCount = 64,
        AssetGeometryAxis axis = AssetGeometryAxis.X,
        AssetGeometryRepeatPattern pattern = AssetGeometryRepeatPattern.Linear,
        double spacing = 1)
    {
        if (fixedCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedCount));
        }

        if (minimumCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumCount));
        }

        if (maximumCount < minimumCount)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        if (!double.IsFinite(spacing))
        {
            throw new ArgumentOutOfRangeException(nameof(spacing));
        }

        if (countParameterId is not null &&
            string.IsNullOrWhiteSpace(countParameterId))
        {
            throw new ArgumentException(
                "A count parameter identifier must not be empty.",
                nameof(countParameterId));
        }

        FixedCount = fixedCount;
        CountParameterId = countParameterId?.Trim();
        MinimumCount = minimumCount;
        MaximumCount = maximumCount;
        Axis = axis;
        Pattern = pattern;
        Spacing = spacing;
    }

    public int FixedCount { get; }

    public string? CountParameterId { get; }

    public int MinimumCount { get; }

    public int MaximumCount { get; }

    public AssetGeometryAxis Axis { get; }

    public AssetGeometryRepeatPattern Pattern { get; }

    public double Spacing { get; }
}
