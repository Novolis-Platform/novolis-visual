using System.Collections.Immutable;
using Novolis.Rendering.Appearance;
using Novolis.Rendering.Materials;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Result of lowering or inspecting a renderer authoring material.
/// </summary>
public sealed class RenderingMaterialResult
{
    internal RenderingMaterialResult(
        IMaterial? material,
        SurfaceSample? evaluatedSurface,
        VisualCapabilityStatus status,
        IEnumerable<VisualRenderingDiagnostic>? diagnostics,
        VisualCapabilityReport capabilities,
        string fingerprint,
        string cacheKey)
    {
        Material = material;
        EvaluatedSurface = evaluatedSurface;
        Status = status;
        Diagnostics = RenderingAdapterDiagnostics.Order(diagnostics);
        Capabilities = capabilities;
        Fingerprint = fingerprint;
        CacheKey = cacheKey;
    }

    /// <summary>
    /// Material that can be passed to <see cref="Novolis.Rendering.Scene.MeshInstance"/>.
    /// This is null for unsupported or bake-required lowering.
    /// </summary>
    public IMaterial? Material { get; }

    /// <summary>
    /// Reference CPU evaluation for appearance-stack lowering, when available.
    /// It is not substituted for a procedural material.
    /// </summary>
    public SurfaceSample? EvaluatedSurface { get; }

    public VisualCapabilityStatus Status { get; }

    public ImmutableArray<VisualRenderingDiagnostic> Diagnostics { get; }

    public VisualCapabilityReport Capabilities { get; }

    public string Fingerprint { get; }

    public string CacheKey { get; }

    public bool IsUsable =>
        Material is not null &&
        Status != VisualCapabilityStatus.Unsupported;

    public bool RequiresBake => Status == VisualCapabilityStatus.BakedFallback;

    internal RenderingMaterialResult WithSourceContext(
        string? sourcePath,
        NodeId? nodeId,
        string? outputId)
    {
        var diagnostics = Diagnostics
            .Select(diagnostic =>
                diagnostic with
                {
                    SourcePath = diagnostic.SourcePath ?? sourcePath,
                    NodeId = diagnostic.NodeId ?? nodeId,
                    OutputId = diagnostic.OutputId ?? outputId,
                });

        return new RenderingMaterialResult(
            Material,
            EvaluatedSurface,
            Status,
            diagnostics,
            Capabilities,
            Fingerprint,
            CacheKey);
    }
}
