using System.Collections.Immutable;
using System.Numerics;
using System.Text.Json;

namespace Novolis.Visual.Definitions;

internal static class VisualValueJson
{
    public static void WriteValue(Utf8JsonWriter writer, VisualValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStartObject();

        switch (value)
        {
            case ScalarValue scalar:
                writer.WriteString("kind", scalar.Kind.ToString());
                writer.WriteString("unit", scalar.Unit.ToString());
                writer.WriteNumber("value", scalar.Value);
                break;

            case IntegerValue integer:
                writer.WriteString("kind", integer.Kind.ToString());
                writer.WriteString("unit", integer.Unit.ToString());
                writer.WriteNumber("value", integer.Value);
                break;

            case BooleanValue boolean:
                writer.WriteString("kind", boolean.Kind.ToString());
                writer.WriteBoolean("value", boolean.Value);
                break;

            case ColorValue color:
                writer.WriteNumber("alpha", color.Alpha);
                writer.WriteNumber("blue", color.Blue);
                writer.WriteString("colorSpace", color.ColorSpace.ToString());
                writer.WriteNumber("green", color.Green);
                writer.WriteString("kind", color.Kind.ToString());
                writer.WriteNumber("red", color.Red);
                break;

            case VectorValue vector:
                WriteVector(writer, vector);
                break;

            case TransformValue transform:
                writer.WriteString("coordinateSpace", transform.CoordinateSpace.ToString());
                writer.WriteString("kind", transform.Kind.ToString());
                writer.WritePropertyName("rotation");
                WriteVectorBody(writer, transform.Rotation);
                writer.WritePropertyName("scale");
                WriteVectorBody(writer, transform.Scale);
                writer.WritePropertyName("translation");
                WriteVectorBody(writer, transform.Translation);
                break;

            case ImageValue image:
                writer.WriteString("alphaMode", image.AlphaMode.ToString());
                writer.WriteString("assetId", image.AssetId);
                writer.WriteString("colorSpace", image.ColorSpace.ToString());
                writer.WriteString("format", image.Format.ToString());
                writer.WriteNumber("height", image.Height);
                writer.WriteString("kind", image.Kind.ToString());
                writer.WriteNumber("width", image.Width);
                break;

            case GeometryValue geometry:
                writer.WriteString("assetId", geometry.AssetId);
                writer.WriteString("coordinateSpace", geometry.CoordinateSpace.ToString());
                writer.WriteString("geometryKind", geometry.GeometryKind.ToString());
                writer.WriteString("kind", geometry.Kind.ToString());
                writer.WriteString("unit", geometry.Unit.ToString());
                break;

            case SurfaceValue surface:
                writer.WritePropertyName("baseColor");
                WriteValue(writer, surface.BaseColor);
                writer.WriteString("kind", surface.Kind.ToString());
                writer.WriteString("materialId", surface.MaterialId);
                writer.WriteNumber("metallic", surface.Metallic);
                writer.WriteNumber("roughness", surface.Roughness);
                break;

            case LightValue light:
                writer.WritePropertyName("color");
                WriteValue(writer, light.Color);
                writer.WriteNumber("intensity", light.Intensity);
                writer.WriteString("intensityUnit", light.IntensityUnit.ToString());
                writer.WriteString("kind", light.Kind.ToString());
                writer.WriteString("lightKind", light.LightKind.ToString());
                writer.WritePropertyName("transform");
                WriteValue(writer, light.Transform);
                break;

            case VolumeValue volume:
                writer.WritePropertyName("albedo");
                WriteValue(writer, volume.Albedo);
                writer.WriteNumber("anisotropy", volume.Anisotropy);
                writer.WriteNumber("density", volume.Density);
                writer.WriteString("densityUnit", volume.DensityUnit.ToString());
                writer.WriteString("kind", volume.Kind.ToString());
                writer.WriteString("volumeId", volume.VolumeId);
                writer.WriteString("volumeKind", volume.VolumeKind.ToString());
                break;

            case EffectValue effect:
                writer.WriteString("effectId", effect.EffectId);
                writer.WriteString("kind", effect.Kind.ToString());
                writer.WritePropertyName("parameters");
                writer.WriteStartObject();
                foreach (var parameter in effect.Parameters)
                {
                    writer.WritePropertyName(parameter.Key);
                    WriteValue(writer, parameter.Value);
                }

                writer.WriteEndObject();
                break;

            case SceneValue scene:
                writer.WriteString("kind", scene.Kind.ToString());
                writer.WritePropertyName("objectIds");
                writer.WriteStartArray();
                foreach (var objectId in scene.ObjectIds)
                {
                    writer.WriteStringValue(objectId);
                }

                writer.WriteEndArray();
                writer.WriteString("sceneId", scene.SceneId);
                break;

            case EmitterValue emitter:
                writer.WritePropertyName("color");
                WriteValue(writer, emitter.Color);
                writer.WriteString("emitterId", emitter.EmitterId);
                writer.WriteNumber("initialVelocityX", emitter.InitialVelocity.X);
                writer.WriteNumber("initialVelocityY", emitter.InitialVelocity.Y);
                writer.WriteNumber("initialVelocityZ", emitter.InitialVelocity.Z);
                writer.WriteString("kind", emitter.Kind.ToString());
                writer.WriteString("renderMode", emitter.RenderMode.ToString());
                writer.WriteNumber("rate", emitter.Rate);
                writer.WriteString("seed", emitter.Seed.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteNumber("lifetimeSeconds", emitter.LifetimeSeconds);
                if (emitter.SpriteAssetId is not null)
                {
                    writer.WriteString("spriteAssetId", emitter.SpriteAssetId);
                }

                break;

            case DecalValue decal:
                writer.WritePropertyName("color");
                WriteValue(writer, decal.Color);
                writer.WriteString("decalId", decal.DecalId);
                writer.WriteString("kind", decal.Kind.ToString());
                writer.WriteString("projection", decal.Projection.ToString());
                writer.WriteNumber("opacity", decal.Opacity);
                writer.WriteNumber("sizeX", decal.Size.X);
                writer.WriteNumber("sizeY", decal.Size.Y);
                writer.WriteNumber("sizeZ", decal.Size.Z);
                if (decal.ImageAssetId is not null)
                {
                    writer.WriteString("imageAssetId", decal.ImageAssetId);
                }

                break;

            case FrameEffectValue frameEffect:
                writer.WriteNumber("amount", frameEffect.Amount);
                writer.WriteString("effectKind", frameEffect.EffectKind.ToString());
                writer.WriteString("kind", frameEffect.Kind.ToString());
                writer.WriteNumber("radius", frameEffect.Radius);
                writer.WriteNumber("threshold", frameEffect.Threshold);
                break;

            case FrameValue frame:
                writer.WriteNumber("deltaSeconds", frame.DeltaSeconds);
                writer.WriteNumber("frameIndex", frame.FrameIndex);
                writer.WriteNumber("framesPerSecond", frame.FramesPerSecond);
                writer.WriteString("kind", frame.Kind.ToString());
                writer.WriteNumber("timeSeconds", frame.TimeSeconds);
                break;

            default:
                throw new JsonException($"Unsupported visual value {value.GetType().Name}.");
        }

        writer.WriteEndObject();
    }

    public static VisualValue ParseValue(JsonElement element)
    {
        EnsureObject(element, "visual value");
        var kind = ParseEnum<VisualValueKind>(Required(element, "kind"), "kind");

        return kind switch
        {
            VisualValueKind.Scalar => new ScalarValue(
                Required(element, "value").GetDouble(),
                ParseEnum<VisualUnit>(Required(element, "unit"), "unit")),
            VisualValueKind.Integer => new IntegerValue(
                Required(element, "value").GetInt64(),
                ParseEnum<VisualUnit>(Required(element, "unit"), "unit")),
            VisualValueKind.Boolean => new BooleanValue(
                Required(element, "value").GetBoolean()),
            VisualValueKind.Color => ParseColor(element),
            VisualValueKind.Vector => ParseVector(element),
            VisualValueKind.Transform => ParseTransform(element),
            VisualValueKind.Image => new ImageValue(
                Required(element, "assetId").GetString() ?? string.Empty,
                Required(element, "width").GetInt32(),
                Required(element, "height").GetInt32(),
                ParseEnum<ImageFormat>(Required(element, "format"), "format"),
                ParseEnum<ColorSpace>(
                    Required(element, "colorSpace"),
                    "colorSpace"),
                ParseEnum<AlphaMode>(
                    Required(element, "alphaMode"),
                    "alphaMode")),
            VisualValueKind.Geometry => new GeometryValue(
                Required(element, "assetId").GetString() ?? string.Empty,
                ParseEnum<GeometryKind>(
                    Required(element, "geometryKind"),
                    "geometryKind"),
                ParseEnum<CoordinateSpace>(
                    Required(element, "coordinateSpace"),
                    "coordinateSpace"),
                ParseEnum<VisualUnit>(Required(element, "unit"), "unit")),
            VisualValueKind.Surface => new SurfaceValue(
                Required(element, "materialId").GetString() ?? string.Empty,
                ParseColorValue(Required(element, "baseColor")),
                Required(element, "roughness").GetDouble(),
                Required(element, "metallic").GetDouble()),
            VisualValueKind.Light => new LightValue(
                ParseEnum<LightKind>(Required(element, "lightKind"), "lightKind"),
                ParseColorValue(Required(element, "color")),
                Required(element, "intensity").GetDouble(),
                ParseEnum<VisualUnit>(
                    Required(element, "intensityUnit"),
                    "intensityUnit"),
                ParseTransformValue(Required(element, "transform"))),
            VisualValueKind.Volume => new VolumeValue(
                Required(element, "volumeId").GetString() ?? string.Empty,
                ParseEnum<VolumeKind>(
                    Required(element, "volumeKind"),
                    "volumeKind"),
                Required(element, "density").GetDouble(),
                ParseEnum<VisualUnit>(
                    Required(element, "densityUnit"),
                    "densityUnit"),
                ParseColorValue(Required(element, "albedo")),
                Required(element, "anisotropy").GetDouble()),
            VisualValueKind.Effect => ParseEffect(element),
            VisualValueKind.Scene => ParseScene(element),
            VisualValueKind.Emitter => new EmitterValue(
                Required(element, "emitterId").GetString() ?? string.Empty,
                ParseSeed(Required(element, "seed")),
                Required(element, "rate").GetDouble(),
                Required(element, "lifetimeSeconds").GetDouble(),
                new Vector3(
                    Required(element, "initialVelocityX").GetSingle(),
                    Required(element, "initialVelocityY").GetSingle(),
                    Required(element, "initialVelocityZ").GetSingle()),
                ParseColorValue(Required(element, "color")),
                ParseEnum<EmitterRenderMode>(
                    Required(element, "renderMode"),
                    "renderMode"),
                OptionalString(element, "spriteAssetId")),
            VisualValueKind.Decal => new DecalValue(
                Required(element, "decalId").GetString() ?? string.Empty,
                ParseEnum<DecalProjection>(
                    Required(element, "projection"),
                    "projection"),
                new Vector3(
                    Required(element, "sizeX").GetSingle(),
                    Required(element, "sizeY").GetSingle(),
                    Required(element, "sizeZ").GetSingle()),
                ParseColorValue(Required(element, "color")),
                Required(element, "opacity").GetDouble(),
                OptionalString(element, "imageAssetId")),
            VisualValueKind.FrameEffect => new FrameEffectValue(
                ParseEnum<FrameEffectKind>(
                    Required(element, "effectKind"),
                    "effectKind"),
                Required(element, "amount").GetDouble(),
                Required(element, "threshold").GetDouble(),
                Required(element, "radius").GetDouble()),
            VisualValueKind.Frame => new FrameValue(
                Required(element, "frameIndex").GetInt64(),
                Required(element, "timeSeconds").GetDouble(),
                Required(element, "deltaSeconds").GetDouble(),
                Required(element, "framesPerSecond").GetDouble()),
            _ => throw new JsonException($"Unsupported visual value kind {kind}."),
        };
    }

    public static void WriteContract(
        Utf8JsonWriter writer,
        VisualTypeContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        writer.WriteStartObject();
        writer.WriteNumber("attributes", (int)contract.Attributes);
        writer.WriteString("colorSpace", contract.ColorSpace.ToString());
        writer.WriteString("coordinateSpace", contract.CoordinateSpace.ToString());
        writer.WriteString("frequency", contract.Frequency.ToString());
        writer.WriteString("purity", contract.Purity.ToString());
        writer.WriteString("type", contract.ValueKind.ToString());
        writer.WriteString("unit", contract.Unit.ToString());
        writer.WriteEndObject();
    }

    public static VisualTypeContract ParseContract(JsonElement element)
    {
        EnsureObject(element, "type contract");
        return new VisualTypeContract(
            ParseEnum<VisualValueKind>(Required(element, "type"), "type"),
            ParseEnum<VisualUnit>(Required(element, "unit"), "unit"),
            ParseEnum<CoordinateSpace>(
                Required(element, "coordinateSpace"),
                "coordinateSpace"),
            ParseEnum<ColorSpace>(
                Required(element, "colorSpace"),
                "colorSpace"),
            ParseEnum<FieldFrequency>(
                Required(element, "frequency"),
                "frequency"),
            ParseEnum<FieldPurity>(Required(element, "purity"), "purity"),
            (VisualAttributeRequirement)Required(element, "attributes").GetInt32());
    }

    public static void WriteVectorBody(Utf8JsonWriter writer, VectorValue vector)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("components");
        writer.WriteStartArray();
        foreach (var component in vector.Components)
        {
            writer.WriteNumberValue(component);
        }

        writer.WriteEndArray();
        writer.WriteString("coordinateSpace", vector.CoordinateSpace.ToString());
        writer.WriteString("unit", vector.Unit.ToString());
        writer.WriteEndObject();
    }

