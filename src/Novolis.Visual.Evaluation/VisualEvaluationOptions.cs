namespace Novolis.Visual.Evaluation;

/// <summary>
/// Controls deterministic reference evaluation output.
/// </summary>
public sealed record VisualEvaluationOptions
{
    public bool IncludeTrace { get; init; } = true;

    public bool EvaluateWhenCompilationHasErrors { get; init; }

    public static VisualEvaluationOptions Default => new();
}
