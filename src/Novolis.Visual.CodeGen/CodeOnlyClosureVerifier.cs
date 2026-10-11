using System.Collections.Immutable;

namespace Novolis.Visual.CodeGen;

/// <summary>
/// Rejects loose-file, graph-interpreter, authoring, and compiler dependencies.
/// </summary>
public static class CodeOnlyClosureVerifier
{
    private static readonly string[] ForbiddenFragments =
    [
        "AssetStudio",
        "Novolis.Visual.Compile",
        "Microsoft.CodeAnalysis",
        "Roslyn",
        "WorkflowEngine",
        "File.ReadAllBytes",
        "File.ReadAllText",
        "JsonDocument",
        "source path",
    ];

    public static CodeOnlyClosureReport Verify(
        IEnumerable<GeneratedVisualSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var violations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            ArgumentNullException.ThrowIfNull(source);
            foreach (var fragment in ForbiddenFragments)
            {
                if (source.Source.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add($"{source.TypeName}: {fragment}");
                }
            }
        }

        return new CodeOnlyClosureReport
        {
            IsClosed = violations.Count == 0,
            Violations = violations
                .Order(StringComparer.Ordinal)
                .ToImmutableArray(),
        };
    }
}