    public static VectorValue ParseVectorBody(JsonElement element)
    {
        EnsureObject(element, "vector");
        var components = Required(element, "components")
            .EnumerateArray()
            .Select(component => component.GetDouble());
        return new VectorValue(
            components,
            ParseEnum<VisualUnit>(Required(element, "unit"), "unit"),
            ParseEnum<CoordinateSpace>(
                Required(element, "coordinateSpace"),
                "coordinateSpace"));
    }

    private static void WriteVector(Utf8JsonWriter writer, VectorValue vector)
    {
        writer.WritePropertyName("components");
        writer.WriteStartArray();
        foreach (var component in vector.Components)
        {
            writer.WriteNumberValue(component);
        }

        writer.WriteEndArray();
        writer.WriteString("coordinateSpace", vector.CoordinateSpace.ToString());
        writer.WriteString("kind", vector.Kind.ToString());
        writer.WriteString("unit", vector.Unit.ToString());
    }

    private static ColorValue ParseColor(JsonElement element)
    {
        return new ColorValue(
            Required(element, "red").GetDouble(),
            Required(element, "green").GetDouble(),
            Required(element, "blue").GetDouble(),
            Required(element, "alpha").GetDouble(),
            ParseEnum<ColorSpace>(
                Required(element, "colorSpace"),
                "colorSpace"));
    }

