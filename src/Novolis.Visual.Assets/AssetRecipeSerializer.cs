using System.Text;
using System.Text.Json;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Assets;

/// <summary>
/// Canonical persistence for assembled assets and child instances.
/// </summary>
public static class AssetRecipeSerializer
{
    public const int CurrentSchemaVersion = 1;

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
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteString("displayName", asset.DisplayName);
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
        if (version != CurrentSchemaVersion)
        {
            throw new JsonException(
                $"Expected asset schema version {CurrentSchemaVersion}, got {version}.");
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
                .Select(element => element.GetString() ?? string.Empty));
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
        if (version != CurrentSchemaVersion)
        {
            throw new JsonException(
                $"Expected asset schema version {CurrentSchemaVersion}, got {version}.");
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
                OptionalString(element, "materialSlotId"));
        }
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
