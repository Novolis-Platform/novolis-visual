namespace Novolis.Visual.Definitions;

/// <summary>
/// Declares how seeded procedural nodes obtain deterministic randomness.
/// </summary>
public sealed record VisualSeedPolicy
{
    public VisualSeedPolicy(
        VisualSeedMode mode = VisualSeedMode.None,
        ulong seed = 0,
        string? scope = null)
    {
        if (mode == VisualSeedMode.Fixed && string.IsNullOrWhiteSpace(scope))
        {
            scope = "recipe";
        }

        Mode = mode;
        Seed = seed;
        Scope = scope;
    }

    public VisualSeedMode Mode { get; }

    public ulong Seed { get; }

    public string? Scope { get; }

    public bool IsDeterministic =>
        Mode is VisualSeedMode.None or VisualSeedMode.Fixed or VisualSeedMode.Recipe;
}
