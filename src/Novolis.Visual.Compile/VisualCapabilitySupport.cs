namespace Novolis.Visual.Compile;

/// <summary>
/// A target's declared support level for one capability.
/// </summary>
public sealed record VisualCapabilitySupport
{
    public VisualCapabilitySupport(
        string id,
        VisualCapabilityStatus status = VisualCapabilityStatus.Exact,
        string? version = null,
        string? message = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A capability identifier is required.", nameof(id));
        }

        if (status == VisualCapabilityStatus.Unsupported &&
            string.IsNullOrWhiteSpace(message))
        {
            message = "The target does not support this capability.";
        }

        Id = id;
        Status = status;
        Version = version;
        Message = message;
    }

    public string Id { get; }

    public VisualCapabilityStatus Status { get; }

    public string? Version { get; }

    public string? Message { get; }
}
