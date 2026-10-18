using System.Collections.Immutable;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Novolis.Modeling;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Scene;
using Novolis.Visual.Assets;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Converts portable asset component metadata into the existing Modeling scene
/// evaluator and Rendering scene contract. The adapter is intentionally
/// unaware of catalog entries, product templates, or asset identifiers.
/// </summary>
public static class VisualAssetSceneAdapter
{
    /// <summary>
    /// Builds and lowers one asset assembly using its serialized component
    /// geometry and material-slot bindings.
    /// </summary>
    public static VisualAssetScenePreview Convert(
        AssetRecipe asset,
        AssetInstance? instance,
        IReadOnlyDictionary<string, IMaterial> materials,
        IMaterial fallbackMaterial,
        IReadOnlyList<LightDefinition>? lights = null,
        RenderingAdapterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(fallbackMaterial);

        var document = new SceneDocument
        {
            Name = asset.DisplayName,
            Generator = "Novolis.Visual.Adapters.Rendering",
        };
        var materialSlots = BuildMaterialSlotMap(asset);
        var nodeMaterials = new Dictionary<Guid, IMaterial>();
        var nodePaths = new Dictionary<Guid, string>();
        var diagnostics = new List<VisualRenderingDiagnostic>();
        var geometryCount = 0;

        foreach (var component in asset.Components.Where(component =>
                     component.Kind == AssetComponentKind.Geometry))
        {
            var geometry = component.Geometry ??
                           TryReadGeometry(asset, component) ??
                           CreateFallbackGeometry(asset, component);
            if (component.Geometry is null)
            {
                diagnostics.Add(
                    RenderingAdapterDiagnostics.Warning(
                        "VRA501",
                        $"Component '{component.Id}' has no portable geometry definition; a box fallback was used.",
                        $"assets/{asset.AssetId}/components/{component.Id}",
                        "visual.asset.geometry.v1"));
            }

            var slotId = component.MaterialSlotId ??
                         (materialSlots.TryGetValue(
                              SourceKey(component.Source),
                              out var componentSlot)
                             ? componentSlot
                             : null);
            var material = slotId is not null &&
                           asset.FindSlot(slotId) is { } slot &&
                           materials.TryGetValue(slot.MaterialId, out var slotMaterial)
                ? slotMaterial
                : fallbackMaterial;

            var specs = ExpandGeometry(
                asset,
                instance,
                component,
                geometry,
                diagnostics);
            foreach (var spec in specs)
            {
                var node = new MeshNode
                {
                    Id = StableGuid(
                        $"{asset.AssetId}|{component.Id}|{spec.Suffix}"),
                    Name = spec.Name,
                    Primitive = spec.Primitive,
                    Size =
                    [
                        (float)global::System.Math.Max(0.01, spec.Size.X),
                        (float)global::System.Math.Max(0.01, spec.Size.Y),
                        (float)global::System.Math.Max(0.01, spec.Size.Z),
                    ],
                    Segments = geometry.Segments,
                    Transform = new SceneTransform
                    {
                        Position =
                        [
                            spec.Position.X,
                            spec.Position.Y,
                            spec.Position.Z,
                        ],
                        RotationDeg =
                        [
                            spec.Rotation.X,
                            spec.Rotation.Y,
                            spec.Rotation.Z,
                        ],
                    },
                };
                document.Nodes.Add(node);
                nodeMaterials[node.Id] = material;
                nodePaths[node.Id] =
                    $"assets/{asset.AssetId}/components/{component.Id}/{spec.Suffix}";
                geometryCount++;
            }
        }

        if (geometryCount == 0)
        {
            var node = new MeshNode
            {
                Id = StableGuid($"{asset.AssetId}|fallback"),
                Name = "Asset geometry",
                Primitive = MeshPrimitiveKind.Box,
                Size = [1, 1, 1],
            };
            document.Nodes.Add(node);
            nodeMaterials[node.Id] = fallbackMaterial;
            nodePaths[node.Id] = $"assets/{asset.AssetId}/fallback";
            diagnostics.Add(
                RenderingAdapterDiagnostics.Warning(
                    "VRA502",
                    "The asset has no geometry components; a neutral box was used.",
                    $"assets/{asset.AssetId}",
                    "visual.asset.geometry.v1"));
        }

        var evaluator = new SceneEvaluator();
        evaluator.Bind(document);
        var evaluatedMeshes = evaluator.Cache.EvaluatedMeshes;
        var inputs = evaluatedMeshes.Select(
            mesh =>
                new RenderingMeshInput(
                    mesh,
                    nodeMaterials.TryGetValue(mesh.SourceId, out var material)
                        ? material
                        : fallbackMaterial,
                    nodePaths.TryGetValue(mesh.SourceId, out var path)
                        ? path
                        : $"assets/{asset.AssetId}/geometry"));
        var conversion = VisualRenderingAdapter.ConvertScene(
            inputs,
            lights ?? DefaultLights(),
            options);
        diagnostics.AddRange(conversion.Diagnostics);

        var (minimum, maximum) = Bounds(evaluatedMeshes);
        return new VisualAssetScenePreview(
            conversion,
            minimum,
            maximum,
            diagnostics.ToImmutableArray(),
            AggregateStatus(
                conversion.Status,
                diagnostics));
    }

