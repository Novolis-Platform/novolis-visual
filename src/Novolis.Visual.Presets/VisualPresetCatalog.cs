using System.Collections.Immutable;
using Novolis.Visual.Assets;
using Novolis.Visual.Definitions;
using Novolis.Visual.Materials;

namespace Novolis.Visual.Presets;

/// <summary>
/// Built-in no-code templates and the material modules they use.
/// </summary>
public sealed class VisualPresetCatalog
{
    private readonly ImmutableArray<VisualPresetTemplate> templates;

    public VisualPresetCatalog(IEnumerable<VisualPresetTemplate> templates)
    {
        ArgumentNullException.ThrowIfNull(templates);
        this.templates = templates
            .OrderBy(template => template.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(template => template.Id, StringComparer.Ordinal)
            .ToImmutableArray();
        if (this.templates
            .Select(template => template.Id)
            .Distinct(StringComparer.Ordinal)
            .Count() != this.templates.Length)
        {
            throw new ArgumentException(
                "Preset identifiers must be unique.",
                nameof(templates));
        }
    }

    public ImmutableArray<VisualPresetTemplate> Templates => templates;

    public ImmutableArray<MaterialGraph> Materials =>
        templates
            .SelectMany(template => template.Materials)
            .GroupBy(material => material.MaterialId, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(material => material.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(material => material.MaterialId, StringComparer.Ordinal)
            .ToImmutableArray();

    public VisualPresetTemplate? Find(string id) =>
        templates.FirstOrDefault(
            template => string.Equals(
                template.Id,
                id,
                StringComparison.Ordinal));

    public IEnumerable<VisualPresetTemplate> Search(string? query = null)
    {
        var normalized = query?.Trim();
        return templates.Where(
            template => string.IsNullOrWhiteSpace(normalized) ||
                        template.DisplayName.Contains(
                            normalized!,
                            StringComparison.OrdinalIgnoreCase) ||
                        template.Description.Contains(
                            normalized!,
                            StringComparison.OrdinalIgnoreCase) ||
                        template.Tags.Any(
                            tag => tag.Contains(
                                normalized!,
                                StringComparison.OrdinalIgnoreCase)));
    }

    public IEnumerable<MaterialGraph> SearchMaterials(string? query = null) =>
        Materials.Where(
            material => string.IsNullOrWhiteSpace(query) ||
                        material.DisplayName.Contains(
                            query.Trim(),
                            StringComparison.OrdinalIgnoreCase) ||
                        material.MaterialId.Contains(
                            query.Trim(),
                            StringComparison.OrdinalIgnoreCase) ||
                        material.Tags.Any(
                            tag => tag.Contains(
                                query.Trim(),
                                StringComparison.OrdinalIgnoreCase)));

    public static VisualPresetCatalog CreateDefault() =>
        new(
        [
            CreateLightsaberBlade(),
            CreateNeonSign(),
            CreateGlass(),
            CreateOrbitalRocket(),
        ]);

    private static VisualPresetTemplate CreateLightsaberBlade()
    {
        var material = CreateMaterial(
            "material.lightsaber.blade",
            "Lightsaber Blade Glow",
            new ColorValue(0.02, 0.04, 0.08),
            0.16,
            0.05,
            new ColorValue(0.25, 0.75, 1),
            12,
            exposed:
            [
                Exposed.Color(
                    "blade.coreColor",
                    "Core Color",
                    "Blade",
                    new ColorValue(0.85, 0.95, 1),
                    target: "emissionColor"),
                Exposed.Scalar(
                    "blade.intensity",
                    "Glow Intensity",
                    "Blade",
                    12,
                    0,
                    32,
                    target: "emissionStrength"),
            ],
            tags: ["blade", "emissive", "science-fiction"]);
        var asset = CreateAsset(
            "asset.lightsaber.blade",
            "Lightsaber Blade",
            [
                new AssetPart(
                    "blade",
                    "Blade",
                    "geometry.lightsaber.blade",
                    "blade"),
            ],
            [new AssetMaterialSlot(
                "blade",
                "Blade Glow",
                material.MaterialId)],
            [
                AssetParameter.Scalar(
                    "blade.length",
                    "Blade Length",
                    3.2,
                    0.25,
                    12,
                    "Blade"),
                AssetParameter.Scalar(
                    "blade.radius",
                    "Blade Radius",
                    0.08,
                    0.01,
                    0.5,
                    "Blade"),
                AssetParameter.Scalar(
                    "blade.flicker",
                    "Flicker",
                    0.08,
                    0,
                    1,
                    "Effects"),
            ],
            tags: ["asset", "blade", "emissive"],
            features:
            [
                new AssetFeature(
                    "blade.glow",
                    "Bloom Glow",
                    AssetComponentKind.Effect,
                    "blade"),
            ]);
        return new VisualPresetTemplate(
            "lightsaber-blade",
            "Lightsaber Blade",
            "A reusable emissive blade with a hot core, colored rim, and glow controls.",
            asset,
            [material],
            ["asset", "emissive", "procedural"]);
    }

    private static VisualPresetTemplate CreateNeonSign()
    {
        var material = CreateMaterial(
            "material.neon.sign",
            "Neon Tube",
            new ColorValue(0.01, 0.015, 0.02),
            0.2,
            0,
            new ColorValue(1, 0.08, 0.42),
            10,
            exposed:
            [
                Exposed.Color(
                    "sign.color",
                    "Tube Color",
                    "Sign",
                    new ColorValue(1, 0.08, 0.42),
                    target: "emissionColor"),
                Exposed.Scalar(
                    "sign.brightness",
                    "Brightness",
                    "Sign",
                    10,
                    0,
                    40,
                    target: "emissionStrength"),
            ],
            tags: ["emissive", "neon", "sign"]);
        var asset = CreateAsset(
            "asset.neon.sign",
            "Neon Sign",
            [
                new AssetPart(
                    "lettering",
                    "Lettering",
                    "geometry.text.extruded",
                    "tube"),
                new AssetPart(
                    "backing",
                    "Backing",
                    "geometry.sign.backing",
                    "backing"),
            ],
            [
                new AssetMaterialSlot(
                    "tube",
                    "Neon Tube",
                    material.MaterialId),
                new AssetMaterialSlot(
                    "backing",
                    "Backing Material",
                    "material.paint.black"),
            ],
            [
                AssetParameter.Text(
                    "sign.text",
                    "Sign Text",
                    "NOVOLIS",
                    "Sign"),
                AssetParameter.Scalar(
                    "sign.size",
                    "Letter Size",
                    1.5,
                    0.1,
                    10,
                    "Sign"),
                AssetParameter.Scalar(
                    "sign.spacing",
                    "Letter Spacing",
                    0.08,
                    -1,
                    2,
                    "Sign"),
            ],
            tags: ["asset", "text", "emissive"],
            features:
            [
                new AssetFeature(
                    "sign.glow",
                    "Neon Bloom",
                    AssetComponentKind.Effect,
                    "lettering"),
            ]);
        return new VisualPresetTemplate(
            "neon-sign",
            "Neon Sign",
            "A text-driven sign with tube geometry, emissive color, backing, and glow controls.",
            asset,
            [material],
            ["asset", "text", "emissive"]);
    }

    private static VisualPresetTemplate CreateGlass()
    {
        var material = CreateMaterial(
            "material.glass",
            "Clear Glass",
            new ColorValue(0.92, 0.98, 1),
            0.04,
            0,
            new ColorValue(0, 0, 0),
            0,
            transmission: 1,
            ior: 1.5,
            absorption: 0.04,
            exposed:
            [
                Exposed.Color(
                    "glass.tint",
                    "Glass Tint",
                    "Transmission",
                    new ColorValue(0.92, 0.98, 1),
                    target: "baseColor"),
                Exposed.Scalar(
                    "glass.ior",
                    "Index of Refraction",
                    "Transmission",
                    1.5,
                    1,
                    2.5,
                    target: "ior"),
                Exposed.Scalar(
                    "glass.roughness",
                    "Surface Roughness",
                    "Surface",
                    0.04,
                    0,
                    1,
                    target: "roughness"),
                Exposed.Scalar(
                    "glass.absorption",
                    "Absorption",
                    "Transmission",
                    0.04,
                    0,
                    10,
                    target: "absorption"),
            ],
            tags: ["glass", "transmission", "pbr"]);
        var asset = CreateAsset(
            "asset.glass.sample",
            "Glass Sample",
            [
                new AssetPart(
                    "pane",
                    "Sample Pane",
                    "geometry.glass.pane",
                    "glass"),
            ],
            [new AssetMaterialSlot(
                "glass",
                "Glass",
                material.MaterialId)],
            [
                AssetParameter.Scalar(
                    "glass.thickness",
                    "Thickness",
                    0.02,
                    0.001,
                    0.25,
                    "Geometry"),
            ],
            tags: ["asset", "glass", "sample"],
            features:
            [
                new AssetFeature(
                    "glass.volume",
                    "Transmission Volume",
                    AssetComponentKind.Volume,
                    "pane"),
            ]);
        return new VisualPresetTemplate(
            "glass",
            "Glass",
            "A reusable transmission material with a sample pane and truthful target fallbacks.",
            asset,
            [material],
            ["material", "glass", "transmission"]);
    }

    private static VisualPresetTemplate CreateOrbitalRocket()
    {
        var paint = CreateMaterial(
            "material.rocket.paint",
            "Orbital Rocket Paint",
            new ColorValue(0.72, 0.75, 0.8),
            0.18,
            0.82,
            new ColorValue(0, 0, 0),
            0,
            clearcoat: 0.65,
            exposed:
            [
                Exposed.Color(
                    "rocket.paintColor",
                    "Paint Color",
                    "Finish",
                    new ColorValue(0.72, 0.75, 0.8),
                    target: "baseColor"),
                Exposed.Scalar(
                    "rocket.metallic",
                    "Metallic",
                    "Finish",
                    0.82,
                    0,
                    1,
                    target: "metallic"),
                Exposed.Scalar(
                    "rocket.roughness",
                    "Roughness",
                    "Finish",
                    0.18,
                    0,
                    1,
                    target: "roughness"),
                Exposed.Scalar(
                    "rocket.clearcoat",
                    "Clearcoat",
                    "Finish",
                    0.65,
                    0,
                    1,
                    target: "clearcoat"),
            ],
            tags: ["metal", "paint", "rocket"]);
        var window = CreateMaterial(
            "material.rocket.window",
            "Orbital Rocket Windows",
            new ColorValue(0.02, 0.06, 0.1),
            0.08,
            0.35,
            new ColorValue(0.05, 0.3, 1),
            2.5,
            exposed:
            [
                Exposed.Color(
                    "rocket.windowColor",
                    "Window Glow",
                    "Windows",
                    new ColorValue(0.05, 0.3, 1),
                    target: "emissionColor"),
            ],
            tags: ["emissive", "window", "rocket"]);
        var asset = CreateAsset(
            "asset.orbital.reusable-rocket",
            "Orbital Reusable Rocket",
            [
                new AssetPart(
                    "fuselage",
                    "Fuselage",
                    "geometry.rocket.fuselage",
                    "paint"),
                new AssetPart(
                    "fins",
                    "Fins",
                    "geometry.rocket.fins",
                    "paint"),
                new AssetPart(
                    "engines",
                    "Engine Cluster",
                    "geometry.rocket.engines",
                    "engine"),
                new AssetPart(
                    "windows",
                    "Windows",
                    "geometry.rocket.windows",
                    "window"),
            ],
            [
                new AssetMaterialSlot(
                    "paint",
                    "Metallic Paint",
                    paint.MaterialId),
                new AssetMaterialSlot(
                    "engine",
                    "Engine Metal",
                    "material.metal.engine"),
                new AssetMaterialSlot(
                    "window",
                    "Windows",
                    window.MaterialId),
            ],
            [
                AssetParameter.Scalar(
                    "rocket.bodyLength",
                    "Body Length",
                    18,
                    2,
                    80,
                    "Shape"),
                AssetParameter.Scalar(
                    "rocket.bodyDiameter",
                    "Body Diameter",
                    2.4,
                    0.2,
                    12,
                    "Shape"),
                AssetParameter.Integer(
                    "rocket.engineCount",
                    "Engine Count",
                    7,
                    1,
                    13,
                    "Engines"),
                AssetParameter.Scalar(
                    "rocket.finSize",
                    "Fin Size",
                    1.4,
                    0.1,
                    8,
                    "Shape"),
            ],
            tags: ["asset", "rocket", "vehicle", "metal"],
            features:
            [
                new AssetFeature(
                    "rocket.studio-light",
                    "Studio Light Rig",
                    AssetComponentKind.Light,
                    "fuselage"),
                new AssetFeature(
                    "rocket.camera",
                    "Studio Camera",
                    AssetComponentKind.Camera,
                    "fuselage"),
                new AssetFeature(
                    "rocket.exhaust",
                    "Engine Exhaust",
                    AssetComponentKind.Effect,
                    "engines"),
                new AssetFeature(
                    "rocket.panels",
                    "Panel Decals",
                    AssetComponentKind.Decal,
                    "fuselage"),
            ]);
        return new VisualPresetTemplate(
            "orbital-reusable-rocket",
            "Orbital Reusable Rocket",
            "A reusable parametric rocket with metallic paint, fins, engine cluster, glowing windows, and reusable finish controls.",
            asset,
            [paint, window],
            ["asset", "rocket", "procedural", "metal"]);
    }

    private static MaterialGraph CreateMaterial(
        string id,
        string name,
        ColorValue baseColor,
        double roughness,
        double metallic,
        ColorValue emissionColor,
        double emissionStrength,
        double transmission = 0,
        double ior = 1.5,
        double absorption = 0,
        double clearcoat = 0,
        IEnumerable<Exposed>? exposed = null,
        IEnumerable<string>? tags = null)
    {
        var registry = VisualNodeRegistry.CreateDefault();
        var nodes = new List<VisualNode>();
        var connections = new List<VisualConnection>();
        var constants = new Dictionary<string, NodePort>(
            StringComparer.Ordinal);
        var parameters = new List<MaterialParameterDefinition>();
        var nextNode = 1;
        var nextConnection = 1;

        NodePort AddColor(string key, ColorValue value) =>
            AddConstant(
                key,
                VisualBuiltInNodeTypes.ConstantColor,
                value);
        NodePort AddScalar(string key, double value) =>
            AddConstant(
                key,
                VisualBuiltInNodeTypes.ConstantScalar,
                new ScalarValue(value));

        NodePort AddConstant(
            string key,
            string typeId,
            VisualValue value)
        {
            var nodeId = NodeId.Parse(
                $"100000000000000000000000{nextNode++:D8}");
            var descriptor = GetDescriptor(registry, typeId);
            var output = descriptor.Ports.Single(
                port => port.Direction == PortDirection.Output);
            nodes.Add(
                VisualNode.Create(
                    nodeId,
                    typeId,
                    descriptor.Version,
                    [new KeyValuePair<string, VisualValue>("value", value)]));
            var port = new NodePort(
                new VisualPortReference(nodeId, output.Id),
                output.Contract,
                nodeId);
            constants.Add(key, port);
            return port;
        }

        var basePort = AddColor("baseColor", baseColor);
        var metallicPort = AddScalar("metallic", metallic);
        var roughnessPort = AddScalar("roughness", roughness);
        var emissionColorPort = AddColor("emissionColor", emissionColor);
        var emissionStrengthPort = AddScalar(
            "emissionStrength",
            emissionStrength);
        var transmissionPort = AddScalar("transmission", transmission);
        var iorPort = AddScalar("ior", ior);
        var absorptionPort = AddScalar("absorption", absorption);
        var clearcoatPort = AddScalar("clearcoat", clearcoat);
        var outputId = NodeId.Parse(
            $"100000000000000000000000{999:D8}");
        var outputDescriptor = GetDescriptor(
            registry,
            VisualBuiltInNodeTypes.MaterialOutput);
        nodes.Add(
            VisualNode.Create(
                outputId,
                VisualBuiltInNodeTypes.MaterialOutput,
                outputDescriptor.Version));

        Connect(basePort, "baseColor");
        Connect(metallicPort, "metallic");
        Connect(roughnessPort, "roughness");
        Connect(emissionColorPort, "emissionColor");
        Connect(emissionStrengthPort, "emissionStrength");
        Connect(transmissionPort, "transmission");
        Connect(iorPort, "ior");
        Connect(absorptionPort, "absorption");
        Connect(clearcoatPort, "clearcoat");

        foreach (var item in exposed ?? [])
        {
            if (!constants.TryGetValue(item.TargetKey, out var target))
            {
                throw new InvalidOperationException(
                    $"Material target '{item.TargetKey}' is not present.");
            }

            parameters.Add(
                new MaterialParameterDefinition(
                    item.Id,
                    item.Name,
                    target.Contract,
                    item.Value,
                    new VisualParameterEditorMetadata(
                        item.Control,
                        item.Name,
                        item.Group,
                        minimum: item.Minimum,
                        maximum: item.Maximum,
                        step: item.Step),
                    target: new MaterialParameterTarget(
                        target.NodeId,
                        "value")));
        }

        var materialContract = outputDescriptor.Ports.Single(
            port => port.Id == new PortId("material")).Contract;
        var output = new VisualOutput(
            "material",
            "Material",
            new VisualPortReference(outputId, new PortId("material")),
            materialContract);
        var recipe = new VisualRecipe(
            id + ".graph",
            nodes,
            connections,
            outputs: [output]);
        return new MaterialGraph(
            id,
            name,
            recipe,
            [
                new MaterialChannelBinding(
                    MaterialChannel.Material,
                    output.Source,
                    materialContract),
            ],
            parameters,
            [new MaterialLayer("surface", "Surface", 0)],
            tags);

        void Connect(NodePort source, string targetPort)
        {
            var target = outputDescriptor.Ports.Single(
                port => port.Id == new PortId(targetPort));
            connections.Add(
                new VisualConnection(
                    ConnectionId.Parse(
                        $"200000000000000000000000{nextConnection++:D8}"),
                    source.Reference,
                    new VisualPortReference(
                        outputId,
                        target.Id),
                    source.Contract,
                    target.Contract));
        }
    }

    private static AssetRecipe CreateAsset(
        string id,
        string name,
        IReadOnlyList<AssetPart> parts,
        IReadOnlyList<AssetMaterialSlot> slots,
        IReadOnlyList<AssetParameterDefinition> parameters,
        IEnumerable<string>? tags = null,
        IReadOnlyList<AssetFeature>? features = null)
    {
        var registry = VisualNodeRegistry.CreateDefault();
        var nodes = new List<VisualNode>();
        var outputs = new List<VisualOutput>();
        var components = new List<AssetComponent>();
        var nextNode = 1;
        foreach (var part in parts)
        {
            var nodeId = NodeId.Parse(
                $"300000000000000000000000{nextNode++:D8}");
            var descriptor = GetDescriptor(
                registry,
                VisualBuiltInNodeTypes.ConstantGeometry);
            var output = descriptor.Ports.Single(
                port => port.Direction == PortDirection.Output);
            nodes.Add(
                VisualNode.Create(
                    nodeId,
                    VisualBuiltInNodeTypes.ConstantGeometry,
                    descriptor.Version,
                    [
                        new KeyValuePair<string, VisualValue>(
                            "value",
                            new GeometryValue(
                                part.GeometryId,
                                GeometryKind.Mesh,
                                CoordinateSpace.Object,
                                VisualUnit.Meters)),
                    ]));
            var source = new VisualPortReference(nodeId, output.Id);
            outputs.Add(
                new VisualOutput(
                    part.Id,
                    part.Name,
                    source,
                    output.Contract));
            components.Add(
                new AssetComponent(
                    part.Id,
                    part.Name,
                    AssetComponentKind.Geometry,
                    source));
            components.Add(
                new AssetComponent(
                    part.Id + ".material",
                    part.Name + " material",
                    AssetComponentKind.MaterialBinding,
                    source,
                    part.MaterialSlotId));
        }

        foreach (var feature in features ?? [])
        {
            var source = components
                .FirstOrDefault(
                    component => component.Id == feature.PartId)
                ?.Source
                ?? throw new InvalidOperationException(
                    $"Asset feature '{feature.Id}' references unknown part '{feature.PartId}'.");
            components.Add(
                new AssetComponent(
                    feature.Id,
                    feature.Name,
                    feature.Kind,
                    source));
        }

        var graph = new VisualRecipe(
            id + ".graph",
            nodes,
            parameters: parameters.Select(
                parameter => new VisualParameter(
                    parameter.Id,
                    parameter.Name,
                    parameter.Contract,
                    parameter.DefaultValue)),
            outputs: outputs);
        return new AssetRecipe(
            id,
            name,
            graph,
            components,
            slots,
            parameters,
            tags);
    }

    private static VisualNodeDescriptor GetDescriptor(
        VisualNodeRegistry registry,
        string typeId) =>
        registry.TryGetDescriptor(typeId, out var descriptor) &&
        descriptor is not null
            ? descriptor
            : throw new InvalidOperationException(
                $"Node '{typeId}' is not registered.");

    private sealed record NodePort(
        VisualPortReference Reference,
        VisualTypeContract Contract,
        NodeId NodeId);

    private sealed record AssetPart(
        string Id,
        string Name,
        string GeometryId,
        string MaterialSlotId);

    private sealed record AssetFeature(
        string Id,
        string Name,
        AssetComponentKind Kind,
        string PartId);

    private sealed record Exposed(
        string Id,
        string Name,
        string Group,
        VisualValue Value,
        VisualParameterControlKind Control,
        string TargetKey,
        double? Minimum,
        double? Maximum,
        double? Step)
    {
        public static Exposed Color(
            string id,
            string name,
            string group,
            ColorValue value,
            string target) =>
            new(
                id,
                name,
                group,
                value,
                VisualParameterControlKind.Color,
                target,
                null,
                null,
                null);

        public static Exposed Scalar(
            string id,
            string name,
            string group,
            double value,
            double minimum,
            double maximum,
            string target) =>
            new(
                id,
                name,
                group,
                new ScalarValue(value),
                VisualParameterControlKind.Slider,
                target,
                minimum,
                maximum,
                0.01);

        public static Exposed Text(
            string id,
            string name,
            string group,
            string value) =>
            new(
                id,
                name,
                group,
                new TextValue(value),
                VisualParameterControlKind.Text,
                "text",
                null,
                null,
                null);
    }

    private static class AssetParameter
    {
        public static AssetParameterDefinition Scalar(
            string id,
            string name,
            double value,
            double minimum,
            double maximum,
            string group) =>
            new(
                id,
                name,
                new VisualTypeContract(
                    VisualValueKind.Scalar,
                    VisualUnit.Unitless,
                    CoordinateSpace.None,
                    ColorSpace.None),
                new ScalarValue(value),
                new VisualParameterEditorMetadata(
                    VisualParameterControlKind.Slider,
                    name,
                    group,
                    minimum: minimum,
                    maximum: maximum,
                    step: 0.01));

        public static AssetParameterDefinition Integer(
            string id,
            string name,
            long value,
            long minimum,
            long maximum,
            string group) =>
            new(
                id,
                name,
                new VisualTypeContract(
                    VisualValueKind.Integer,
                    VisualUnit.Unitless,
                    CoordinateSpace.None,
                    ColorSpace.None),
                new IntegerValue(value),
                new VisualParameterEditorMetadata(
                    VisualParameterControlKind.Slider,
                    name,
                    group,
                    minimum: minimum,
                    maximum: maximum,
                    step: 1));

        public static AssetParameterDefinition Text(
            string id,
            string name,
            string value,
            string group) =>
            new(
                id,
                name,
                new VisualTypeContract(
                    VisualValueKind.Text,
                    VisualUnit.Unitless,
                    CoordinateSpace.None,
                    ColorSpace.None),
                new TextValue(value),
                new VisualParameterEditorMetadata(
                    VisualParameterControlKind.Text,
                    name,
                    group));
    }
}
