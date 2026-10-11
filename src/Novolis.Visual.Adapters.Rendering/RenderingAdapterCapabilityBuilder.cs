using Novolis.Visual.Compile;

namespace Novolis.Visual.Adapters.Rendering;

internal sealed class RenderingAdapterCapabilityBuilder
{
    private readonly Dictionary<string, VisualCapabilityResult> _results =
        new(StringComparer.Ordinal);

    public void Add(
        string id,
        VisualCapabilityStatus status,
        string? message = null,
        bool required = true)
    {
        var candidate = new VisualCapabilityResult(
            id,
            status,
            required,
            message: message);

        if (!_results.TryGetValue(id, out var existing) ||
            Rank(status) > Rank(existing.Status))
        {
            _results[id] = candidate;
        }
    }

    public VisualCapabilityReport Build() =>
        new(_results.Values);

    private static int Rank(VisualCapabilityStatus status) => status switch
    {
        VisualCapabilityStatus.Unsupported => 3,
        VisualCapabilityStatus.BakedFallback => 2,
        VisualCapabilityStatus.Approximate => 1,
        _ => 0,
    };
}
