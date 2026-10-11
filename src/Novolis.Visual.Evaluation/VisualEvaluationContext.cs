using System.Numerics;

namespace Novolis.Visual.Evaluation;

/// <summary>
/// Host-neutral inputs for one deterministic CPU sample.
/// </summary>
public sealed record VisualEvaluationContext
{
    public Vector3 Position { get; init; }

    public Vector3 Normal { get; init; } = Vector3.UnitY;

    public Vector2 Uv { get; init; }

    public double Time { get; init; }

    public double Age { get; init; }

    public Vector3 Velocity { get; init; }

    public double Distance { get; init; }

    public Vector3 ViewDirection { get; init; } = -Vector3.UnitZ;

    public double Seed { get; init; }

    public static VisualEvaluationContext Default => new();
}
