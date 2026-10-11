using Novolis.Modeling;
using Novolis.Rendering.Appearance;
using Novolis.Rendering.FrameGraph;
using Novolis.Rendering.Materials;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Host-neutral entry points for the Visual-to-Modeling/Rendering boundary.
/// The adapter consumes evaluated modeling data and normalized VisualIR; it
/// does not evaluate omitted geometry generators or modifiers.
/// </summary>
public static class VisualRenderingAdapter
{
    public static RenderingMeshReadiness InspectMesh(
        EvaluatedMesh mesh,
        MeshAttributeFlags requiredAppearanceAttributes =
            MeshAttributeSet.AppearanceDefaults) =>
        RenderingMeshAdapter.InspectReadiness(
            mesh,
            requiredAppearanceAttributes);

    public static RenderingMeshConversion ConvertMesh(
        EvaluatedMesh mesh,
        IMaterial material,
        RenderingAdapterOptions? options = null,
        string? sourcePath = null) =>
        RenderingMeshAdapter.Convert(mesh, material, options, sourcePath);

    public static RenderingSceneConversion ConvertScene(
        IEnumerable<RenderingMeshInput> meshes,
        IReadOnlyList<Novolis.Rendering.Scene.LightDefinition>? lights = null,
        RenderingAdapterOptions? options = null) =>
        RenderingSceneAdapter.Convert(meshes, lights, options);

    public static VisualSceneFeaturePlan LowerSceneFeatures(
        IEnumerable<VisualValue> values,
        string? sourcePath = null) =>
        VisualSceneFeatureAdapter.Lower(values, sourcePath);

    public static VisualFrameGraphResult BuildFrameGraph(
        IEnumerable<FrameEffectValue> effects,
        FrameGraphSize outputSize,
        FrameGraphQualityProfile? qualityProfile = null) =>
        VisualFrameGraphAdapter.Build(effects, outputSize, qualityProfile);

    public static RenderingMaterialResult LowerSurface(
        SurfaceValue surface,
        RenderingAdapterOptions? options = null,
        string? sourcePath = null) =>
        RenderingMaterialAdapter.LowerSurface(surface, options, sourcePath);

    public static RenderingMaterialResult LowerAppearance(
        AppearanceStack stack,
        RenderingAdapterOptions? options = null,
        string? sourcePath = null) =>
        RenderingMaterialAdapter.LowerAppearance(stack, options, sourcePath);

    /// <summary>
    /// Lowers a constant surface output from normalized VisualIR.
    /// </summary>
    public static RenderingMaterialResult LowerConstantSurface(
        VisualIR ir,
        string outputId,
        RenderingAdapterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(ir);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputId);

        var sourcePath = ir.SourceMap.TryGetOutput(outputId, out var outputEntry)
            ? outputEntry?.Path
            : $"outputs/{outputId}";
        if (!ir.Outputs.Any(output => output.Id == outputId))
        {
            return UnsupportedOutput(
                "VRA410",
                $"Visual output '{outputId}' does not exist.",
                sourcePath,
                outputId);
        }

        var output = ir.Outputs.First(output => output.Id == outputId);
        if (!ir.TryGetNode(output.SourceNodeId, out var node) ||
            node is null)
        {
            return UnsupportedOutput(
                "VRA411",
                $"Visual output '{outputId}' refers to a missing source node.",
                sourcePath,
                outputId);
        }

        if (output.Contract.ValueKind != VisualValueKind.Surface)
        {
            return UnsupportedOutput(
                "VRA413",
                $"Visual output '{outputId}' has contract kind '{output.Contract.ValueKind}', not Surface.",
                sourcePath,
                outputId,
                node.Id);
        }

        if (node.ConstantValue is not SurfaceValue surface)
        {
            return UnsupportedOutput(
                "VRA412",
                $"Visual output '{outputId}' is not a constant SurfaceValue.",
                sourcePath ?? $"nodes/{node.Id}",
                outputId,
                node.Id);
        }

