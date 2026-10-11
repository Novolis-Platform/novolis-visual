using System.Collections.Immutable;

namespace Novolis.Visual.Compile;

/// <summary>
/// Immutable target capability declarations used during compilation.
/// </summary>
public sealed class VisualTargetCapabilities
{
    private readonly ImmutableSortedDictionary<string, VisualCapabilitySupport> supports;

    public VisualTargetCapabilities(
        IEnumerable<VisualCapabilitySupport>? supports = null,
        bool assumeExactForUnlisted = true)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<
            string,
            VisualCapabilitySupport>(StringComparer.Ordinal);

        foreach (var support in supports ?? [])
        {
            ArgumentNullException.ThrowIfNull(support);
            builder[support.Id] = support;
        }

        this.supports = builder.ToImmutable();
        AssumeExactForUnlisted = assumeExactForUnlisted;
    }

    public bool AssumeExactForUnlisted { get; }

    public ImmutableArray<VisualCapabilitySupport> Supports =>
        supports.Values.ToImmutableArray();

    public ImmutableHashSet<string> Exact =>
        supports.Values
            .Where(support => support.Status == VisualCapabilityStatus.Exact)
            .Select(support => support.Id)
            .ToImmutableHashSet(StringComparer.Ordinal);

    public ImmutableHashSet<string> Approximate =>
        supports.Values
            .Where(support => support.Status == VisualCapabilityStatus.Approximate)
            .Select(support => support.Id)
            .ToImmutableHashSet(StringComparer.Ordinal);

    public ImmutableHashSet<string> BakedFallback =>
        supports.Values
            .Where(support => support.Status == VisualCapabilityStatus.BakedFallback)
            .Select(support => support.Id)
            .ToImmutableHashSet(StringComparer.Ordinal);

    public ImmutableHashSet<string> Unsupported =>
        supports.Values
            .Where(support => support.Status == VisualCapabilityStatus.Unsupported)
            .Select(support => support.Id)
            .ToImmutableHashSet(StringComparer.Ordinal);

    public VisualCapabilitySupport GetSupport(
        string id,
        string? minimumVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!supports.TryGetValue(id, out var support))
        {
            return new VisualCapabilitySupport(
                id,
                AssumeExactForUnlisted
                    ? VisualCapabilityStatus.Exact
                    : VisualCapabilityStatus.Unsupported,
                message: AssumeExactForUnlisted
                    ? "The target declares open support for unlisted capabilities."
                    : "The target did not declare this capability.");
        }

        if (minimumVersion is null ||
            support.Version is null ||
            !Version.TryParse(minimumVersion, out var required) ||
            !Version.TryParse(support.Version, out var available) ||
            available >= required)
        {
            return support;
        }

        return new VisualCapabilitySupport(
            id,
            VisualCapabilityStatus.Unsupported,
            support.Version,
            $"Target capability version {support.Version} is older than required version {minimumVersion}.");
    }

    public static VisualTargetCapabilities Open() => new();

    public static VisualTargetCapabilities Empty() =>
        new(assumeExactForUnlisted: false);

    public static VisualTargetCapabilities Cpu => Open();
}
