using System.Collections.Immutable;
using Novolis.Rendering.Runtime;
using Novolis.Visual.Compile;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Result of handing an adapted authoring scene to the existing rendering
/// compiler.
/// </summary>
public sealed class RenderingCompiledSceneResult
{
    internal RenderingCompiledSceneResult(
        CompiledScene? scene,
        VisualCapabilityStatus status,
        IEnumerable<VisualRenderingDiagnostic>? diagnostics,
        VisualCapabilityReport capabilities,
        string fingerprint,
        string cacheKey)
    {
        Scene = scene;
        Status = status;
        Diagnostics = RenderingAdapterDiagnostics.Order(diagnostics);
        Capabilities = capabilities;
        Fingerprint = fingerprint;
        CacheKey = cacheKey;
    }

    public CompiledScene? Scene { get; }

    public VisualCapabilityStatus Status { get; }

    public ImmutableArray<VisualRenderingDiagnostic> Diagnostics { get; }

    public VisualCapabilityReport Capabilities { get; }

    public string Fingerprint { get; }

    public string CacheKey { get; }

    public bool IsUsable =>
        Scene is not null &&
        Status != VisualCapabilityStatus.Unsupported;
}
