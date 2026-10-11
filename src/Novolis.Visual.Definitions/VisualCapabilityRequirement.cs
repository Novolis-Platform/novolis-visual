namespace Novolis.Visual.Definitions;

/// <summary>
/// A capability that a compiler or runtime target must provide.
/// </summary>
public sealed record VisualCapabilityRequirement
{
    public VisualCapabilityRequirement(
        string id,
        string? minimumVersion = null,
        bool required = true)
    {
        Id = VisualValueValidation.Required(id, nameof(id));
        MinimumVersion = minimumVersion;
        Required = required;
    }

    public string Id { get; }

    public string? MinimumVersion { get; }

    public bool Required { get; }
}
