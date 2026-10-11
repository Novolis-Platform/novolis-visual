using Novolis.Rendering.Compile;
using Novolis.Visual.Compile;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Delegates an adapted authoring scene to the existing rendering compiler.
/// </summary>
public static class RenderingSceneCompilerAdapter
{
    public static RenderingCompiledSceneResult Compile(
        RenderingSceneConversion conversion)
    {
        ArgumentNullException.ThrowIfNull(conversion);
        var capabilities = new RenderingAdapterCapabilityBuilder();
        foreach (var capability in conversion.Capabilities.Results)
        {
            capabilities.Add(
                capability.Id,
                capability.Status,
                capability.Message,
                capability.Required);
        }

        if (conversion.Scene is null)
        {
            var diagnostics = conversion.Diagnostics
                .Append(
                    RenderingAdapterDiagnostics.Error(
                        "VRA501",
                        "The rendering compiler was not called because scene adaptation failed.",
                        "scene",
                        RenderingAdapterContract.MeshInstanceCapability))
                .ToArray();
            return new RenderingCompiledSceneResult(
                null,
                VisualCapabilityStatus.Unsupported,
                diagnostics,
                capabilities.Build(),
                conversion.Fingerprint,
                RenderingAdapterFingerprint.CreateCacheKey(
                    "compiled-scene",
                    conversion.Fingerprint,
                    VisualCapabilityStatus.Unsupported.ToString()));
        }

        try
        {
            var compiled = SceneCompiler.Compile(conversion.Scene);
            capabilities.Add(
                "rendering.scene-compile.v1",
                VisualCapabilityStatus.Exact);
            return new RenderingCompiledSceneResult(
                compiled,
                conversion.Status,
                conversion.Diagnostics,
                capabilities.Build(),
                conversion.Fingerprint,
                RenderingAdapterFingerprint.CreateCacheKey(
                    "compiled-scene",
                    conversion.Fingerprint,
                    conversion.Status.ToString()));
        }
        catch (ArgumentException exception)
        {
            const string capability = "rendering.scene-compile.v1";
            const string code = "VRA502";
            var diagnostic = RenderingAdapterDiagnostics.Error(
                code,
                $"The existing rendering compiler rejected the adapted scene: {exception.Message}",
                "scene",
                capability);
            capabilities.Add(
                capability,
                VisualCapabilityStatus.Unsupported,
                diagnostic.Message);
            return new RenderingCompiledSceneResult(
                null,
                VisualCapabilityStatus.Unsupported,
                conversion.Diagnostics.Append(diagnostic),
                capabilities.Build(),
                conversion.Fingerprint,
                RenderingAdapterFingerprint.CreateCacheKey(
                    "compiled-scene",
                    conversion.Fingerprint,
                    VisualCapabilityStatus.Unsupported.ToString()));
        }
    }
}