    private static IReadOnlyDictionary<string, string> BuildMaterialSlotMap(
        AssetRecipe asset) =>
        asset.Components
            .Where(component =>
                component.Kind == AssetComponentKind.MaterialBinding &&
                component.MaterialSlotId is not null)
            .GroupBy(
                component => SourceKey(component.Source),
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(component => component.MaterialSlotId!)
                    .First(),
                StringComparer.Ordinal);

    private static AssetGeometryDefinition? TryReadGeometry(
        AssetRecipe asset,
        AssetComponent component)
    {
        var node = asset.Graph.Nodes.FirstOrDefault(
            candidate => candidate.Id == component.Source.NodeId);
        if (node?.Values.TryGetValue("value", out var value) != true ||
            value is not GeometryValue geometry)
        {
            return null;
        }

        return new AssetGeometryDefinition(
            geometry.AssetId,
            AssetGeometryPrimitive.Box);
    }

    private static AssetGeometryDefinition CreateFallbackGeometry(
        AssetRecipe asset,
        AssetComponent component) =>
        new(
            $"{asset.AssetId}.{component.Id}.fallback",
            AssetGeometryPrimitive.Box);

    private static IEnumerable<GeometrySpec> ExpandGeometry(
        AssetRecipe asset,
        AssetInstance? instance,
        AssetComponent component,
        AssetGeometryDefinition geometry,
        ICollection<VisualRenderingDiagnostic> diagnostics)
    {
        var size = ResolveVector(
            geometry.Size,
            geometry.SizeBindings,
            asset,
            instance);
        var position = ResolveVector(
            geometry.Position,
            geometry.PositionBindings,
            asset,
            instance);
        var rotation = ToVector(geometry.RotationDegrees);
        var repeat = geometry.Repeat;
        var count = repeat is null
            ? 1
            : ResolveCount(repeat, asset, instance);

        if (geometry.Primitive == AssetGeometryPrimitive.Text)
        {
            var text = ResolveText(
                geometry.TextParameterId,
                asset,
                instance);
            var spacing = geometry.TextSpacingParameterId is null
                ? 0.1
                : ResolveScalar(
                    geometry.TextSpacingParameterId,
                    asset,
                    instance,
                    0.1);
            return ExpandText(
                component,
                text,
                size,
                position,
                spacing,
                diagnostics);
        }

        var primitive = ToModelingPrimitive(geometry.Primitive);
        var specs = new List<GeometrySpec>();
        for (var index = 0; index < count; index++)
        {
            var repeated = RepeatTransform(
                position,
                rotation,
                repeat,
                index,
                count);
            specs.Add(
                new GeometrySpec(
                    $"{geometry.GeometryId}-{index:D3}",
                    component.Name,
                    primitive,
                    size,
                    repeated.Position,
                    repeated.Rotation));
        }

        return specs;
    }

    private static IEnumerable<GeometrySpec> ExpandText(
        AssetComponent component,
        string text,
        Vector3 size,
        Vector3 position,
        double spacing,
        ICollection<VisualRenderingDiagnostic> diagnostics)
    {
        var cell = global::System.Math.Max(0.01f, size.Y / 7f);
        var depth = global::System.Math.Max(0.01f, size.Z);
        var advance = cell * 6f + (float)spacing;
        var start = -global::System.Math.Max(0, text.Length - 1) *
                    advance *
                    0.5f;
        var specs = new List<GeometrySpec>();
        for (var characterIndex = 0; characterIndex < text.Length; characterIndex++)
        {
            var glyph = Glyph(text[characterIndex]);
            for (var row = 0; row < glyph.Length; row++)
            {
                for (var column = 0; column < glyph[row].Length; column++)
                {
                    if (glyph[row][column] != '#')
                    {
                        continue;
                    }

                    specs.Add(
                        new GeometrySpec(
                            $"text-{characterIndex:D3}-{row:D2}-{column:D2}",
                            component.Name,
                            MeshPrimitiveKind.Box,
                            new Vector3(cell, cell, depth),
                            new Vector3(
                                start +
                                characterIndex * advance +
                                column * cell,
                                position.Y +
                                (3f - row) * cell,
                                position.Z),
                            Vector3.Zero));
                }
            }
        }

        if (specs.Count == 0)
        {
            diagnostics.Add(
                RenderingAdapterDiagnostics.Warning(
                    "VRA503",
                    $"Text component '{component.Id}' has no visible glyphs.",
                    $"components/{component.Id}",
                    "visual.asset.text.v1"));
        }

        return specs;
    }

