namespace Novolis.Visual.Definitions;

/// <summary>
/// A stable fingerprint for an external input used by a recipe.
/// </summary>
public sealed record VisualDependencyFingerprint
{
    public VisualDependencyFingerprint(
        string id,
        string algorithm,
        string fingerprint)
    {
        Id = VisualValueValidation.Required(id, nameof(id));
        Algorithm = VisualValueValidation.Required(algorithm, nameof(algorithm));
        Fingerprint = VisualValueValidation.Required(fingerprint, nameof(fingerprint));
    }

    public string Id { get; }

    public string Algorithm { get; }

    public string Fingerprint { get; }
}
