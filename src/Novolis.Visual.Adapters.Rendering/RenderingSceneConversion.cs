using System.Collections.Immutable;
using Novolis.Rendering.Scene;
using Novolis.Visual.Compile;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Result of assembling converted mesh instances into one rendering scene.
/// </summary>
public sealed class RenderingSceneConversion
{
    internal RenderingSceneConversion(
        Scene? scene,
        IEnumerable<RenderingMeshConversion> meshes,
        VisualCapabilityStatus status,
        IEnumerable<VisualRenderingDiagnostic>? diagnostics,
        VisualCapabilityReport capabilities,
        string fingerprint,
        string cacheKey)
    {
        Scene = scene;
        Meshes = meshes.ToImmutableArray();
        Status = status;
        Diagnostics = RenderingAdapterDiagnostics.Order(diagnostics);
        Capabilities = capabilities;
        Fingerprint = fingerprint;
        CacheKey = cacheKey;
    }

    public Scene? Scene { get; }

    public ImmutableArray<RenderingMeshConversion> Meshes { get; }

    public VisualCapabilityStatus Status { get; }

    public ImmutableArray<VisualRenderingDiagnostic> Diagnostics { get; }

    public VisualCapabilityReport Capabilities { get; }

    public string Fingerprint { get; }

    public string CacheKey { get; }

    public bool IsUsable =>
        Scene is not null &&
        Status != VisualCapabilityStatus.Unsupported;
}
