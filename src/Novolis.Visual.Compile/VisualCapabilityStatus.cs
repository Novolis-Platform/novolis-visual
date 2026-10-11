namespace Novolis.Visual.Compile;

/// <summary>
/// The truthfulness level negotiated for one target capability.
/// </summary>
public enum VisualCapabilityStatus
{
    Exact,
    Approximate,
    BakedFallback,
    Unsupported,
}
