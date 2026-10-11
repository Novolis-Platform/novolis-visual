namespace Novolis.Visual.Baking;

/// <summary>
/// Deterministic image bake dimensions and seed.
/// </summary>
public sealed record VisualBakeOptions
{
    public int Width { get; init; } = 256;

    public int Height { get; init; } = 256;

    public int Seed { get; init; }

    public VisualBakeTarget Target { get; init; } = VisualBakeTarget.SurfaceColor;

    public void Validate()
    {
        if (Width <= 0 || Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Width),
                "Bake dimensions must be positive.");
        }
    }
}
