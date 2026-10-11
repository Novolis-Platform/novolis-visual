namespace Novolis.Visual.Definitions;

/// <summary>
/// Frame timing information for time-varying visual fields.
/// </summary>
public sealed record FrameValue : VisualValue
{
    public FrameValue(
        long frameIndex,
        double timeSeconds,
        double deltaSeconds,
        double framesPerSecond)
    {
        if (framesPerSecond < 0 || !double.IsFinite(framesPerSecond))
        {
            throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
        }

        FrameIndex = frameIndex;
        TimeSeconds = VisualValueValidation.Finite(timeSeconds, nameof(timeSeconds));
        DeltaSeconds = VisualValueValidation.Finite(deltaSeconds, nameof(deltaSeconds));
        FramesPerSecond = framesPerSecond;
    }

    public long FrameIndex { get; }

    public double TimeSeconds { get; }

    public double DeltaSeconds { get; }

    public double FramesPerSecond { get; }

    public override VisualValueKind Kind => VisualValueKind.Frame;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Frames,
        CoordinateSpace.None,
        ColorSpace.None,
        FieldFrequency.Frame);
}