    private static string[] Glyph(char value) =>
        char.ToUpperInvariant(value) switch
        {
            'A' => [".###.", "#...#", "#####", "#...#", "#...#"],
            'B' => ["####.", "#...#", "####.", "#...#", "####."],
            'C' => [".####", "#....", "#....", "#....", ".####"],
            'D' => ["####.", "#...#", "#...#", "#...#", "####."],
            'E' => ["#####", "#....", "####.", "#....", "#####"],
            'F' => ["#####", "#....", "####.", "#....", "#...."],
            'G' => [".####", "#....", "#.###", "#...#", ".###."],
            'H' => ["#...#", "#...#", "#####", "#...#", "#...#"],
            'I' => ["#####", "..#..", "..#..", "..#..", "#####"],
            'J' => ["..###", "...#.", "...#.", "#..#.", ".##.."],
            'K' => ["#..#.", "#.#..", "##...", "#.#..", "#..#."],
            'L' => ["#....", "#....", "#....", "#....", "#####"],
            'M' => ["#...#", "##.##", "#.#.#", "#...#", "#...#"],
            'N' => ["#...#", "##..#", "#.#.#", "#..##", "#...#"],
            'O' => [".###.", "#...#", "#...#", "#...#", ".###."],
            'P' => ["####.", "#...#", "####.", "#....", "#...."],
            'Q' => [".###.", "#...#", "#.#.#", "#..##", ".####"],
            'R' => ["####.", "#...#", "####.", "#.#..", "#..#."],
            'S' => [".####", "#....", ".###.", "....#", "####."],
            'T' => ["#####", "..#..", "..#..", "..#..", "..#.."],
            'U' => ["#...#", "#...#", "#...#", "#...#", ".###."],
            'V' => ["#...#", "#...#", "#...#", ".#.#.", "..#.."],
            'W' => ["#...#", "#...#", "#.#.#", "##.##", "#...#"],
            'X' => ["#...#", ".#.#.", "..#..", ".#.#.", "#...#"],
            'Y' => ["#...#", ".#.#.", "..#..", "..#..", "..#.."],
            'Z' => ["#####", "...#.", "..#..", ".#...", "#####"],
            _ => ["#####", "....#", "...#.", "..#..", ".#..."],
        };

    private static (Vector3 Position, Vector3 Rotation) RepeatTransform(
        Vector3 position,
        Vector3 rotation,
        AssetGeometryRepeat? repeat,
        int index,
        int count)
    {
        if (repeat is null || count == 1)
        {
            return (position, rotation);
        }

        if (repeat.Pattern == AssetGeometryRepeatPattern.Linear)
        {
            var offset = (float)((index - (count - 1) * 0.5) * repeat.Spacing);
            return (
                position + AxisVector(repeat.Axis) * offset,
                rotation);
        }

        var angle = (float)(
            index * global::System.Math.PI * 2d / count);
        var radius = position.LengthSquared() > 1e-8f
            ? MathF.Sqrt(position.X * position.X + position.Z * position.Z)
            : (float)global::System.Math.Abs(repeat.Spacing);
        var centerY = position.Y;
        var baseAngle = position.LengthSquared() > 1e-8f
            ? MathF.Atan2(position.Z, position.X)
            : 0f;
        var radial = baseAngle + angle;
        var radialPosition = new Vector3(
            MathF.Cos(radial) * radius,
            centerY,
            MathF.Sin(radial) * radius);
        return (
            radialPosition,
            rotation + new Vector3(0, angle * 180f / MathF.PI, 0));
    }

    private static int ResolveCount(
        AssetGeometryRepeat repeat,
        AssetRecipe asset,
        AssetInstance? instance)
    {
        var value = repeat.CountParameterId is null
            ? repeat.FixedCount
            : (int)global::System.Math.Round(
                ResolveScalar(
                    repeat.CountParameterId,
                    asset,
                    instance,
                    repeat.FixedCount));
        return global::System.Math.Clamp(
            value,
            repeat.MinimumCount,
            repeat.MaximumCount);
    }

    private static Vector3 ResolveVector(
        IReadOnlyList<double> values,
        IEnumerable<AssetGeometryParameterBinding> bindings,
        AssetRecipe asset,
        AssetInstance? instance)
    {
        var result = ToVector(values);
        foreach (var binding in bindings)
        {
            result[binding.Component] =
                (float)(ResolveScalar(
                            binding.ParameterId,
                            asset,
                            instance,
                            result[binding.Component]) *
                        binding.Multiplier +
                        binding.Offset);
        }

        return result;
    }

