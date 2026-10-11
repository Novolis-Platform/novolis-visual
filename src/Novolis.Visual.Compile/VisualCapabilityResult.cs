namespace Novolis.Visual.Compile;

/// <summary>
/// The negotiated result for one recipe capability.
/// </summary>
public sealed record VisualCapabilityResult
{
    public VisualCapabilityResult(
        string id,
        VisualCapabilityStatus status,
        bool required,
        string? minimumVersion = null,
        string? message = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Status = status;
        Required = required;
        MinimumVersion = minimumVersion;
        Message = message;
    }

    public string Id { get; }

    public VisualCapabilityStatus Status { get; }

    public bool Required { get; }

    public string? MinimumVersion { get; }

    public string? Message { get; }

    public bool IsUsable =>
        Status != VisualCapabilityStatus.Unsupported ||
        !Required;
}
