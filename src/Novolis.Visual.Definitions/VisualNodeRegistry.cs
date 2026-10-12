using System.Collections.Immutable;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Immutable allowlist of node descriptors. The registry describes what may be
/// interpreted; it never instantiates arbitrary types from recipe data.
/// </summary>
public sealed class VisualNodeRegistry
{
    private readonly ImmutableSortedDictionary<string, VisualNodeDescriptor> descriptors;

    public VisualNodeRegistry(IEnumerable<VisualNodeDescriptor>? descriptors = null)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<
            string,
            VisualNodeDescriptor>(StringComparer.Ordinal);

        foreach (var descriptor in descriptors ?? [])
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            if (!builder.TryAdd(descriptor.TypeId, descriptor))
            {
                throw new ArgumentException(
                    $"The node type '{descriptor.TypeId}' is already registered.",
                    nameof(descriptors));
            }
        }

        this.descriptors = builder.ToImmutable();
    }

    public ImmutableArray<VisualNodeDescriptor> Descriptors =>
        descriptors.Values.ToImmutableArray();

    public bool IsAllowed(string typeId) =>
        typeId is not null && descriptors.ContainsKey(typeId);

    public bool TryGetDescriptor(
        string typeId,
        out VisualNodeDescriptor? descriptor) =>
        descriptors.TryGetValue(typeId, out descriptor);

    public VisualValidationResult Validate(VisualRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        return VisualRecipeValidator.Validate(recipe, this);
    }

    public static VisualNodeRegistry CreateDefault() =>
        new(
        [
            new VisualNodeDescriptor(
                VisualBuiltInNodeTypes.ConstantScalar,
                1,
                "Scalar constant",
                ports:
                [
                    new VisualPortDescriptor(
                        new PortId("value"),
                        PortDirection.Output,
                        new VisualTypeContract(
                            VisualValueKind.Scalar,
                            VisualUnit.Unitless,
                            CoordinateSpace.None,
                            ColorSpace.None)),
                ],
                parameters:
                [
                    new VisualParameterDescriptor(
                        "value",
                        new VisualTypeContract(
                            VisualValueKind.Scalar,
                            VisualUnit.Unitless,
                            CoordinateSpace.None,
                            ColorSpace.None),
                        new ScalarValue(0),
                        isExposed: true),
                ]),
            new VisualNodeDescriptor(
                VisualBuiltInNodeTypes.ConstantColor,
                1,
                "Color constant",
                ports:
                [
                    new VisualPortDescriptor(
                        new PortId("value"),
                        PortDirection.Output,
                        new VisualTypeContract(
                            VisualValueKind.Color,
                            VisualUnit.Unitless,
                            CoordinateSpace.None,
                            ColorSpace.LinearSrgb)),
                ],
                parameters:
                [
                    new VisualParameterDescriptor(
                        "value",
                        new VisualTypeContract(
                            VisualValueKind.Color,
                            VisualUnit.Unitless,
                            CoordinateSpace.None,
                            ColorSpace.LinearSrgb),
                        new ColorValue(1, 1, 1),
                        isExposed: true),
                ]),
            ConstantDescriptor(
                VisualBuiltInNodeTypes.ConstantVector,
                "Vector constant",
                VectorValue.Create3(0, 0, 0)),
            ConstantDescriptor(
                VisualBuiltInNodeTypes.ConstantMaterial,
                "Material constant",
                new MaterialValue(
                    "material.default",
                    VisualMaterialModel.StandardPbr,
                    new ColorValue(0.75, 0.75, 0.78),
                    roughness: 0.45)),
            ConstantDescriptor(
                VisualBuiltInNodeTypes.ConstantGeometry,
                "Geometry module",
                new GeometryValue(
                    "geometry.default",
                    GeometryKind.Mesh,
                    CoordinateSpace.Object,
                    VisualUnit.Meters)),
            new VisualNodeDescriptor(
                VisualBuiltInNodeTypes.AddScalar,
                1,
                "Add scalars",
                ports:
                [
                    new VisualPortDescriptor(
                        new PortId("left"),
                        PortDirection.Input,
                        new VisualTypeContract(
                            VisualValueKind.Scalar,
                            VisualUnit.Unitless,
                            CoordinateSpace.None,
                            ColorSpace.None)),
                    new VisualPortDescriptor(
                        new PortId("right"),
                        PortDirection.Input,
                        new VisualTypeContract(
                            VisualValueKind.Scalar,
                            VisualUnit.Unitless,
                            CoordinateSpace.None,
                            ColorSpace.None)),
                    new VisualPortDescriptor(
                        new PortId("result"),
                        PortDirection.Output,
                        new VisualTypeContract(
                            VisualValueKind.Scalar,
                            VisualUnit.Unitless,
                            CoordinateSpace.None,
                            ColorSpace.None)),
                ]),
            BinaryDescriptor(
                VisualBuiltInNodeTypes.MultiplyScalar,
                "Multiply scalars",
                new VisualTypeContract(
                    VisualValueKind.Scalar,
                    VisualUnit.Unitless,
                    CoordinateSpace.None,
                    ColorSpace.None)),
            NoiseDescriptor(
                VisualBuiltInNodeTypes.NoiseScalar,
                "Deterministic scalar noise"),
            MaterialOutputDescriptor(),
            new VisualNodeDescriptor(
                VisualBuiltInNodeTypes.FrameOutput,
                1,
                "Frame output",
                ports:
                [
                    new VisualPortDescriptor(
                        new PortId("frame"),
                        PortDirection.Output,
                        new VisualTypeContract(
                            VisualValueKind.Frame,
                            VisualUnit.Frames,
                            CoordinateSpace.None,
                            ColorSpace.None,
                            FieldFrequency.Frame)),
                ]),
            ConstantDescriptor(
                VisualBuiltInNodeTypes.ConstantLight,
                "Light definition",
                new LightValue(
                    LightKind.Point,
                    new ColorValue(1, 1, 1),
                    1,
                    VisualUnit.Unitless,
                    TransformValue.Identity(CoordinateSpace.World))),
            ConstantDescriptor(
                VisualBuiltInNodeTypes.ConstantVolume,
                "Volume definition",
                new VolumeValue(
                    "volume",
                    VolumeKind.Homogeneous,
                    0,
                    VisualUnit.Unitless,
                    new ColorValue(1, 1, 1),
                    0)),
            ConstantDescriptor(
                VisualBuiltInNodeTypes.ConstantEffect,
                "Effect assembly",
                new EffectValue("effect")),
            ConstantDescriptor(
                VisualBuiltInNodeTypes.ConstantScene,
                "Scene assembly",
                new SceneValue("scene")),
            ConstantDescriptor(
                VisualBuiltInNodeTypes.ConstantEmitter,
                "Particle emitter",
                new EmitterValue(
                    "emitter",
                    1,
                    0,
                    1,
                    System.Numerics.Vector3.Zero,
                    new ColorValue(1, 1, 1))),
            ConstantDescriptor(
                VisualBuiltInNodeTypes.ConstantDecal,
                "Decal",
                new DecalValue(
                    "decal",
                    DecalProjection.Planar,
                    new System.Numerics.Vector3(1, 1, 1),
                    new ColorValue(1, 1, 1),
                    1)),
            ConstantDescriptor(
                VisualBuiltInNodeTypes.ConstantFrameEffect,
                "Frame effect",
                new FrameEffectValue(FrameEffectKind.Bloom)),
        ]);

    private static VisualNodeDescriptor ConstantDescriptor(
        string typeId,
        string displayName,
        VisualValue defaultValue) =>
        new(
            typeId,
            1,
            displayName,
            ports:
            [
                new VisualPortDescriptor(
                    new PortId("value"),
                    PortDirection.Output,
                    defaultValue.Contract),
            ],
            parameters:
            [
                new VisualParameterDescriptor(
                    "value",
                    defaultValue.Contract,
                    defaultValue,
                    isExposed: true),
            ]);

    private static VisualNodeDescriptor BinaryDescriptor(
        string typeId,
        string displayName,
        VisualTypeContract contract) =>
        new(
            typeId,
            1,
            displayName,
            ports:
            [
                new VisualPortDescriptor(
                    new PortId("left"),
                    PortDirection.Input,
                    contract),
                new VisualPortDescriptor(
                    new PortId("right"),
                    PortDirection.Input,
                    contract),
                new VisualPortDescriptor(
                    new PortId("result"),
                    PortDirection.Output,
                    contract),
            ]);

    private static VisualNodeDescriptor NoiseDescriptor(
        string typeId,
        string displayName) =>
        new(
            typeId,
            1,
            displayName,
            ports:
            [
                new VisualPortDescriptor(
                    new PortId("result"),
                    PortDirection.Output,
                    new VisualTypeContract(
                        VisualValueKind.Scalar,
                        VisualUnit.Unitless,
                        CoordinateSpace.Object,
                        ColorSpace.None,
                        FieldFrequency.Vertex,
                        FieldPurity.DeterministicWithSeed,
                        VisualAttributeRequirement.Position)),
            ]);

    private static VisualNodeDescriptor MaterialOutputDescriptor()
    {
        var scalar = new VisualTypeContract(
            VisualValueKind.Scalar,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.None);
        var color = new VisualTypeContract(
            VisualValueKind.Color,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.LinearSrgb);
        var material = new VisualTypeContract(
            VisualValueKind.Material,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.None);

        return new VisualNodeDescriptor(
            VisualBuiltInNodeTypes.MaterialOutput,
            1,
            "Material output",
            ports:
            [
                new VisualPortDescriptor(
                    new PortId("baseColor"),
                    PortDirection.Input,
                    color,
                    required: false),
                new VisualPortDescriptor(
                    new PortId("metallic"),
                    PortDirection.Input,
                    scalar,
                    required: false),
                new VisualPortDescriptor(
                    new PortId("roughness"),
                    PortDirection.Input,
                    scalar,
                    required: false),
                new VisualPortDescriptor(
                    new PortId("emissionColor"),
                    PortDirection.Input,
                    color,
                    required: false),
                new VisualPortDescriptor(
                    new PortId("emissionStrength"),
                    PortDirection.Input,
                    scalar,
                    required: false),
                new VisualPortDescriptor(
                    new PortId("transmission"),
                    PortDirection.Input,
                    scalar,
                    required: false),
                new VisualPortDescriptor(
                    new PortId("ior"),
                    PortDirection.Input,
                    scalar,
                    required: false),
                new VisualPortDescriptor(
                    new PortId("absorption"),
                    PortDirection.Input,
                    scalar,
                    required: false),
                new VisualPortDescriptor(
                    new PortId("clearcoat"),
                    PortDirection.Input,
                    scalar,
                    required: false),
                new VisualPortDescriptor(
                    new PortId("anisotropy"),
                    PortDirection.Input,
                    scalar,
                    required: false),
                new VisualPortDescriptor(
                    new PortId("alpha"),
                    PortDirection.Input,
                    scalar,
                    required: false),
                new VisualPortDescriptor(
                    new PortId("material"),
                    PortDirection.Output,
                    material),
            ]);
    }
}