    private static double ResolveScalar(
        string parameterId,
        AssetRecipe asset,
        AssetInstance? instance,
        double fallback)
    {
        var value = ResolveValue(parameterId, asset, instance);
        return value switch
        {
            ScalarValue scalar => scalar.Value,
            IntegerValue integer => integer.Value,
            _ => fallback,
        };
    }

    private static string ResolveText(
        string? parameterId,
        AssetRecipe asset,
        AssetInstance? instance) =>
        parameterId is null
            ? string.Empty
            : ResolveValue(parameterId, asset, instance) is TextValue text
                ? text.Value
                : string.Empty;

    private static VisualValue? ResolveValue(
        string parameterId,
        AssetRecipe asset,
        AssetInstance? instance)
    {
        var parameter = asset.FindParameter(parameterId);
        if (parameter is null)
        {
            return null;
        }

        if (instance is not null &&
            string.Equals(
                instance.Parent.AssetId,
                asset.AssetId,
                StringComparison.Ordinal) &&
            instance.Overrides.TryGetValue(parameterId, out var overrideValue))
        {
            return overrideValue;
        }

        return parameter.DefaultValue;
    }

    private static MeshPrimitiveKind ToModelingPrimitive(
        AssetGeometryPrimitive primitive) =>
        primitive switch
        {
            AssetGeometryPrimitive.Box => MeshPrimitiveKind.Box,
            AssetGeometryPrimitive.Sphere => MeshPrimitiveKind.Sphere,
            AssetGeometryPrimitive.Plane => MeshPrimitiveKind.Plane,
            AssetGeometryPrimitive.Cylinder => MeshPrimitiveKind.Cylinder,
            AssetGeometryPrimitive.Cone => MeshPrimitiveKind.Cone,
            AssetGeometryPrimitive.Capsule => MeshPrimitiveKind.Capsule,
            AssetGeometryPrimitive.Torus => MeshPrimitiveKind.Torus,
            AssetGeometryPrimitive.Pyramid => MeshPrimitiveKind.Pyramid,
            AssetGeometryPrimitive.Disc => MeshPrimitiveKind.Disc,
            AssetGeometryPrimitive.Tube => MeshPrimitiveKind.Tube,
            _ => MeshPrimitiveKind.Box,
        };

    private static Vector3 AxisVector(AssetGeometryAxis axis) =>
        axis switch
        {
            AssetGeometryAxis.Y => Vector3.UnitY,
            AssetGeometryAxis.Z => Vector3.UnitZ,
            _ => Vector3.UnitX,
        };

    private static Vector3 ToVector(IReadOnlyList<double> values) =>
        new((float)values[0], (float)values[1], (float)values[2]);

    private static string SourceKey(VisualPortReference source) =>
        $"{source.NodeId}:{source.PortId.Value}";

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static (Vector3 Minimum, Vector3 Maximum) Bounds(
        IReadOnlyList<EvaluatedMesh> meshes)
    {
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        foreach (var mesh in meshes)
        {
            foreach (var vertex in mesh.Vertices)
            {
                var world = Vector3.Transform(vertex, mesh.World);
                minimum = Vector3.Min(minimum, world);
                maximum = Vector3.Max(maximum, world);
            }
        }

        return float.IsPositiveInfinity(minimum.X)
            ? (Vector3.Zero, Vector3.One)
            : (minimum, maximum);
    }

    private static IReadOnlyList<LightDefinition> DefaultLights() =>
    [
        new(
            Novolis.Rendering.Scene.LightKind.Directional,
            Vector3.Normalize(new Vector3(-0.45f, -0.7f, -0.35f)),
            new Vector3(1f, 0.94f, 0.86f),
            3.5f),
        new(
            Novolis.Rendering.Scene.LightKind.Point,
            new Vector3(-3f, 5f, 4f),
            new Vector3(0.5f, 0.7f, 1f),
            90f),
        new(
            Novolis.Rendering.Scene.LightKind.Point,
            new Vector3(4f, 2f, -3f),
            new Vector3(1f, 0.35f, 0.12f),
            45f),
    ];

    private static VisualCapabilityStatus AggregateStatus(
        VisualCapabilityStatus conversionStatus,
        IEnumerable<VisualRenderingDiagnostic> diagnostics)
    {
        if (conversionStatus == VisualCapabilityStatus.Unsupported ||
            diagnostics.Any(diagnostic =>
                diagnostic.Severity == VisualCompileDiagnosticSeverity.Error))
        {
            return VisualCapabilityStatus.Unsupported;
        }

        return conversionStatus;
    }

    private sealed record GeometrySpec(
        string Suffix,
        string Name,
        MeshPrimitiveKind Primitive,
        Vector3 Size,
        Vector3 Position,
        Vector3 Rotation);
}
