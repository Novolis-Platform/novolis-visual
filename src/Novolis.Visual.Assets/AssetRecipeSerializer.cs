using System.Text;
using System.Text.Json;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Assets;

/// <summary>
/// Canonical persistence for assembled assets and child instances.
/// </summary>
public static class AssetRecipeSerializer
{
    public const int CurrentSchemaVersion = 2;

    public static string Serialize(AssetRecipe asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("assetId", asset.AssetId);
            writer.WriteStartArray("components");
            foreach (var component in asset.Components)
            {
                writer.WriteStartObject();
                writer.WriteString("id", component.Id);
                writer.WriteString("kind", component.Kind.ToString());
                writer.WriteString("materialSlotId", component.MaterialSlotId);
                writer.WriteString("name", component.Name);
                WritePort(writer, component.Source);
                if (component.Geometry is { } geometry)
                {
                    writer.WritePropertyName("geometry");
                    WriteGeometry(writer, geometry);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteString("displayName", asset.DisplayName);
            writer.WriteNumber("moduleVersion", asset.ModuleVersion);
            writer.WritePropertyName("graph");
            WriteRaw(writer, VisualRecipeSerializer.Serialize(asset.Graph));
            writer.WriteStartArray("materialSlots");
            foreach (var slot in asset.MaterialSlots)
            {
                writer.WriteStartObject();
                writer.WriteString("id", slot.Id);
                writer.WriteString("materialId", slot.MaterialId);
                writer.WriteString("name", slot.Name);
                writer.WriteBoolean("required", slot.Required);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteStartArray("parameters");
            foreach (var parameter in asset.Parameters)
            {
                WriteParameter(writer, parameter);
            }

            writer.WriteEndArray();
            writer.WriteNumber("schemaVersion", CurrentSchemaVersion);
            writer.WriteStartArray("dependencies");
            foreach (var dependency in asset.Dependencies)
            {
                writer.WriteStartObject();
                writer.WriteString("algorithm", dependency.Algorithm);
                writer.WriteString("fingerprint", dependency.Fingerprint);
                writer.WriteString("id", dependency.Id);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteStartArray("tags");
            foreach (var tag in asset.Tags)
            {
                writer.WriteStringValue(tag);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static AssetRecipe Deserialize(
        string json,
        VisualNodeRegistry? registry = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var version = Required(root, "schemaVersion").GetInt32();
        if (version is not 1 and not CurrentSchemaVersion)
        {
            throw new JsonException(
                $"Expected asset schema version 1 or {CurrentSchemaVersion}, got {version}.");
        }

        return new AssetRecipe(
            RequiredString(root, "assetId"),
            RequiredString(root, "displayName"),
            VisualRecipeSerializer.Deserialize(
                Required(root, "graph").GetRawText(),
                registry),
            ParseComponents(OptionalArray(root, "components")),
            ParseSlots(OptionalArray(root, "materialSlots")),
            ParseParameters(OptionalArray(root, "parameters")),
            OptionalArray(root, "tags")
                .EnumerateArray()
                .Select(element => element.GetString() ?? string.Empty),
            root.TryGetProperty("moduleVersion", out var moduleVersion)
                ? moduleVersion.GetInt32()
                : 1,
            ParseDependencies(OptionalArray(root, "dependencies")));
    }

    public static string SerializeInstance(AssetInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("assetInstanceId", instance.InstanceId);
            writer.WriteString("assetId", instance.Parent.AssetId);
            writer.WriteStartObject("overrides");
            foreach (var pair in instance.Overrides)
            {
                writer.WritePropertyName(pair.Key);
                WriteRaw(
                    writer,
                    VisualRecipeSerializer.SerializeValue(pair.Value));
            }

            writer.WriteEndObject();
            writer.WriteNumber("schemaVersion", CurrentSchemaVersion);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static AssetInstance DeserializeInstance(
        string json,
        AssetRecipe parent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentNullException.ThrowIfNull(parent);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var version = Required(root, "schemaVersion").GetInt32();
        if (version is not 1 and not CurrentSchemaVersion)
        {
            throw new JsonException(
                $"Expected asset schema version 1 or {CurrentSchemaVersion}, got {version}.");
        }

        var parentId = RequiredString(root, "assetId");
        if (!string.Equals(parentId, parent.AssetId, StringComparison.Ordinal))
        {
            throw new JsonException(
                $"Instance parent '{parentId}' does not match '{parent.AssetId}'.");
        }

        return new AssetInstance(
            RequiredString(root, "assetInstanceId"),
            parent,
            Required(root, "overrides")
                .EnumerateObject()
                .Select(property =>
                    new KeyValuePair<string, VisualValue>(
                        property.Name,
                        VisualRecipeSerializer.DeserializeValue(
                            property.Value.GetRawText()))));
    }

    private static IEnumerable<AssetComponent> ParseComponents(
        JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new AssetComponent(
                RequiredString(element, "id"),
                RequiredString(element, "name"),
                ParseEnum<AssetComponentKind>(
                    Required(element, "kind"),
                    "kind"),
                ParsePort(element),
                OptionalString(element, "materialSlotId"),
                element.TryGetProperty("geometry", out var geometry)
                    ? ParseGeometry(geometry)
                    : null);
        }
    }

    private static AssetGeometryDefinition ParseGeometry(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Property 'geometry' must be an object.");
        }

        var size = OptionalArray(element, "size")
            .EnumerateArray()
            .Select(value => value.GetDouble());
        var position = OptionalArray(element, "position")
            .EnumerateArray()
            .Select(value => value.GetDouble());
        var rotation = OptionalArray(element, "rotationDegrees")
            .EnumerateArray()
            .Select(value => value.GetDouble());
        var repeat = element.TryGetProperty("repeat", out var repeatElement)
            ? ParseRepeat(repeatElement)
            : null;

        return new AssetGeometryDefinition(
            RequiredString(element, "geometryId"),
            ParseEnum<AssetGeometryPrimitive>(
                Required(element, "primitive"),
                "primitive"),
            size,
            position,
            rotation,
            Required(element, "segments").GetInt32(),
            ParseBindings(OptionalArray(element, "sizeBindings")),
            ParseBindings(OptionalArray(element, "positionBindings")),
            repeat,
            OptionalString(element, "textParameterId"),
            OptionalString(element, "textSpacingParameterId"));
    }

    private static AssetGeometryRepeat ParseRepeat(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Property 'repeat' must be an object.");
        }

        return new AssetGeometryRepeat(
            Required(element, "fixedCount").GetInt32(),
            OptionalString(element, "countParameterId"),
            Required(element, "minimumCount").GetInt32(),
            Required(element, "maximumCount").GetInt32(),
            ParseEnum<AssetGeometryAxis>(
                Required(element, "axis"),
                "axis"),
            ParseEnum<AssetGeometryRepeatPattern>(
                Required(element, "pattern"),
                "pattern"),
            Required(element, "spacing").GetDouble());
    }

    private static IEnumerable<AssetGeometryParameterBinding> ParseBindings(
        JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new AssetGeometryParameterBinding(
                Required(element, "component").GetInt32(),
                RequiredString(element, "parameterId"),
                Required(element, "multiplier").GetDouble(),
                Required(element, "offset").GetDouble());
        }
    }

    private static void WriteGeometry(
        Utf8JsonWriter writer,
        AssetGeometryDefinition geometry)
    {
        writer.WriteStartObject();
        writer.WriteString("geometryId", geometry.GeometryId);
        writer.WriteString("primitive", geometry.Primitive.ToString());
        WriteVector(writer, "size", geometry.Size);
        WriteVector(writer, "position", geometry.Position);
        WriteVector(writer, "rotationDegrees", geometry.RotationDegrees);
        writer.WriteNumber("segments", geometry.Segments);
        writer.WriteStartArray("sizeBindings");
        foreach (var binding in geometry.SizeBindings)
        {
            WriteBinding(writer, binding);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("positionBindings");
        foreach (var binding in geometry.PositionBindings)
        {
            WriteBinding(writer, binding);
        }

        writer.WriteEndArray();
        if (geometry.Repeat is { } repeat)
        {
            writer.WritePropertyName("repeat");
            writer.WriteStartObject();
            writer.WriteNumber("fixedCount", repeat.FixedCount);
            writer.WriteString("countParameterId", repeat.CountParameterId);
            writer.WriteNumber("minimumCount", repeat.MinimumCount);
            writer.WriteNumber("maximumCount", repeat.MaximumCount);
            writer.WriteString("axis", repeat.Axis.ToString());
            writer.WriteString("pattern", repeat.Pattern.ToString());
            writer.WriteNumber("spacing", repeat.Spacing);
            writer.WriteEndObject();
        }

        writer.WriteString("textParameterId", geometry.TextParameterId);
        writer.WriteString(
            "textSpacingParameterId",
            geometry.TextSpacingParameterId);
        writer.WriteEndObject();
    }

    private static void WriteBinding(
        Utf8JsonWriter writer,
        AssetGeometryParameterBinding binding)
    {
        writer.WriteStartObject();
        writer.WriteNumber("component", binding.Component);
        writer.WriteString("parameterId", binding.ParameterId);
        writer.WriteNumber("multiplier", binding.Multiplier);
        writer.WriteNumber("offset", binding.Offset);
        writer.WriteEndObject();
    }

    private static void WriteVector(
        Utf8JsonWriter writer,
        string name,
        IEnumerable<double> values)
    {
        writer.WriteStartArray(name);
        foreach (var value in values)
        {
            writer.WriteNumberValue(value);
        }

        writer.WriteEndArray();
    }

    private static IEnumerable<AssetMaterialSlot> ParseSlots(
        JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new AssetMaterialSlot(
                RequiredString(element, "id"),
                RequiredString(element, "name"),
                RequiredString(element, "materialId"),
                Required(element, "required").GetBoolean());
        }
    }

    private static IEnumerable<AssetParameterDefinition> ParseParameters(
        JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new AssetParameterDefinition(
                RequiredString(element, "id"),
                RequiredString(element, "name"),
                VisualRecipeSerializer.DeserializeContract(
                    Required(element, "contract").GetRawText()),
                VisualRecipeSerializer.DeserializeValue(
                    Required(element, "defaultValue").GetRawText()),
                ParseEditor(OptionalObject(element, "editor")),
                Required(element, "isInstanceEditable").GetBoolean(),
                ParseTarget(element));
        }
    }

    private static IEnumerable<VisualDependencyFingerprint> ParseDependencies(
        JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new VisualDependencyFingerprint(
                RequiredString(element, "id"),
                RequiredString(element, "algorithm"),
                RequiredString(element, "fingerprint"));
        }
    }

    private static void WriteParameter(
        Utf8JsonWriter writer,
        AssetParameterDefinition parameter)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("contract");
        WriteRaw(
            writer,
            VisualRecipeSerializer.SerializeContract(parameter.Contract));
        writer.WritePropertyName("defaultValue");
        WriteRaw(
            writer,
            VisualRecipeSerializer.SerializeValue(parameter.DefaultValue));
        writer.WritePropertyName("editor");
        WriteEditor(writer, parameter.Editor);
        writer.WriteString("id", parameter.Id);
        writer.WriteBoolean("isInstanceEditable", parameter.IsInstanceEditable);
        writer.WriteString("name", parameter.Name);
        if (parameter.Target is { } target)
        {
            writer.WritePropertyName("target");
            writer.WriteStartObject();
            writer.WriteString("nodeId", target.NodeId.ToString());
            writer.WriteString("valueName", target.ValueName);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static AssetParameterTarget? ParseTarget(JsonElement element)
    {
        if (!element.TryGetProperty("target", out var target))
        {
            return null;
        }

        return new AssetParameterTarget(
            NodeId.Parse(RequiredString(target, "nodeId")),
            RequiredString(target, "valueName"));
    }

    private static void WriteEditor(
        Utf8JsonWriter writer,
        VisualParameterEditorMetadata editor)
    {
        writer.WriteStartObject();
        writer.WriteString("control", editor.Control.ToString());
        writer.WriteString("description", editor.Description);
        writer.WriteString("displayName", editor.DisplayName);
        writer.WriteString("group", editor.Group);
        if (editor.Maximum is { } maximum)
        {
            writer.WriteNumber("maximum", maximum);
        }

        if (editor.Minimum is { } minimum)
        {
            writer.WriteNumber("minimum", minimum);
        }

        if (editor.Step is { } step)
        {
            writer.WriteNumber("step", step);
        }

        writer.WriteString("unitLabel", editor.UnitLabel);
        writer.WriteEndObject();
    }

    private static VisualParameterEditorMetadata ParseEditor(
        JsonElement element) =>
        new(
            ParseEnum<VisualParameterControlKind>(
                Required(element, "control"),
                "control"),
            OptionalString(element, "displayName"),
            OptionalString(element, "group"),
            OptionalString(element, "description"),
            OptionalDouble(element, "minimum"),
            OptionalDouble(element, "maximum"),
            OptionalDouble(element, "step"),
            OptionalString(element, "unitLabel"));

    private static VisualPortReference ParsePort(JsonElement element) =>
        new(
            NodeId.Parse(RequiredString(element, "nodeId")),
            new PortId(RequiredString(element, "portId")));

    private static void WritePort(
        Utf8JsonWriter writer,
        VisualPortReference port)
    {
        writer.WritePropertyName("nodeId");
        writer.WriteStringValue(port.NodeId.ToString());
        writer.WriteString("portId", port.PortId.Value);
    }

    private static void WriteRaw(Utf8JsonWriter writer, string json)
    {
        using var document = JsonDocument.Parse(json);
        document.RootElement.WriteTo(writer);
    }

    private static JsonElement OptionalArray(
        JsonElement element,
        string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            using var document = JsonDocument.Parse("[]");
            return document.RootElement.Clone();
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"Property '{name}' must be an array.");
        }

        return value;
    }

    private static JsonElement OptionalObject(
        JsonElement element,
        string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            using var document = JsonDocument.Parse("{}");
            return document.RootElement.Clone();
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Property '{name}' must be an object.");
        }

        return value;
    }

    private static double? OptionalDouble(
        JsonElement element,
        string name) =>
        element.TryGetProperty(name, out var value) &&
        value.ValueKind != JsonValueKind.Null
            ? value.GetDouble()
            : null;

    private static string? OptionalString(
        JsonElement element,
        string name) =>
        element.TryGetProperty(name, out var value) &&
        value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;

    private static string RequiredString(
        JsonElement element,
        string name) =>
        Required(element, name).GetString() ??
        throw new JsonException($"Property '{name}' must be a string.");

    private static JsonElement Required(
        JsonElement element,
        string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            throw new JsonException($"Missing JSON property '{name}'.");
        }

        return value;
    }

    private static T ParseEnum<T>(
        JsonElement element,
        string name)
        where T : struct, Enum
    {
        var text = element.GetString();
        if (text is null || !Enum.TryParse<T>(text, true, out var result))
        {
            throw new JsonException($"Invalid {typeof(T).Name} value for '{name}'.");
        }

        return result;
    }
}
