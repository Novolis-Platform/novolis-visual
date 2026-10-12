using System.Numerics;
using Novolis.Rendering.Appearance;
using Novolis.Rendering.Materials;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Lowers Visual constant surfaces and existing appearance stacks to the
/// renderer's authoring material contracts.
/// </summary>
public static class RenderingMaterialAdapter
{
    /// <summary>
    /// Lowers a constant PBR surface to <see cref="StandardMaterial"/>.
    /// Only linear sRGB and sRGB colors are accepted; unsupported encodings
    /// are never silently treated as linear values.
    /// </summary>
    public static RenderingMaterialResult LowerSurface(
        SurfaceValue surface,
        RenderingAdapterOptions? options = null,
        string? sourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(surface);
        var effectiveOptions = options ?? RenderingAdapterOptions.Default;
        var fingerprint = RenderingAdapterFingerprint.ForSurface(surface);
        var capabilities = new RenderingAdapterCapabilityBuilder();
        var diagnostics = new List<VisualRenderingDiagnostic>();

        var target = effectiveOptions.TargetCapabilities.GetSupport(
            RenderingAdapterContract.StandardSurfaceCapability);
        var targetStatus = ResolveTargetStatus(
            target,
            RenderingAdapterContract.StandardSurfaceCapability,
            sourcePath,
            diagnostics);
        capabilities.Add(
            RenderingAdapterContract.StandardSurfaceCapability,
            targetStatus,
            target.Message);

        if (surface.BaseColor.Alpha != 1d)
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA210",
                    "The rendering material contract has no alpha channel for a constant surface.",
                    sourcePath,
                    RenderingAdapterContract.StandardSurfaceCapability));
            capabilities.Add(
                RenderingAdapterContract.StandardSurfaceCapability,
                VisualCapabilityStatus.Unsupported,
                "Surface alpha requires a transparency-capable target.");
            return CreateResult(
                null,
                null,
                VisualCapabilityStatus.Unsupported,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        if (!TryConvertColor(surface.BaseColor, out var baseColor, out var colorMessage))
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA211",
                    colorMessage,
                    sourcePath,
                    RenderingAdapterContract.StandardSurfaceCapability));
            capabilities.Add(
                RenderingAdapterContract.StandardSurfaceCapability,
                VisualCapabilityStatus.Unsupported,
                colorMessage);
            return CreateResult(
                null,
                null,
                VisualCapabilityStatus.Unsupported,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        if (targetStatus == VisualCapabilityStatus.Unsupported)
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA212",
                    target.Message ??
                    "The selected rendering target does not support constant standard surfaces.",
                    sourcePath,
                    RenderingAdapterContract.StandardSurfaceCapability));
            return CreateResult(
                null,
                null,
                targetStatus,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        var material = new StandardMaterial
        {
            BaseColor = baseColor,
            Roughness = (float)surface.Roughness,
            Metallic = (float)surface.Metallic,
        };

        if (targetStatus != VisualCapabilityStatus.Exact)
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Warning(
                    "VRA213",
                    $"The target lowers the constant surface as {targetStatus}.",
                    sourcePath,
                    RenderingAdapterContract.StandardSurfaceCapability));
        }

        return CreateResult(
            material,
            null,
            targetStatus,
            diagnostics,
            capabilities,
            fingerprint,
            sourcePath);
    }

    /// <summary>
    /// Lowers a channelized material value to the closest existing renderer
    /// material while reporting every channel that cannot be represented.
    /// </summary>
    public static RenderingMaterialResult LowerMaterial(
        MaterialValue material,
        RenderingAdapterOptions? options = null,
        string? sourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(material);
        var effectiveOptions = options ?? RenderingAdapterOptions.Default;
        var fingerprint = RenderingAdapterFingerprint.ForMaterialValue(material);
        var capabilities = new RenderingAdapterCapabilityBuilder();
        var diagnostics = new List<VisualRenderingDiagnostic>();
        var target = effectiveOptions.TargetCapabilities.GetSupport(
            RenderingAdapterContract.MaterialAuthoringCapability);
        var targetStatus = ResolveTargetStatus(
            target,
            RenderingAdapterContract.MaterialAuthoringCapability,
            sourcePath,
            diagnostics);
        capabilities.Add(
            RenderingAdapterContract.MaterialAuthoringCapability,
            targetStatus,
            target.Message);

        if (targetStatus == VisualCapabilityStatus.Unsupported)
        {
            return CreateResult(
                null,
                null,
                targetStatus,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        if (!TryConvertColor(
                material.BaseColor,
                out var baseColor,
                out var baseColorMessage))
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA250",
                    baseColorMessage,
                    sourcePath,
                    RenderingAdapterContract.MaterialAuthoringCapability));
            return CreateResult(
                null,
                null,
                VisualCapabilityStatus.Unsupported,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        if (!TryConvertColor(
                material.EmissionColor,
                out var emissionColor,
                out var emissionMessage))
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA250",
                    emissionMessage,
                    sourcePath,
                    RenderingAdapterContract.MaterialAuthoringCapability));
            return CreateResult(
                null,
                null,
                VisualCapabilityStatus.Unsupported,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        if (material.Transmission > 0)
        {
            var transmissionSupport =
                effectiveOptions.TargetCapabilities.GetSupport(
                    RenderingAdapterContract.MaterialTransmissionCapability);
            capabilities.Add(
                RenderingAdapterContract.MaterialTransmissionCapability,
                transmissionSupport.Status,
                transmissionSupport.Message);
            const string message =
                "The selected target cannot represent material transmission.";
            if (transmissionSupport.Status == VisualCapabilityStatus.Unsupported)
            {
                diagnostics.Add(
                    RenderingAdapterDiagnostics.Error(
                        "VRA251",
                        message,
                        sourcePath,
                        RenderingAdapterContract.MaterialTransmissionCapability));
                return CreateResult(
                    null,
                    null,
                    VisualCapabilityStatus.Unsupported,
                    diagnostics,
                    capabilities,
                    fingerprint,
                    sourcePath);
            }
        }

        if (material.EmissionStrength > 0)
        {
            var emissionSupport = effectiveOptions.TargetCapabilities.GetSupport(
                RenderingAdapterContract.MaterialEmissionCapability);
            capabilities.Add(
                RenderingAdapterContract.MaterialEmissionCapability,
                emissionSupport.Status,
                emissionSupport.Message);
            if (emissionSupport.Status == VisualCapabilityStatus.Unsupported)
            {
                diagnostics.Add(
                    RenderingAdapterDiagnostics.Warning(
                        "VRA252",
                        "The selected target will omit material emission.",
                        sourcePath,
                        RenderingAdapterContract.MaterialEmissionCapability));
            }
        }

        IMaterial lowered;
        SurfaceSample? sample = null;
        if (material.Transmission > 0 ||
            material.Model == VisualMaterialModel.Glass)
        {
            lowered = new GlassMaterial
            {
                Tint = baseColor,
                Roughness = (float)material.Roughness,
                Ior = (float)material.Ior,
                Absorption = (float)material.Absorption,
            };
        }
        else if (material.Model == VisualMaterialModel.Emissive &&
                 material.Metallic == 0)
        {
            lowered = new EmissiveMaterial
            {
                EmissionColor = emissionColor,
                EmissionStrength = (float)material.EmissionStrength,
            };
        }
        else
        {
            lowered = new StandardMaterial
            {
                BaseColor = baseColor,
                Roughness = (float)material.Roughness,
                Metallic = (float)material.Metallic,
                EmissionColor = emissionColor,
                EmissionStrength = (float)material.EmissionStrength,
            };
            sample = new SurfaceSample(
                ToRgba(material.BaseColor),
                (float)material.Roughness,
                (float)material.Metallic,
                ToRgba(material.EmissionColor),
                (float)material.Alpha);
        }

        var status = targetStatus;
        if (material.Clearcoat > 0 ||
            System.Math.Abs(material.Anisotropy) > 0)
        {
            status = status == VisualCapabilityStatus.Exact
                ? VisualCapabilityStatus.Approximate
                : status;
            diagnostics.Add(
                RenderingAdapterDiagnostics.Warning(
                    "VRA253",
                    "Clearcoat and anisotropy are approximated by the current material contracts.",
                    sourcePath,
                    RenderingAdapterContract.MaterialAuthoringCapability));
        }

        if (material.Alpha < 1)
        {
            status = effectiveOptions.AllowBakedFallback
                ? VisualCapabilityStatus.BakedFallback
                : VisualCapabilityStatus.Unsupported;
            diagnostics.Add(
                status == VisualCapabilityStatus.BakedFallback
                    ? RenderingAdapterDiagnostics.Warning(
                        "VRA254",
                        "Material alpha requires an explicit transparency bake.",
                        sourcePath,
                        RenderingAdapterContract.MaterialAuthoringCapability)
                    : RenderingAdapterDiagnostics.Error(
                        "VRA255",
                        "Material alpha requires a transparency-capable target.",
                        sourcePath,
                        RenderingAdapterContract.MaterialAuthoringCapability));
            if (status == VisualCapabilityStatus.Unsupported)
            {
                return CreateResult(
                    null,
                    sample,
                    status,
                    diagnostics,
                    capabilities,
                    fingerprint,
                    sourcePath);
            }
        }

        return CreateResult(
            lowered,
            sample,
            status,
            diagnostics,
            capabilities,
            fingerprint,
            sourcePath);
    }

    /// <summary>
    /// Lowers an existing appearance stack when all operations have fixed
    /// material equivalents. Procedural operations report Unsupported or
    /// BakedFallback; they are not sampled into a fixed material.
    /// </summary>
    public static RenderingMaterialResult LowerAppearance(
        AppearanceStack stack,
        RenderingAdapterOptions? options = null,
        string? sourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(stack);
        var effectiveOptions = options ?? RenderingAdapterOptions.Default;
        var fingerprint = RenderingAdapterFingerprint.ForAppearance(stack);
        var capabilities = new RenderingAdapterCapabilityBuilder();
        var diagnostics = new List<VisualRenderingDiagnostic>();

        if (stack.Domain != AppearanceDomain.Surface)
        {
            const string capability = RenderingAdapterContract.ConstantAppearanceCapability;
            const string message =
                "Only surface appearance stacks can lower to an IMaterial.";
            capabilities.Add(capability, VisualCapabilityStatus.Unsupported, message);
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA220",
                    message,
                    sourcePath,
                    capability));
            return CreateResult(
                null,
                null,
                VisualCapabilityStatus.Unsupported,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        var procedural = new List<(int Index, AppearanceOp Op)>();
        var invalid = false;
        for (var index = 0; index < stack.Layers.Count; index++)
        {
            var layer = stack.Layers[index];
            if (!IsFinite(layer))
            {
                invalid = true;
                diagnostics.Add(
                    RenderingAdapterDiagnostics.Error(
                        "VRA221",
                        $"Appearance layer {index} contains a non-finite parameter.",
                        LayerPath(sourcePath, index),
                        CapabilityFor(layer.Op)));
                continue;
            }

            if (IsConstantSurfaceOperation(layer))
            {
                continue;
            }

            if (IsBakeableSurfaceOperation(layer.Op))
            {
                procedural.Add((index, layer.Op));
                continue;
            }

            invalid = true;
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA222",
                    $"Appearance operation '{layer.Op}' has no fixed material lowering.",
                    LayerPath(sourcePath, index),
                    CapabilityFor(layer.Op)));
            capabilities.Add(
                CapabilityFor(layer.Op),
                VisualCapabilityStatus.Unsupported,
                "The operation needs a field, texture, or another target contract.");
        }

        if (invalid)
        {
            return CreateResult(
                null,
                null,
                VisualCapabilityStatus.Unsupported,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        if (procedural.Count > 0)
        {
            var status = effectiveOptions.AllowBakedFallback
                ? VisualCapabilityStatus.BakedFallback
                : VisualCapabilityStatus.Unsupported;
            foreach (var (index, operation) in procedural)
            {
                var capability = CapabilityFor(operation);
                capabilities.Add(
                    capability,
                    status,
                    effectiveOptions.AllowBakedFallback
                        ? "The operation requires an explicit appearance bake."
                        : "The fixed material target cannot represent this operation.");
                diagnostics.Add(
                    effectiveOptions.AllowBakedFallback
                        ? RenderingAdapterDiagnostics.Warning(
                            "VRA223",
                            $"Appearance operation '{operation}' requires a bake; no texture is produced by this adapter.",
                            LayerPath(sourcePath, index),
                            capability)
                        : RenderingAdapterDiagnostics.Error(
                            "VRA224",
                            $"Appearance operation '{operation}' is unsupported by fixed materials.",
                            LayerPath(sourcePath, index),
                            capability));
            }

            return CreateResult(
                null,
                TryEvaluateSurface(stack),
                status,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        var sample = TryEvaluateSurface(stack);
        if (sample is null)
        {
            const string message = "The appearance stack could not be evaluated.";
            capabilities.Add(
                RenderingAdapterContract.ConstantAppearanceCapability,
                VisualCapabilityStatus.Unsupported,
                message);
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA225",
                    message,
                    sourcePath,
                    RenderingAdapterContract.ConstantAppearanceCapability));
            return CreateResult(
                null,
                null,
                VisualCapabilityStatus.Unsupported,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        if (sample.Value.Alpha < 1f)
        {
            const string message =
                "The appearance stack produces transparency, which fixed IMaterial contracts do not carry.";
            capabilities.Add(
                RenderingAdapterContract.ConstantAppearanceCapability,
                VisualCapabilityStatus.Unsupported,
                message);
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA226",
                    message,
                    sourcePath,
                    RenderingAdapterContract.ConstantAppearanceCapability));
            return CreateResult(
                null,
                sample,
                VisualCapabilityStatus.Unsupported,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        var target = effectiveOptions.TargetCapabilities.GetSupport(
            RenderingAdapterContract.ConstantAppearanceCapability);
        var targetStatus = ResolveTargetStatus(
            target,
            RenderingAdapterContract.ConstantAppearanceCapability,
            sourcePath,
            diagnostics);
        capabilities.Add(
            RenderingAdapterContract.ConstantAppearanceCapability,
            targetStatus,
            target.Message);

        if (targetStatus == VisualCapabilityStatus.Unsupported)
        {
            return CreateResult(
                null,
                sample,
                targetStatus,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        var material = ToStandardMaterial(sample.Value);
        if (targetStatus != VisualCapabilityStatus.Exact)
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Warning(
                    "VRA227",
                    $"The target lowers the constant appearance as {targetStatus}.",
                    sourcePath,
                    RenderingAdapterContract.ConstantAppearanceCapability));
        }

        return CreateResult(
            material,
            sample,
            targetStatus,
            diagnostics,
            capabilities,
            fingerprint,
            sourcePath);
    }

    /// <summary>
    /// Inspects a concrete renderer material before it is attached to a mesh.
    /// </summary>
    public static RenderingMaterialResult Inspect(
        IMaterial material,
        string? sourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(material);
        var fingerprint = RenderingAdapterFingerprint.ForMaterial(material);
        var capabilities = new RenderingAdapterCapabilityBuilder();
        var diagnostics = new List<VisualRenderingDiagnostic>();

        if (material is StandardMaterial or GlassMaterial or SkinMaterial or EmissiveMaterial)
        {
            capabilities.Add(
                "rendering.material.authoring.v1",
                VisualCapabilityStatus.Exact);
            return CreateResult(
                material,
                null,
                VisualCapabilityStatus.Exact,
                diagnostics,
                capabilities,
                fingerprint,
                sourcePath);
        }

        const string message =
            "The rendering compiler does not recognize this IMaterial implementation.";
        capabilities.Add(
            "rendering.material.authoring.v1",
            VisualCapabilityStatus.Unsupported,
            message);
        diagnostics.Add(
            RenderingAdapterDiagnostics.Error(
                "VRA230",
                message,
                sourcePath,
                "rendering.material.authoring.v1"));
        return CreateResult(
            null,
            null,
            VisualCapabilityStatus.Unsupported,
            diagnostics,
            capabilities,
            fingerprint,
            sourcePath);
    }

    private static RenderingMaterialResult CreateResult(
        IMaterial? material,
        SurfaceSample? sample,
        VisualCapabilityStatus status,
        IEnumerable<VisualRenderingDiagnostic> diagnostics,
        RenderingAdapterCapabilityBuilder capabilities,
        string fingerprint,
        string? sourcePath) =>
        new(
            material,
            sample,
            status,
            diagnostics,
            capabilities.Build(),
            fingerprint,
            RenderingAdapterFingerprint.CreateCacheKey(
                "material",
                fingerprint,
                status.ToString(),
                sourcePath));

    private static StandardMaterial ToStandardMaterial(SurfaceSample sample)
    {
        var emission = ToVector(sample.Emission);
        return new StandardMaterial
        {
            BaseColor = ToVector(sample.Albedo),
            Roughness = sample.Roughness,
            Metallic = sample.Metallic,
            EmissionColor = emission,
            EmissionStrength = emission == Vector3.Zero ? 0f : 1f,
        };
    }

    private static SurfaceSample? TryEvaluateSurface(AppearanceStack stack)
    {
        try
        {
            return AppearanceEvaluator.EvaluateSurface(stack, default);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool IsConstantSurfaceOperation(AppearanceLayer layer) =>
        layer.Op switch
        {
            AppearanceOp.Identity => true,
            AppearanceOp.BaseColor => true,
            AppearanceOp.Roughness => true,
            AppearanceOp.Metallic => true,
            AppearanceOp.Multiply => true,
            AppearanceOp.Mix => true,
            AppearanceOp.Emission => layer.Scale <= 0f,
            _ => false,
        };

    private static bool IsBakeableSurfaceOperation(AppearanceOp operation) =>
        operation is AppearanceOp.Emission
            or AppearanceOp.Noise
            or AppearanceOp.PanelGrid
            or AppearanceOp.EdgeWear
            or AppearanceOp.Fresnel
            or AppearanceOp.Transparency;

    private static string CapabilityFor(AppearanceOp operation) =>
        $"rendering.appearance.op.{operation.ToString().ToLowerInvariant()}";

    private static string LayerPath(string? sourcePath, int index) =>
        $"{sourcePath ?? "appearance"}/layers/{index}";

    private static bool IsFinite(AppearanceLayer layer) =>
        float.IsFinite(layer.Amount) &&
        float.IsFinite(layer.Scale) &&
        float.IsFinite(layer.Aux);

    private static VisualCapabilityStatus ResolveTargetStatus(
        VisualCapabilitySupport target,
        string capability,
        string? sourcePath,
        ICollection<VisualRenderingDiagnostic> diagnostics)
    {
        if (target.Status == VisualCapabilityStatus.Unsupported)
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Error(
                    "VRA240",
                    target.Message ?? $"Capability '{capability}' is unsupported.",
                    sourcePath,
                    capability));
        }

        return target.Status;
    }

    private static bool TryConvertColor(
        ColorValue color,
        out Vector3 value,
        out string message)
    {
        if (color.ColorSpace == ColorSpace.LinearSrgb)
        {
            value = new Vector3(
                (float)color.Red,
                (float)color.Green,
                (float)color.Blue);
            message = string.Empty;
            return true;
        }

        if (color.ColorSpace == ColorSpace.Srgb &&
            IsUnitRange(color.Red) &&
            IsUnitRange(color.Green) &&
            IsUnitRange(color.Blue))
        {
            value = new Vector3(
                (float)SrgbToLinear(color.Red),
                (float)SrgbToLinear(color.Green),
                (float)SrgbToLinear(color.Blue));
            message = string.Empty;
            return true;
        }

        value = default;
        message =
            $"Color space '{color.ColorSpace}' has no exact conversion in the rendering adapter.";
        return false;
    }

    private static bool IsUnitRange(double value) => value is >= 0d and <= 1d;

    private static double SrgbToLinear(double value) =>
        value <= 0.04045d
            ? value / 12.92d
            : System.Math.Pow((value + 0.055d) / 1.055d, 2.4d);

    private static Vector3 ToVector(Novolis.Math.Geometry.Rgba32 color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f);

    private static Novolis.Math.Geometry.Rgba32 ToRgba(ColorValue color) =>
        new(
            (byte)System.Math.Clamp(
                (int)System.Math.Round(color.Red * 255d),
                0,
                255),
            (byte)System.Math.Clamp(
                (int)System.Math.Round(color.Green * 255d),
                0,
                255),
            (byte)System.Math.Clamp(
                (int)System.Math.Round(color.Blue * 255d),
                0,
                255),
            (byte)System.Math.Clamp(
                (int)System.Math.Round(color.Alpha * 255d),
                0,
                255));
}
