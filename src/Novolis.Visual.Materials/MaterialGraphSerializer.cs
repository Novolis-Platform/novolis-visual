using System.Text;
using System.Text.Json;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Materials;

/// <summary>
/// Canonical persistence for reusable material graphs and instances.
/// </summary>
public static class MaterialGraphSerializer
{
    public const int CurrentSchemaVersion = 1;

    public static string Serialize(MaterialGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("displayName", graph.DisplayName);
            writer.WriteStartArray("layers");
            foreach (var layer in graph.Layers)
            {
                WriteLayer(writer, layer);
            }

            writer.WriteEndArray();
            writer.WriteString("materialId", graph.MaterialId);
            writer.WriteStartArray("parameters");
            foreach (var parameter in graph.Parameters)
            {
                WriteParameter(writer, parameter);
            }

            writer.WriteEndArray();
            writer.WritePropertyName("recipe");
            using (var recipe = JsonDocument.Parse(
                       VisualRecipeSerializer.Serialize(graph.Recipe)))
            {
                recipe.RootElement.WriteTo(writer);
            }

            writer.WriteNumber("schemaVersion", CurrentSchemaVersion);
            writer.WriteStartArray("tags");
            foreach (var tag in graph.Tags)
            {
                writer.WriteStringValue(tag);
            }

            writer.WriteEndArray();
            writer.WriteStartArray("channels");
            foreach (var binding in graph.Channels)
            {
                WriteBinding(writer, binding);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static MaterialGraph Deserialize(
        string json,
        VisualNodeRegistry? registry = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var schemaVersion = Required(root, "schemaVersion").GetInt32();
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new JsonException(
                $"Expected material schema version {CurrentSchemaVersion}, got {schemaVersion}.");
        }

        var recipe = VisualRecipeSerializer.Deserialize(
            Required(root, "recipe").GetRawText(),
            registry);
        return new MaterialGraph(
            RequiredString(root, "materialId"),
            RequiredString(root, "displayName"),
            recipe,
            ParseBindings(OptionalArray(root, "channels")),
            ParseParameters(OptionalArray(root, "parameters")),
            ParseLayers(OptionalArray(root, "layers")),
            OptionalArray(root, "tags")
                .EnumerateArray()
                .Select(element => element.GetString() ?? string.Empty));
    }

    public static string SerializeInstance(MaterialInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("instanceId", instance.InstanceId);
            writer.WriteString("parentMaterialId", instance.Parent.MaterialId);
            writer.WriteStartObject("overrides");
            foreach (var pair in instance.Overrides)
            {
                writer.WritePropertyName(pair.Key);
                using var value = JsonDocument.Parse(
                    VisualRecipeSerializer.SerializeValue(pair.Value));
                value.RootElement.WriteTo(writer);
            }

            writer.WriteEndObject();
            writer.WriteNumber("schemaVersion", CurrentSchemaVersion);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static MaterialInstance DeserializeInstance(
        string json,
        MaterialGraph parent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentNullException.ThrowIfNull(parent);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var schemaVersion = Required(root, "schemaVersion").GetInt32();
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new JsonException(
                $"Expected material instance schema version {CurrentSchemaVersion}, got {schemaVersion}.");
        }

        var parentId = RequiredString(root, "parentMaterialId");
        if (!string.Equals(parentId, parent.MaterialId, StringComparison.Ordinal))
        {
            throw new JsonException(
                $"Instance parent '{parentId}' does not match '{parent.MaterialId}'.");
        }

        var overrides = new List<KeyValuePair<string, VisualValue>>();
        foreach (var property in Required(root, "overrides").EnumerateObject())
        {
            overrides.Add(
                new(
                    property.Name,
                    VisualRecipeSerializer.DeserializeValue(
                        property.Value.GetRawText())));
        }

        return new MaterialInstance(
            RequiredString(root, "instanceId"),
            parent,
            overrides);
    }

    private static void WriteBinding(
        Utf8JsonWriter writer,
        MaterialChannelBinding binding)
    {
        writer.WriteStartObject();
        writer.WriteString("channel", binding.Channel.ToString());
        writer.WritePropertyName("contract");
        WriteRaw(
            writer,
            VisualRecipeSerializer.SerializeContract(binding.Contract));
        writer.WritePropertyName("source");
        writer.WriteStartObject();
        writer.WriteString("nodeId", binding.Source.NodeId.ToString());
        writer.WriteString("portId", binding.Source.PortId.Value);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteLayer(Utf8JsonWriter writer, MaterialLayer layer)
    {
        writer.WriteStartObject();
        writer.WriteString("id", layer.Id);
        writer.WriteBoolean("muted", layer.Muted);
        writer.WriteString("name", layer.Name);
        writer.WriteNumber("order", layer.Order);
        writer.WriteBoolean("solo", layer.Solo);
        writer.WriteNumber("weight", layer.Weight);
        writer.WriteEndObject();
    }

    private static void WriteParameter(
        Utf8JsonWriter writer,
        MaterialParameterDefinition parameter)
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
        writer.WriteBoolean("isAnimatable", parameter.IsAnimatable);
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

    private static IEnumerable<MaterialChannelBinding> ParseBindings(
        JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new MaterialChannelBinding(
                ParseEnum<MaterialChannel>(
                    Required(element, "channel"),
                    "channel"),
                ParsePort(Required(element, "source")),
                VisualRecipeSerializer.DeserializeContract(
                    Required(element, "contract").GetRawText()));
        }
    }

    private static IEnumerable<MaterialLayer> ParseLayers(JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new MaterialLayer(
                RequiredString(element, "id"),
                RequiredString(element, "name"),
                Required(element, "order").GetInt32(),
                Required(element, "weight").GetDouble(),
                Required(element, "muted").GetBoolean(),
                Required(element, "solo").GetBoolean());
        }
    }

    private static IEnumerable<MaterialParameterDefinition> ParseParameters(
        JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new MaterialParameterDefinition(
                RequiredString(element, "id"),
                RequiredString(element, "name"),
                VisualRecipeSerializer.DeserializeContract(
                    Required(element, "contract").GetRawText()),
                VisualRecipeSerializer.DeserializeValue(
                    Required(element, "defaultValue").GetRawText()),
                ParseEditor(OptionalObject(element, "editor")),
                Required(element, "isAnimatable").GetBoolean(),
                Required(element, "isInstanceEditable").GetBoolean(),
                ParseTarget(element));
        }
    }

    private static MaterialParameterTarget? ParseTarget(JsonElement element)
    {
        if (!element.TryGetProperty("target", out var target))
        {
            return null;
        }

        return new MaterialParameterTarget(
            NodeId.Parse(RequiredString(target, "nodeId")),
            RequiredString(target, "valueName"));
    }

    private static VisualParameterEditorMetadata ParseEditor(JsonElement element) =>
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

    private static double? OptionalDouble(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) &&
        value.ValueKind != JsonValueKind.Null
            ? value.GetDouble()
            : null;

    private static string? OptionalString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) &&
        value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;

    private static string RequiredString(JsonElement element, string name) =>
        Required(element, name).GetString() ??
        throw new JsonException($"Property '{name}' must be a string.");

    private static JsonElement Required(JsonElement element, string name)
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
