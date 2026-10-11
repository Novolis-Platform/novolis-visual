using System.Collections.Immutable;
using Novolis.Rendering.Scene;
using Novolis.Visual.Compile;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Result of converting an evaluated modeling mesh to a rendering mesh
/// instance.
/// </summary>
public sealed class RenderingMeshConversion
{
    internal RenderingMeshConversion(
        MeshInstance? mesh,
        RenderingMeshReadiness readiness,
        VisualCapabilityStatus status,
        IEnumerable<VisualRenderingDiagnostic>? diagnostics,
        VisualCapabilityReport capabilities,
        string fingerprint,
        string cacheKey)
    {
        Mesh = mesh;
        Readiness = readiness;
        Status = status;
        Diagnostics = RenderingAdapterDiagnostics.Order(diagnostics);
        Capabilities = capabilities;
        Fingerprint = fingerprint;
        CacheKey = cacheKey;
    }

    public MeshInstance? Mesh { get; }

    public RenderingMeshReadiness Readiness { get; }

    public VisualCapabilityStatus Status { get; }

    public ImmutableArray<VisualRenderingDiagnostic> Diagnostics { get; }

    public VisualCapabilityReport Capabilities { get; }

    public string Fingerprint { get; }

    public string CacheKey { get; }

    public bool IsUsable =>
        Mesh is not null &&
        Status != VisualCapabilityStatus.Unsupported;
}
