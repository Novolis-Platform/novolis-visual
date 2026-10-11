using Novolis.Modeling;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Scene;
using Novolis.Visual.Compile;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Bridges an already evaluated modeling mesh to the current rendering scene
/// mesh contract. Geometry generation and omitted modifiers remain owned by
/// the renderer-neutral modeling scene evaluator.
/// </summary>
public static class RenderingMeshAdapter
{
    /// <summary>
    /// Measures geometry and appearance readiness from the actual mesh arrays.
    /// </summary>
    public static RenderingMeshReadiness InspectReadiness(
        EvaluatedMesh mesh,
        MeshAttributeFlags requiredAppearanceAttributes =
            MeshAttributeSet.AppearanceDefaults)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var available = MeshAttributeSet.From(mesh);
        var readiness = MeshReadiness.From(
            available,
            requiredAppearanceAttributes);
        return new RenderingMeshReadiness(
            available,
            readiness,
            requiredAppearanceAttributes);
    }

    /// <summary>
    /// Converts positions, indices, and the evaluated world transform. The
    /// current <see cref="MeshInstance"/> contract does not carry optional
    /// modeling attributes, so their readiness is reported separately.
    /// </summary>
    public static RenderingMeshConversion Convert(
        EvaluatedMesh mesh,
        IMaterial material,
        RenderingAdapterOptions? options = null,
        string? sourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(material);
        var effectiveOptions = options ?? RenderingAdapterOptions.Default;
        var readiness = InspectReadiness(
            mesh,
            effectiveOptions.RequiredAppearanceAttributes);
        var fingerprint = RenderingAdapterFingerprint.ForMesh(mesh, material);
        var capabilities = new RenderingAdapterCapabilityBuilder();
        var diagnostics = new List<VisualRenderingDiagnostic>();

        capabilities.Add(
            RenderingAdapterContract.MeshInstanceCapability,
            readiness.IsGeometryReady
                ? VisualCapabilityStatus.Exact
                : VisualCapabilityStatus.Unsupported);

        AddReadinessDiagnostics(mesh, readiness, sourcePath, diagnostics);
        if (!readiness.IsGeometryReady)
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA301",
                    $"Evaluated mesh is missing geometry attributes: {readiness.MissingGeometryAttributes}.",
                    sourcePath,
                    RenderingAdapterContract.MeshInstanceCapability));
            return CreateResult(
                null,
                readiness,
                VisualCapabilityStatus.Unsupported,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        var materialInspection = RenderingMaterialAdapter.Inspect(
            material,
            sourcePath);
        foreach (var diagnostic in materialInspection.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        if (!materialInspection.IsUsable)
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA302",
                    "The mesh cannot be attached to an unsupported rendering material.",
                    sourcePath,
                    "rendering.material.authoring.v1"));
            capabilities.Add(
                RenderingAdapterContract.MeshInstanceCapability,
                VisualCapabilityStatus.Unsupported,
                "The supplied material is not supported by the rendering compiler.");
            return CreateResult(
                null,
                readiness,
                VisualCapabilityStatus.Unsupported,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        var converted = new MeshInstance(
            mesh.Vertices.ToArray(),
            mesh.Indices.ToArray(),
            material,
            mesh.World);

        return CreateResult(
            converted,
            readiness,
            VisualCapabilityStatus.Exact,
            diagnostics,
            capabilities,
            fingerprint,
            sourcePath);
    }

    /// <summary>
    /// Strict convenience form for callers that already enforce the mesh
    /// contract.
    /// </summary>
    public static MeshInstance ToMeshInstance(
        EvaluatedMesh mesh,
        IMaterial material,
        RenderingAdapterOptions? options = null,
        string? sourcePath = null)
    {
        var result = Convert(mesh, material, options, sourcePath);
        return result.Mesh
            ?? throw new InvalidOperationException(
                string.Join(
                    Environment.NewLine,
                    result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    private static RenderingMeshConversion CreateResult(
        MeshInstance? converted,
        RenderingMeshReadiness readiness,
        VisualCapabilityStatus status,
        IEnumerable<VisualRenderingDiagnostic> diagnostics,
        RenderingAdapterCapabilityBuilder capabilities,
        string fingerprint,
        string? sourcePath) =>
        new(
            converted,
            readiness,
            status,
            diagnostics,
            capabilities.Build(),
            fingerprint,
            RenderingAdapterFingerprint.CreateCacheKey(
                "mesh-instance",
                fingerprint,
                status.ToString(),
                readiness.AvailableAttributes.Flags.ToString(),
                sourcePath));

    private static void AddReadinessDiagnostics(
        EvaluatedMesh mesh,
        RenderingMeshReadiness readiness,
        string? sourcePath,
        ICollection<VisualRenderingDiagnostic> diagnostics)
    {
        if (!readiness.IsAppearanceReady)
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Warning(
                    "VRA303",
                    $"Appearance attributes are not ready: {readiness.MissingAppearanceAttributes}.",
                    sourcePath,
                    "rendering.mesh-appearance-attributes.v1"));
        }

        var claimed = mesh.Readiness;
        if (claimed.Geometry != readiness.Geometry ||
            claimed.Appearance != readiness.Appearance ||
            claimed.MissingGeometryAttributes != readiness.MissingGeometryAttributes ||
            claimed.MissingAppearanceAttributes != readiness.MissingAppearanceAttributes)
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Warning(
                    "VRA304",
                    "Mesh readiness metadata did not match the attributes present in the evaluated mesh; the adapter used the measured contract.",
                    sourcePath,
                    RenderingAdapterContract.MeshInstanceCapability));
        }

        if (readiness.AvailableAttributes.HasAny(MeshAttributeFlags.Normals |
                                                MeshAttributeFlags.Tangents |
                                                MeshAttributeFlags.UV0 |
                                                MeshAttributeFlags.Curvature |
                                                MeshAttributeFlags.Cavity |
                                                MeshAttributeFlags.MaterialSlots))
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Info(
                    "VRA305",
                    "Optional modeling attributes remain available on the source mesh but are not carried by the current MeshInstance contract.",
                    sourcePath,
                    RenderingAdapterContract.MeshInstanceCapability));
        }
    }
}
