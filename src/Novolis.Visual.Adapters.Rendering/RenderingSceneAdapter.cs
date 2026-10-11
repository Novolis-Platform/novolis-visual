using Novolis.Rendering.Scene;
using Novolis.Visual.Compile;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Assembles evaluated mesh conversions into the existing rendering
/// <see cref="Scene"/> contract.
/// </summary>
public static class RenderingSceneAdapter
{
    /// <summary>
    /// Converts each evaluated mesh and creates a scene only when every
    /// required mesh has a usable geometry and material contract.
    /// </summary>
    public static RenderingSceneConversion Convert(
        IEnumerable<RenderingMeshInput> meshes,
        IReadOnlyList<LightDefinition>? lights = null,
        RenderingAdapterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(meshes);
        var effectiveOptions = options ?? RenderingAdapterOptions.Default;
        var effectiveLights = (lights ?? []).ToArray();
        var inputs = meshes
            .OrderBy(input => input.Mesh.SourceId)
            .ThenBy(input => input.SourcePath, StringComparer.Ordinal)
            .ToArray();
        var conversions = inputs
            .Select(input =>
                RenderingMeshAdapter.Convert(
                    input.Mesh,
                    input.Material,
                    effectiveOptions,
                    input.SourcePath))
            .ToArray();
        var diagnostics = conversions
            .SelectMany(conversion => conversion.Diagnostics)
            .ToList();
        var capabilities = new RenderingAdapterCapabilityBuilder();
        foreach (var result in conversions.SelectMany(
                     conversion => conversion.Capabilities.Results))
        {
            capabilities.Add(
                result.Id,
                result.Status,
                result.Message,
                result.Required);
        }

        var status = AggregateStatus(conversions.Select(conversion => conversion.Status));
        Scene? scene = null;
        if (conversions.All(conversion => conversion.Mesh is not null))
        {
            scene = new Scene(
                conversions
                    .Select(conversion => conversion.Mesh!)
                    .ToArray(),
                effectiveLights);
        }
        else
        {
            status = VisualCapabilityStatus.Unsupported;
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA401",
                    "The rendering scene was not created because at least one mesh conversion is unusable.",
                    "scene",
                    RenderingAdapterContract.MeshInstanceCapability));
        }

        var fingerprint = RenderingAdapterFingerprint.ForScene(
            conversions,
            effectiveLights);
        return new RenderingSceneConversion(
            scene,
            conversions,
            status,
            diagnostics,
            capabilities.Build(),
            fingerprint,
            RenderingAdapterFingerprint.CreateCacheKey(
                "scene",
                fingerprint,
                status.ToString()));
    }

    private static VisualCapabilityStatus AggregateStatus(
        IEnumerable<VisualCapabilityStatus> statuses)
    {
        var values = statuses.ToArray();
        if (values.Any(status => status == VisualCapabilityStatus.Unsupported))
        {
            return VisualCapabilityStatus.Unsupported;
        }

        if (values.Any(status => status == VisualCapabilityStatus.BakedFallback))
        {
            return VisualCapabilityStatus.BakedFallback;
        }

        if (values.Any(status => status == VisualCapabilityStatus.Approximate))
        {
            return VisualCapabilityStatus.Approximate;
        }

        return VisualCapabilityStatus.Exact;
    }
}
