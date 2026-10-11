using Novolis.Visual.Definitions;

namespace Novolis.Visual.Definitions.Unit;

public sealed class VisualRecipeContractTests
{
    [Test]
    public async Task Canonical_json_is_independent_of_input_collection_order()
    {
        var scalarContract = new VisualTypeContract(
            VisualValueKind.Scalar,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.None);
        var source = VisualNode.Create(
            Node("00000000000000000000000000000001"),
            VisualBuiltInNodeTypes.ConstantScalar,
            1,
            [new KeyValuePair<string, VisualValue>("value", new ScalarValue(2.5))]);
        var target = VisualNode.Create(
            Node("00000000000000000000000000000002"),
            VisualBuiltInNodeTypes.AddScalar,
            1);
        var connection = new VisualConnection(
            Connection("00000000000000000000000000000003"),
            new VisualPortReference(source.Id, new PortId("value")),
            new VisualPortReference(target.Id, new PortId("left")),
            scalarContract,
            scalarContract);
        var parameter = new VisualParameter(
            "amount",
            "Amount",
            scalarContract,
            new ScalarValue(2.5));

        var first = new VisualRecipe(
            "canonical",
            nodes: [target, source],
            connections: [connection],
            parameters: [parameter],
            outputs:
            [
                new VisualOutput(
                    "result",
                    "Result",
                    new VisualPortReference(target.Id, new PortId("result")),
                    scalarContract),
            ],
            seedPolicy: new VisualSeedPolicy(VisualSeedMode.Fixed, 42),
            dependencies:
            [
                new VisualDependencyFingerprint("texture", "sha256", "abc"),
            ],
            capabilities:
            [
                new VisualCapabilityRequirement("cpu"),
            ]);
        var second = new VisualRecipe(
            "canonical",
            nodes: [source, target],
            connections: [connection],
            parameters: [parameter],
            outputs:
            [
                new VisualOutput(
                    "result",
                    "Result",
                    new VisualPortReference(target.Id, new PortId("result")),
                    scalarContract),
            ],
            seedPolicy: new VisualSeedPolicy(VisualSeedMode.Fixed, 42),
            dependencies:
            [
                new VisualDependencyFingerprint("texture", "sha256", "abc"),
            ],
            capabilities:
            [
                new VisualCapabilityRequirement("cpu"),
            ]);

        var firstJson = VisualRecipeSerializer.Serialize(first);
        var secondJson = VisualRecipeSerializer.Serialize(second);

        await Assert.That(firstJson).IsEqualTo(secondJson);
        await Assert.That(first.SemanticFingerprint())
            .IsEqualTo(second.SemanticFingerprint());
        await Assert.That(firstJson).DoesNotContain("viewState");

        var roundTripped = VisualRecipeSerializer.Deserialize(firstJson);
        await Assert.That(roundTripped.Nodes[0].Id)
            .IsEqualTo(Node("00000000000000000000000000000001"));
        await Assert.That(roundTripped.SemanticFingerprint())
            .IsEqualTo(first.SemanticFingerprint());
    }

    [Test]
    public async Task Schema_zero_is_migrated_and_legacy_links_are_preserved()
    {
        var legacy = """
            {
              "schemaVersion": 0,
              "id": "legacy-recipe",
              "nodes": [
                {
                  "id": "00000000000000000000000000000011",
                  "nodeType": "novolis.visual.constant.scalar",
                  "values": {
                    "value": {
                      "kind": "Scalar",
                      "unit": "Unitless",
                      "value": 3
                    }
                  }
                }
              ],
              "links": []
            }
            """;

        var migrated = VisualSchemaMigrationRegistry.CreateDefault().Upgrade(legacy);
        var recipe = VisualRecipeSerializer.Deserialize(legacy);

        await Assert.That(migrated).Contains("\"schemaVersion\":1");
        await Assert.That(recipe.SchemaVersion)
            .IsEqualTo(VisualRecipe.CurrentSchemaVersion);
        await Assert.That(recipe.RecipeId).IsEqualTo("legacy-recipe");
        await Assert.That(recipe.Nodes.Single().IsUnknown).IsFalse();
        await Assert.That(((ScalarValue)recipe.Nodes.Single().Values["value"]).Value)
            .IsEqualTo(3d);
    }

    [Test]
    public async Task Unknown_nodes_round_trip_without_interpretation()
    {
        var source = """
            {
              "schemaVersion": 1,
              "recipeId": "future",
              "nodes": [
                {
                  "version": 7,
                  "type": "vendor.future.noise",
                  "id": "00000000000000000000000000000021",
                  "future": {
                    "enabled": true,
                    "samples": [2, 1]
                  }
                }
              ],
              "connections": [],
              "dependencies": [],
              "parameters": [],
              "outputs": [],
              "capabilities": [],
              "seedPolicy": {
                "mode": "None",
                "scope": null,
                "seed": 0
              }
            }
            """;

        var recipe = VisualRecipeSerializer.Deserialize(source);
        var canonical = VisualRecipeSerializer.Serialize(recipe);
        var expected = VisualSchemaMigrationRegistry.CreateDefault().Upgrade(source);

        await Assert.That(recipe.Nodes.Single().IsUnknown).IsTrue();
        await Assert.That(recipe.Nodes.Single().Id)
            .IsEqualTo(Node("00000000000000000000000000000021"));
        await Assert.That(canonical).IsEqualTo(expected);
        await Assert.That(
                VisualRecipeSerializer.Deserialize(canonical).Nodes.Single().IsUnknown)
            .IsTrue();
    }

