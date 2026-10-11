using System.Collections.Immutable;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering;

internal static class RenderingAdapterDiagnostics
{
    public static ImmutableArray<VisualRenderingDiagnostic> Order(
        IEnumerable<VisualRenderingDiagnostic>? diagnostics) =>
        (diagnostics ?? [])
            .Where(diagnostic => diagnostic is not null)
            .OrderBy(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.SourcePath, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.CapabilityId, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.NodeId)
            .ThenBy(diagnostic => diagnostic.OutputId, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToImmutableArray();

    public static VisualRenderingDiagnostic Error(
        string code,
        string message,
        string? sourcePath = null,
        string? capabilityId = null,
        NodeId? nodeId = null,
        string? outputId = null) =>
        new(
            VisualCompileDiagnosticSeverity.Error,
            code,
            message,
            sourcePath,
            capabilityId,
            nodeId,
            outputId: outputId);

    public static VisualRenderingDiagnostic Warning(
        string code,
        string message,
        string? sourcePath = null,
        string? capabilityId = null,
        NodeId? nodeId = null,
        string? outputId = null) =>
        new(
            VisualCompileDiagnosticSeverity.Warning,
            code,
            message,
            sourcePath,
            capabilityId,
            nodeId,
            outputId: outputId);

    public static VisualRenderingDiagnostic Info(
        string code,
        string message,
        string? sourcePath = null,
        string? capabilityId = null,
        NodeId? nodeId = null,
        string? outputId = null) =>
        new(
            VisualCompileDiagnosticSeverity.Info,
            code,
            message,
            sourcePath,
            capabilityId,
            nodeId,
            outputId: outputId);
}
