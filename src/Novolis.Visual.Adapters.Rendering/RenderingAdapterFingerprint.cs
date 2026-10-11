using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Novolis.Modeling;
using Novolis.Rendering.Appearance;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Scene;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Creates deterministic identities for adapter inputs and outputs.
/// </summary>
public static class RenderingAdapterFingerprint
{
    /// <summary>
    /// Computes an identity for one evaluated mesh and its render material.
    /// </summary>
    public static string ForMesh(EvaluatedMesh mesh, IMaterial material)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(material);

        var builder = new StringBuilder();
        Append(builder, "contract", RenderingAdapterContract.Version);
        Append(builder, "source", mesh.SourceId);
        Append(builder, "semantic", EffectiveSemanticHash(mesh));
        Append(builder, "geometry", EffectiveGeometryHash(mesh));
        Append(builder, "cache", mesh.CacheKey);
        Append(builder, "world", mesh.World);
        Append(builder, "material", ForMaterial(material));
        return Hash(builder);
    }

    /// <summary>
    /// Computes an identity for a constant Visual surface.
    /// </summary>
    public static string ForSurface(SurfaceValue surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        var builder = new StringBuilder();
        Append(builder, "contract", RenderingAdapterContract.Version);
        Append(builder, "kind", "surface");
        Append(builder, "material-id", surface.MaterialId);
        Append(builder, "color-space", surface.BaseColor.ColorSpace);
        Append(builder, "red", surface.BaseColor.Red);
        Append(builder, "green", surface.BaseColor.Green);
        Append(builder, "blue", surface.BaseColor.Blue);
        Append(builder, "alpha", surface.BaseColor.Alpha);
        Append(builder, "roughness", surface.Roughness);
        Append(builder, "metallic", surface.Metallic);
        return Hash(builder);
    }

    /// <summary>
    /// Computes an identity for an ordered appearance stack.
    /// </summary>
    public static string ForAppearance(AppearanceStack stack)
    {
        ArgumentNullException.ThrowIfNull(stack);

        var builder = new StringBuilder();
        Append(builder, "contract", RenderingAdapterContract.Version);
        Append(builder, "kind", "appearance");
        Append(builder, "domain", stack.Domain);
        for (var index = 0; index < stack.Layers.Count; index++)
        {
            var layer = stack.Layers[index];
            Append(builder, "layer-index", index);
            Append(builder, "operation", layer.Op);
            Append(builder, "amount", layer.Amount);
            Append(builder, "scale", layer.Scale);
            Append(builder, "color-r", layer.Color.R);
            Append(builder, "color-g", layer.Color.G);
            Append(builder, "color-b", layer.Color.B);
            Append(builder, "color-a", layer.Color.A);
            Append(builder, "seed", layer.Seed);
            Append(builder, "aux", layer.Aux);
        }

        return Hash(builder);
    }

    /// <summary>
    /// Computes an identity for a known rendering material.
    /// </summary>
    public static string ForMaterial(IMaterial material)
    {
        ArgumentNullException.ThrowIfNull(material);

        var builder = new StringBuilder();
        Append(builder, "contract", RenderingAdapterContract.Version);
        switch (material)
        {
            case StandardMaterial standard:
                Append(builder, "type", nameof(StandardMaterial));
                Append(builder, "base-r", standard.BaseColor.X);
                Append(builder, "base-g", standard.BaseColor.Y);
                Append(builder, "base-b", standard.BaseColor.Z);
                Append(builder, "roughness", standard.Roughness);
                Append(builder, "metallic", standard.Metallic);
                Append(builder, "emission-strength", standard.EmissionStrength);
                Append(builder, "emission-r", standard.EmissionColor.X);
                Append(builder, "emission-g", standard.EmissionColor.Y);
                Append(builder, "emission-b", standard.EmissionColor.Z);
                break;
            case GlassMaterial glass:
                Append(builder, "type", nameof(GlassMaterial));
                Append(builder, "tint-r", glass.Tint.X);
                Append(builder, "tint-g", glass.Tint.Y);
                Append(builder, "tint-b", glass.Tint.Z);
                Append(builder, "roughness", glass.Roughness);
                Append(builder, "ior", glass.Ior);
                Append(builder, "absorption", glass.Absorption);
                break;
            case SkinMaterial skin:
                Append(builder, "type", nameof(SkinMaterial));
                Append(builder, "base-r", skin.BaseColor.X);
                Append(builder, "base-g", skin.BaseColor.Y);
                Append(builder, "base-b", skin.BaseColor.Z);
                Append(builder, "roughness", skin.Roughness);
                Append(builder, "subsurface", skin.SubsurfaceStrength);
                Append(builder, "blood-r", skin.BloodTint.X);
                Append(builder, "blood-g", skin.BloodTint.Y);
                Append(builder, "blood-b", skin.BloodTint.Z);
                Append(builder, "scatter-radius", skin.ScatterRadius);
                break;
            case EmissiveMaterial emissive:
                Append(builder, "type", nameof(EmissiveMaterial));
                Append(builder, "emission-r", emissive.EmissionColor.X);
                Append(builder, "emission-g", emissive.EmissionColor.Y);
                Append(builder, "emission-b", emissive.EmissionColor.Z);
                Append(builder, "emission-strength", emissive.EmissionStrength);
                break;
            default:
                Append(builder, "type", material.GetType().AssemblyQualifiedName);
                break;
        }

        return Hash(builder);
    }

    /// <summary>
    /// Computes an identity for a converted scene and its lights.
    /// </summary>
    public static string ForScene(
        IEnumerable<RenderingMeshConversion> meshes,
        IReadOnlyList<LightDefinition> lights)
    {
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentNullException.ThrowIfNull(lights);

        var builder = new StringBuilder();
        Append(builder, "contract", RenderingAdapterContract.Version);
        Append(builder, "kind", "scene");
        foreach (var mesh in meshes.OrderBy(
                     value => value.Fingerprint,
                     StringComparer.Ordinal))
        {
            Append(builder, "mesh", mesh.Fingerprint);
            Append(builder, "mesh-status", mesh.Status);
        }

        for (var index = 0; index < lights.Count; index++)
        {
            var light = lights[index];
            Append(builder, "light-index", index);
            Append(builder, "light-kind", light.Kind);
            Append(builder, "light-x", light.DirectionOrPosition.X);
            Append(builder, "light-y", light.DirectionOrPosition.Y);
            Append(builder, "light-z", light.DirectionOrPosition.Z);
            Append(builder, "light-color-x", light.Color.X);
            Append(builder, "light-color-y", light.Color.Y);
            Append(builder, "light-color-z", light.Color.Z);
            Append(builder, "light-intensity", light.Intensity);
        }

        return Hash(builder);
    }

    /// <summary>
    /// Computes a stable cache key for one adapter artifact.
    /// </summary>
    public static string CreateCacheKey(
        string artifactKind,
        string fingerprint,
        params string?[] components)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);

        var builder = new StringBuilder();
        Append(builder, "contract", RenderingAdapterContract.Version);
        Append(builder, "artifact", artifactKind);
        Append(builder, "fingerprint", fingerprint);
        foreach (var component in components)
        {
            Append(builder, "component", component);
        }

        return $"{RenderingAdapterContract.Version}|{artifactKind}|{Hash(builder)}";
    }

    private static string EffectiveSemanticHash(EvaluatedMesh mesh) =>
        string.IsNullOrWhiteSpace(mesh.SemanticHash)
            ? MeshSemanticHasher.Compute(mesh)
            : mesh.SemanticHash;

    private static string EffectiveGeometryHash(EvaluatedMesh mesh) =>
        string.IsNullOrWhiteSpace(mesh.GeometryHash)
            ? MeshSemanticHasher.ComputeGeometry(mesh.Vertices, mesh.Indices)
            : mesh.GeometryHash;

    private static string Hash(StringBuilder builder) =>
        Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())))
            .ToLowerInvariant();

    private static void Append(StringBuilder builder, string name, object? value)
    {
        builder.Append(name)
            .Append('=')
            .Append(value switch
            {
                null => "<null>",
                Guid guid => guid.ToString("D"),
                Vector3 vector => Vector(vector),
                float single => BitConverter.SingleToInt32Bits(single)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture),
                double number => BitConverter.DoubleToInt64Bits(number)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => Convert.ToString(
                         value,
                         System.Globalization.CultureInfo.InvariantCulture)
                     ?? string.Empty,
            })
            .Append('\n');
    }

    private static string Vector(Vector3 value) =>
        string.Join(
            ',',
            BitConverter.SingleToInt32Bits(value.X),
            BitConverter.SingleToInt32Bits(value.Y),
            BitConverter.SingleToInt32Bits(value.Z));
}
