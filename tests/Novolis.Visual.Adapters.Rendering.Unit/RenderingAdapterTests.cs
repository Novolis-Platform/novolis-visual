using System.Numerics;
using Novolis.Modeling;
using Novolis.Rendering.Appearance;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Scene;
using Novolis.Visual.Adapters.Rendering;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering.Unit;

public sealed class RenderingAdapterTests
{
    [Test]
    public async Task Mesh_readiness_separates_geometry_from_appearance()
    {
        var mesh = CreateMesh(withAppearanceAttributes: false);

        var readiness = VisualRenderingAdapter.InspectMesh(mesh);
        var converted = VisualRenderingAdapter.ConvertMesh(
            mesh,
            new StandardMaterial());

        await Assert.That(readiness.IsGeometryReady).IsTrue();
        await Assert.That(readiness.IsAppearanceReady).IsFalse();
        await Assert.That(readiness.Appearance)
            .IsEqualTo(MeshReadinessState.Unavailable);
        await Assert.That(converted.Mesh).IsNotNull();
        await Assert.That(converted.Status)
            .IsEqualTo(VisualCapabilityStatus.Exact);
        await Assert.That(converted.Diagnostics.Select(diagnostic => diagnostic.Code))
            .Contains("VRA303");
    }

    [Test]
    public async Task Mesh_conversion_copies_renderer_contract_and_transform()
    {
        var mesh = CreateMesh(withAppearanceAttributes: true);
        var material = new StandardMaterial
        {
            BaseColor = new Vector3(0.2f, 0.3f, 0.4f),
        };

        var converted = VisualRenderingAdapter.ConvertMesh(mesh, material);

        await Assert.That(converted.Mesh).IsNotNull();
        await Assert.That(converted.Mesh!.Vertices)
            .IsNotSameReferenceAs(mesh.Vertices);
        await Assert.That(converted.Mesh.TriangleIndices)
            .IsNotSameReferenceAs(mesh.Indices);
        await Assert.That(converted.Mesh.Transform)
            .IsEqualTo(mesh.World);
        await Assert.That(converted.Mesh.Material).IsEqualTo(material);
        await Assert.That(converted.Readiness.IsAppearanceReady).IsTrue();
    }

    [Test]
    public async Task Constant_linear_surface_lowers_exactly_to_standard_material()
    {
        var surface = new SurfaceValue(
            "surface.asset",
            new ColorValue(0.7, 0.25, 0.1, 1, ColorSpace.LinearSrgb),
            0.32,
            0.78);

        var lowered = VisualRenderingAdapter.LowerSurface(surface);
        var material = lowered.Material as StandardMaterial;

        await Assert.That(lowered.Status)
            .IsEqualTo(VisualCapabilityStatus.Exact);
        await Assert.That(material).IsNotNull();
        await Assert.That(material!.BaseColor)
            .IsEqualTo(new Vector3(0.7f, 0.25f, 0.1f));
        await Assert.That(material.Roughness).IsEqualTo(0.32f);
        await Assert.That(material.Metallic).IsEqualTo(0.78f);
    }

    [Test]
    public async Task Surface_alpha_is_not_dropped()
    {
        var surface = new SurfaceValue(
            "transparent",
            new ColorValue(1, 1, 1, 0.5),
            0.5,
            0);

        var lowered = VisualRenderingAdapter.LowerSurface(surface);

        await Assert.That(lowered.Status)
            .IsEqualTo(VisualCapabilityStatus.Unsupported);
        await Assert.That(lowered.Material).IsNull();
        await Assert.That(lowered.Diagnostics.Select(diagnostic => diagnostic.Code))
            .Contains("VRA210");
    }

    [Test]
    public async Task Constant_appearance_stack_uses_reference_evaluator()
    {
        var stack = AppearanceStack.GreySurface()
            .Then(
                AppearanceOp.BaseColor,
                amount: 1f,
                color: new Novolis.Math.Geometry.Rgba32(200, 120, 40))
            .Then(AppearanceOp.Roughness, amount: 0.22f)
            .Then(AppearanceOp.Metallic, amount: 0.65f);

        var lowered = VisualRenderingAdapter.LowerAppearance(stack);
        var material = lowered.Material as StandardMaterial;
        var expected = AppearanceEvaluator.EvaluateSurface(stack, default);

        await Assert.That(lowered.Status)
            .IsEqualTo(VisualCapabilityStatus.Exact);
        await Assert.That(material).IsNotNull();
        await Assert.That(lowered.EvaluatedSurface).IsEqualTo(expected);
        await Assert.That(material!.BaseColor)
            .IsEqualTo(new Vector3(200f / 255f, 120f / 255f, 40f / 255f));
        await Assert.That(material.Roughness).IsEqualTo(expected.Roughness);
        await Assert.That(material.Metallic).IsEqualTo(expected.Metallic);
    }