        var lowered = RenderingMaterialAdapter.LowerSurface(
            surface,
            options,
            sourcePath ?? $"nodes/{node.Id}");
        var contextualDiagnostics = lowered.Diagnostics
            .Select(diagnostic =>
                diagnostic with
                {
                    NodeId = diagnostic.NodeId ?? node.Id,
                    OutputId = diagnostic.OutputId ?? outputId,
                })
            .ToList();
        AddCapabilityDiagnostics(
            ir,
            sourcePath,
            outputId,
            node.Id,
            contextualDiagnostics);

        var capabilities = new RenderingAdapterCapabilityBuilder();
        foreach (var capability in lowered.Capabilities.Results)
        {
            capabilities.Add(
                capability.Id,
                capability.Status,
                capability.Message,
                capability.Required);
        }

        foreach (var capability in ir.Capabilities.Results)
        {
            capabilities.Add(
                capability.Id,
                capability.Status,
                capability.Message,
                capability.Required);
        }

        var status = AggregateStatus(
            lowered.Status,
            ir.Capabilities.Results.Select(result => result.Status));
        var fingerprint = RenderingAdapterFingerprint.CreateCacheKey(
            "visual-surface",
            lowered.Fingerprint,
            ir.SemanticFingerprint,
            outputId);
        return new RenderingMaterialResult(
            lowered.Material,
            lowered.EvaluatedSurface,
            status,
            contextualDiagnostics,
            capabilities.Build(),
            lowered.Fingerprint,
            fingerprint);
    }

    private static RenderingMaterialResult UnsupportedOutput(
        string code,
        string message,
        string? sourcePath,
        string outputId,
        NodeId? nodeId = null)
    {
        const string capability = "rendering.output.constant-surface.v1";
        var diagnostic = RenderingAdapterDiagnostics.Error(
            code,
            message,
            sourcePath,
            capability,
            nodeId,
            outputId);
        var capabilities = new RenderingAdapterCapabilityBuilder();
        capabilities.Add(capability, VisualCapabilityStatus.Unsupported, message);
        var fingerprint = RenderingAdapterFingerprint.CreateCacheKey(
            "invalid-visual-surface",
            outputId,
            sourcePath,
            nodeId?.ToString());
        return new RenderingMaterialResult(
            null,
            null,
            VisualCapabilityStatus.Unsupported,
            [diagnostic],
            capabilities.Build(),
            fingerprint,
            RenderingAdapterFingerprint.CreateCacheKey(
                "visual-surface",
                fingerprint,
                outputId));
    }

    private static void AddCapabilityDiagnostics(
        VisualIR ir,
        string? sourcePath,
        string outputId,
        NodeId nodeId,
        ICollection<VisualRenderingDiagnostic> diagnostics)
    {
        foreach (var capability in ir.Capabilities.Results.Where(result =>
                     result.Status != VisualCapabilityStatus.Exact))
        {
            var severity = capability.Status == VisualCapabilityStatus.Unsupported &&
                           capability.Required
                ? VisualCompileDiagnosticSeverity.Error
                : VisualCompileDiagnosticSeverity.Warning;
            diagnostics.Add(
                new VisualRenderingDiagnostic(
                    severity,
                    "VRA420",
                    capability.Message ??
                    $"Visual capability '{capability.Id}' is {capability.Status}.",
                    sourcePath,
                    capability.Id,
                    nodeId: nodeId,
                    outputId: outputId));
        }
    }

    private static VisualCapabilityStatus AggregateStatus(
        VisualCapabilityStatus primary,
        IEnumerable<VisualCapabilityStatus> additional)
    {
        var statuses = additional.Append(primary).ToArray();
        if (statuses.Any(status => status == VisualCapabilityStatus.Unsupported))
        {
            return VisualCapabilityStatus.Unsupported;
        }

        if (statuses.Any(status => status == VisualCapabilityStatus.BakedFallback))
        {
            return VisualCapabilityStatus.BakedFallback;
        }

        if (statuses.Any(status => status == VisualCapabilityStatus.Approximate))
        {
            return VisualCapabilityStatus.Approximate;
        }

        return VisualCapabilityStatus.Exact;
    }
}
