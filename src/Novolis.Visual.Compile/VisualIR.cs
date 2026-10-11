using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// Deterministic, normalized representation of a visual recipe.
/// </summary>
public sealed class VisualIR
{
    public VisualIR(
        string recipeId,
        int schemaVersion,
        VisualSeedPolicy seedPolicy,
        IEnumerable<VisualIRNode>? nodes = null,
        IEnumerable<VisualIROutput>? outputs = null,
        IEnumerable<VisualDiagnostic>? diagnostics = null,
        VisualSourceMap? sourceMap = null,
        VisualCapabilityReport? capabilities = null,
        string? semanticFingerprint = null)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            throw new ArgumentException("A recipe identifier is required.", nameof(recipeId));
        }

        ArgumentNullException.ThrowIfNull(seedPolicy);
        if (schemaVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        RecipeId = recipeId;
        SchemaVersion = schemaVersion;
        SeedPolicy = seedPolicy;
        Nodes = (nodes ?? [])
            .Where(node => node is not null)
            .ToImmutableArray();
        Outputs = (outputs ?? [])
            .Where(output => output is not null)
            .OrderBy(output => output.Id, StringComparer.Ordinal)
            .ToImmutableArray();
        Diagnostics = (diagnostics ?? [])
            .Where(diagnostic => diagnostic is not null)
            .OrderBy(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.NodeId?.ToString(), StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.ConnectionId?.ToString(), StringComparer.Ordinal)
            .ToImmutableArray();
        SourceMap = sourceMap ?? new VisualSourceMap();
        Capabilities = capabilities ?? new VisualCapabilityReport();
        SemanticFingerprint = semanticFingerprint ??
                             VisualIRFingerprint.Compute(this);
    }

    public string RecipeId { get; }

    public int SchemaVersion { get; }

    public VisualSeedPolicy SeedPolicy { get; }

    public ImmutableArray<VisualIRNode> Nodes { get; }

    public ImmutableArray<VisualIROutput> Outputs { get; }

    public ImmutableArray<VisualDiagnostic> Diagnostics { get; }

    public VisualSourceMap SourceMap { get; }

    public VisualCapabilityReport Capabilities { get; }

    public string SemanticFingerprint { get; }

    public string Fingerprint => SemanticFingerprint;

    public VisualIR IR => this;

    public bool IsValid =>
        Diagnostics.All(diagnostic =>
            diagnostic.Severity != VisualDiagnosticSeverity.Error);

    public bool Success =>
        IsValid &&
        !Capabilities.HasUnsupportedRequired;

    public bool TryGetNode(NodeId id, out VisualIRNode? node)
    {
        node = Nodes.FirstOrDefault(candidate => candidate.Id == id);
        return node is not null;
    }
}