    [Test]
    public async Task Procedural_appearance_reports_unsupported_or_bake_required()
    {
        var stack = AppearanceRecipes.SteelPanels(seed: 17);

        var unsupported = VisualRenderingAdapter.LowerAppearance(stack);
        var baked = VisualRenderingAdapter.LowerAppearance(
            stack,
            new RenderingAdapterOptions { AllowBakedFallback = true });

        await Assert.That(unsupported.Status)
            .IsEqualTo(VisualCapabilityStatus.Unsupported);
        await Assert.That(unsupported.Material).IsNull();
        await Assert.That(baked.Status)
            .IsEqualTo(VisualCapabilityStatus.BakedFallback);
        await Assert.That(baked.RequiresBake).IsTrue();
        await Assert.That(baked.Material).IsNull();
        await Assert.That(baked.Diagnostics.Select(diagnostic => diagnostic.Code))
            .Contains("VRA223");
    }

    [Test]
    public async Task Equivalent_inputs_have_deterministic_fingerprints_and_cache_keys()
    {
        var first = new SurfaceValue(
            "same",
            new ColorValue(0.1, 0.2, 0.3),
            0.4,
            0.5);
        var second = new SurfaceValue(
            "same",
            new ColorValue(0.1, 0.2, 0.3),
            0.4,
            0.5);
        var changed = new SurfaceValue(
            "same",
            new ColorValue(0.1, 0.2, 0.31),
            0.4,
            0.5);

        var firstResult = VisualRenderingAdapter.LowerSurface(first);
        var secondResult = VisualRenderingAdapter.LowerSurface(second);
        var changedResult = VisualRenderingAdapter.LowerSurface(changed);

        await Assert.That(firstResult.Fingerprint)
            .IsEqualTo(secondResult.Fingerprint);
        await Assert.That(firstResult.CacheKey)
            .IsEqualTo(secondResult.CacheKey);
        await Assert.That(firstResult.Fingerprint)
            .IsNotEqualTo(changedResult.Fingerprint);
        await Assert.That(firstResult.CacheKey)
            .IsNotEqualTo(changedResult.CacheKey);
    }

    [Test]
    public async Task Visual_ir_lowering_keeps_source_and_capability_diagnostics()
    {
        var id = NodeId.Parse("00000000000000000000000000000001");
        var surface = new SurfaceValue(
            "source",
            new ColorValue(0.8, 0.8, 0.8),
            0.5,
            0);
        var contract = surface.Contract;
        var node = new VisualIRNode(
            id,
            "constant.surface",
            "constant.surface",
            contract,
            constantValue: surface);
        var output = new VisualIROutput(
            "surface",
            "Surface",
            id,
            new PortId("value"),
            contract);
        var ir = new VisualIR(
            "source-recipe",
            1,
            new VisualSeedPolicy(),
            [node],
            [output],
            sourceMap: new VisualSourceMap(
            [
                new VisualSourceMapEntry(
                    "nodes/00000000000000000000000000000001",
                    nodeId: id,
                    typeId: "constant.surface"),
                new VisualSourceMapEntry(
                    "outputs/surface",
                    outputId: "surface",
                    portId: "value"),
            ]),
            capabilities: new VisualCapabilityReport(
            [
                new VisualCapabilityResult(
                    "visual.procedural.missing",
                    VisualCapabilityStatus.Unsupported,
                    required: true,
                    message: "Required target capability is missing."),
            ]));

        var lowered = VisualRenderingAdapter.LowerConstantSurface(ir, "surface");

        await Assert.That(lowered.Status)
            .IsEqualTo(VisualCapabilityStatus.Unsupported);
        await Assert.That(lowered.Diagnostics.Any(diagnostic =>
            diagnostic.Code == "VRA420" &&
            diagnostic.SourcePath == "outputs/surface" &&
            diagnostic.CapabilityId == "visual.procedural.missing"))
            .IsTrue();
        await Assert.That(lowered.Diagnostics.Any(diagnostic =>
            diagnostic.NodeId == id &&
            diagnostic.OutputId == "surface"))
            .IsTrue();
    }

    [Test]
    public async Task Scene_conversion_can_delegate_to_existing_scene_compiler()
    {
        var mesh = CreateMesh(withAppearanceAttributes: true);
        var converted = VisualRenderingAdapter.ConvertScene(
        [
            new RenderingMeshInput(
                mesh,
                new StandardMaterial(),
                "nodes/mesh"),
        ],
        [
            new LightDefinition(
                Novolis.Rendering.Scene.LightKind.Directional,
                Vector3.Normalize(new Vector3(0, -1, 0)),
                Vector3.One,
                2f),
        ]);

        var compiled = RenderingSceneCompilerAdapter.Compile(converted);

        await Assert.That(converted.Scene).IsNotNull();
        await Assert.That(compiled.Scene).IsNotNull();
        await Assert.That(compiled.Scene!.Triangles.Length).IsEqualTo(1);
    }

    private static EvaluatedMesh CreateMesh(bool withAppearanceAttributes) =>
        new()
        {
            SourceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Vertices =
            [
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
            ],
            Indices = [0, 1, 2],
            World = Matrix4x4.CreateTranslation(2, 3, 4),
            Normals = withAppearanceAttributes
                ? [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]
                : null,
            UVs = withAppearanceAttributes
                ? [Vector2.Zero, Vector2.UnitX, Vector2.UnitY]
                : null,
        };
}
