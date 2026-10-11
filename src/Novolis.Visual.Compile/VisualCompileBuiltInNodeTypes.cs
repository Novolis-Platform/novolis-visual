namespace Novolis.Visual.Compile;

/// <summary>
/// Compatibility identifiers for compiler-owned scalar and vector operations.
/// The schema package remains the authority for persisted recipe contracts.
/// </summary>
public static class VisualCompileBuiltInNodeTypes
{
    public const string ConstantVector = "novolis.visual.constant.vector";

    public const string MultiplyScalar = "novolis.visual.math.multiply";

    public const string NoiseScalar = "novolis.visual.math.noise";

    public const string Noise = NoiseScalar;
}