    [Test]
    public async Task Values_round_trip_with_units_spaces_and_field_metadata()
    {
        var values = new List<KeyValuePair<string, VisualValue>>
        {
            new("scalar", new ScalarValue(1.25, VisualUnit.Meters)),
            new("integer", new IntegerValue(4, VisualUnit.Frames)),
            new("boolean", new BooleanValue(true)),
            new("color", new ColorValue(0.1, 0.2, 0.3, 1, ColorSpace.DisplayP3)),
            new(
                "vector",
                VectorValue.Create3(1, 2, 3, VisualUnit.Meters, CoordinateSpace.World)),
            new("transform", TransformValue.Identity(CoordinateSpace.World)),
            new(
                "image",
                new ImageValue(
                    "image.asset",
                    64,
                    32,
                    ImageFormat.Rgba8,
                    ColorSpace.Srgb,
                    AlphaMode.Straight)),
            new(
                "geometry",
                new GeometryValue(
                    "mesh.asset",
                    GeometryKind.Mesh,
                    CoordinateSpace.Object,
                    VisualUnit.Meters)),
            new(
                "surface",
                new SurfaceValue(
                    "surface.default",
                    new ColorValue(1, 0, 0),
                    0.4,
                    0.2)),
            new(
                "light",
                new LightValue(
                    LightKind.Point,
                    new ColorValue(1, 1, 1),
                    10,
                    VisualUnit.Unitless,
                    TransformValue.Identity(CoordinateSpace.World))),
            new(
                "volume",
                new VolumeValue(
                    "volume.default",
                    VolumeKind.Homogeneous,
                    0.8,
                    VisualUnit.Meters,
                    new ColorValue(0.8, 0.9, 1),
                    0.1)),
            new(
                "effect",
                new EffectValue(
                    "bloom",
                    [new KeyValuePair<string, VisualValue>("threshold", new ScalarValue(1))])),
            new("scene", new SceneValue("scene.asset", ["b", "a"])),
            new("frame", new FrameValue(12, 0.5, 0.016, 60)),
        };
        var recipe = new VisualRecipe(
            "typed-values",
            [
                VisualNode.Create(
                    Node("00000000000000000000000000000031"),
                    VisualBuiltInNodeTypes.ConstantScalar,
                    1,
                    values),
            ]);

        var roundTripped = VisualRecipeSerializer.Deserialize(
            VisualRecipeSerializer.Serialize(recipe));
        var roundTrippedValues = roundTripped.Nodes.Single().Values.Values;

        await Assert.That(roundTrippedValues.Count).IsEqualTo(values.Count);
        await Assert.That(roundTrippedValues.Select(value => value.Kind))
            .Contains(VisualValueKind.Transform);
        await Assert.That(((VectorValue)roundTripped.Nodes.Single().Values["vector"])
                .CoordinateSpace)
            .IsEqualTo(CoordinateSpace.World);
        await Assert.That(((ImageValue)roundTripped.Nodes.Single().Values["image"])
                .ColorSpace)
            .IsEqualTo(ColorSpace.Srgb);
    }

    [Test]
    public async Task Registry_allowlists_nodes_and_contracts_reject_mismatches()
    {
        var registry = VisualNodeRegistry.CreateDefault();
        var meters = new VisualTypeContract(
            VisualValueKind.Scalar,
            VisualUnit.Meters,
            CoordinateSpace.None,
            ColorSpace.None,
            FieldFrequency.Vertex,
            FieldPurity.DeterministicWithSeed,
            VisualAttributeRequirement.Position);
        var seconds = new VisualTypeContract(
            VisualValueKind.Scalar,
            VisualUnit.Seconds,
            CoordinateSpace.None,
            ColorSpace.None,
            FieldFrequency.Vertex,
            FieldPurity.DeterministicWithSeed,
            VisualAttributeRequirement.Position);

        await Assert.That(registry.IsAllowed(VisualBuiltInNodeTypes.AddScalar))
            .IsTrue();
        await Assert.That(registry.IsAllowed("vendor.not-registered")).IsFalse();
        await Assert.That(meters.IsCompatibleWith(seconds)).IsFalse();
        await Assert.That(meters.Frequency).IsEqualTo(FieldFrequency.Vertex);
        await Assert.That(meters.Purity)
            .IsEqualTo(FieldPurity.DeterministicWithSeed);
        await Assert.That(meters.Attributes)
            .IsEqualTo(VisualAttributeRequirement.Position);

        var unknown = VisualRecipeSerializer.Deserialize(
            """
            {
              "schemaVersion": 1,
              "recipeId": "allowlist",
              "nodes": [
                {
                  "id": "00000000000000000000000000000041",
                  "type": "vendor.not-registered",
                  "version": 1
                }
              ]
            }
            """);

        var validation = registry.Validate(unknown);
        await Assert.That(validation.IsValid).IsFalse();
        await Assert.That(validation.Diagnostics.Select(diagnostic => diagnostic.Code))
            .Contains("VIS001");
    }

    private static NodeId Node(string value) => NodeId.Parse(value);

    private static ConnectionId Connection(string value) => ConnectionId.Parse(value);
}
