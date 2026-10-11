namespace Novolis.Visual.Baking;

/// <summary>
/// Deterministic shelf-packing options for sprite atlases.
/// </summary>
public sealed record VisualSpriteAtlasOptions
{
    public int MaxWidth { get; init; } = 4096;

    public int Padding { get; init; } = 1;

    public void Validate()
    {
        if (MaxWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxWidth));
        }

        if (Padding < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Padding));
        }
    }
}
