namespace Novolis.Visual.Definitions;

/// <summary>
/// Purity and reproducibility guarantees for a field.
/// </summary>
public enum FieldPurity
{
    Pure,
    DeterministicWithSeed,
    ContextDependent,
    Impure,
}
