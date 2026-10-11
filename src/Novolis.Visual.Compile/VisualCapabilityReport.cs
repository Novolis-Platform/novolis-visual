using System.Collections.Immutable;

namespace Novolis.Visual.Compile;

/// <summary>
/// Deterministic capability negotiation output for a compiled recipe.
/// </summary>
public sealed class VisualCapabilityReport
{
    public VisualCapabilityReport(IEnumerable<VisualCapabilityResult>? results = null)
    {
        Results = (results ?? [])
            .Where(result => result is not null)
            .OrderBy(result => result.Id, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    public ImmutableArray<VisualCapabilityResult> Results { get; }

    public ImmutableSortedDictionary<string, VisualCapabilityStatus> Decisions =>
        Results.ToImmutableSortedDictionary(
            result => result.Id,
            result => result.Status,
            StringComparer.Ordinal);

    public bool HasUnsupportedRequired =>
        Results.Any(result =>
            result.Required &&
            result.Status == VisualCapabilityStatus.Unsupported);

    public bool IsExact =>
        Results.All(result => result.Status == VisualCapabilityStatus.Exact);

    public VisualCapabilityStatus GetStatus(string id) =>
        Results.FirstOrDefault(result =>
            string.Equals(result.Id, id, StringComparison.Ordinal))?.Status
        ?? VisualCapabilityStatus.Unsupported;
}
