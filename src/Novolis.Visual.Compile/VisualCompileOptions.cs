using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// Deterministic options shared by binding, validation, normalization, and
/// capability negotiation.
/// </summary>
public sealed class VisualCompileOptions
{
    public VisualCompileOptions(
        VisualNodeRegistry? registry = null,
        VisualTargetCapabilities? targetCapabilities = null,
        IEnumerable<string>? selectedOutputIds = null,
        bool optimize = true,
        bool strict = false,
        bool eliminateDeadNodes = true)
    {
        Registry = registry ?? VisualCompiler.CreateDefaultRegistry();
        TargetCapabilities = targetCapabilities ?? VisualTargetCapabilities.Open();
        SelectedOutputIds = (selectedOutputIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToImmutableHashSet(StringComparer.Ordinal);
        Optimize = optimize;
        Strict = strict;
        EliminateDeadNodes = eliminateDeadNodes;
    }

    public VisualNodeRegistry Registry { get; }

    public VisualTargetCapabilities TargetCapabilities { get; }

    public ImmutableHashSet<string> SelectedOutputIds { get; }

    public bool Optimize { get; }

    public bool Strict { get; }

    public bool EliminateDeadNodes { get; }

    public static VisualCompileOptions Default => new();
}