    private static VectorValue ParseVector(JsonElement element) =>
        ParseVectorBody(element);

    private static TransformValue ParseTransform(JsonElement element) =>
        new(
            ParseVectorBody(Required(element, "translation")),
            ParseVectorBody(Required(element, "rotation")),
            ParseVectorBody(Required(element, "scale")),
            ParseEnum<CoordinateSpace>(
                Required(element, "coordinateSpace"),
                "coordinateSpace"));

    private static EffectValue ParseEffect(JsonElement element)
    {
        var parameters = new List<KeyValuePair<string, VisualValue>>();
        foreach (var parameter in Required(element, "parameters").EnumerateObject())
        {
            parameters.Add(
                new(parameter.Name, ParseValue(parameter.Value)));
        }

        return new EffectValue(
            Required(element, "effectId").GetString() ?? string.Empty,
            parameters);
    }

    private static SceneValue ParseScene(JsonElement element) =>
        new(
            Required(element, "sceneId").GetString() ?? string.Empty,
            Required(element, "objectIds")
                .EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty));

    private static ColorValue ParseColorValue(JsonElement element)
    {
        var value = ParseValue(element);
        return value as ColorValue ??
               throw new JsonException("Expected a color value.");
    }

    private static TransformValue ParseTransformValue(JsonElement element)
    {
        var value = ParseValue(element);
        return value as TransformValue ??
               throw new JsonException("Expected a transform value.");
    }

    private static uint ParseSeed(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number &&
            element.TryGetUInt32(out var number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.String &&
            uint.TryParse(
                element.GetString(),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out var text))
        {
            return text;
        }

        throw new JsonException("Invalid emitter seed.");
    }

    private static string? OptionalString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static JsonElement Required(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            throw new JsonException($"Missing JSON property '{name}'.");
        }

        return value;
    }

    private static void EnsureObject(JsonElement element, string description)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Expected {description} to be a JSON object.");
        }
    }

    private static T ParseEnum<T>(JsonElement element, string propertyName)
        where T : struct, Enum
    {
        var text = element.GetString();
        if (text is null || !Enum.TryParse<T>(text, ignoreCase: true, out var value))
        {
            throw new JsonException(
                $"Invalid {typeof(T).Name} value for '{propertyName}'.");
        }

        return value;
    }
}
