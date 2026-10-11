namespace Novolis.Visual.CodeGen;

/// <summary>
/// Generated source and its deterministic manifest metadata.
/// </summary>
public sealed record GeneratedVisualSource
{
    public required string Source { get; init; }

    public required string RecipeFingerprint { get; init; }

    public required VisualCodeGenerationMode Mode { get; init; }

    public required string TypeName { get; init; }

    public required string SourceSha256 { get; init; }
}
