using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Canonical JSON serializer and reader for semantic visual recipes.
/// Collections and object keys are sorted; editor view state is never included.
/// </summary>
public static class VisualRecipeSerializer
{
    public static string ToCanonicalJson(VisualRecipe recipe) =>
        Serialize(recipe);

    public static string Serialize(VisualRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(
                   stream,
                   new JsonWriterOptions
                   {
                       Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default,
                   }))
        {
            writer.WriteStartObject();

            writer.WritePropertyName("capabilities");
            writer.WriteStartArray();
            foreach (var capability in recipe.Capabilities)
            {
                WriteCapability(writer, capability);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("connections");
            writer.WriteStartArray();
            foreach (var connection in recipe.Connections)
            {
                WriteConnection(writer, connection);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("dependencies");
            writer.WriteStartArray();
            foreach (var dependency in recipe.Dependencies)
            {
                WriteDependency(writer, dependency);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("nodes");
            writer.WriteStartArray();
            foreach (var node in recipe.Nodes)
            {
                WriteNode(writer, node);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("outputs");
            writer.WriteStartArray();
            foreach (var output in recipe.Outputs)
            {
                WriteOutput(writer, output);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("parameters");
            writer.WriteStartArray();
            foreach (var parameter in recipe.Parameters)
            {
                WriteParameter(writer, parameter);
            }

            writer.WriteEndArray();

            writer.WriteString("recipeId", recipe.RecipeId);
            writer.WriteNumber("schemaVersion", recipe.SchemaVersion);

            writer.WritePropertyName("seedPolicy");
            WriteSeedPolicy(writer, recipe.SeedPolicy);

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static string SemanticFingerprint(VisualRecipe recipe)
    {
        var bytes = Encoding.UTF8.GetBytes(Serialize(recipe));
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    public static VisualRecipe Deserialize(
        string json,
        VisualNodeRegistry? registry = null,
        VisualSchemaMigrationRegistry? migrations = null)
    {
        ArgumentNullException.ThrowIfNull(json);

        var migrationRegistry =
            migrations ?? VisualSchemaMigrationRegistry.CreateDefault();
        var upgraded = migrationRegistry.Upgrade(json);

        using var document = JsonDocument.Parse(upgraded);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("A visual recipe must be a JSON object.");
        }

        var schemaVersion = Required(root, "schemaVersion").GetInt32();
        if (schemaVersion != VisualRecipe.CurrentSchemaVersion)
        {
            throw new JsonException(
                $"Expected schema version {VisualRecipe.CurrentSchemaVersion}, got {schemaVersion}.");
        }

        var nodes = ParseNodes(
            OptionalArray(root, "nodes"),
            registry ?? VisualNodeRegistry.CreateDefault());

        return new VisualRecipe(
            Required(root, "recipeId").GetString() ?? string.Empty,
            nodes,
            ParseConnections(OptionalArray(root, "connections")),
            ParseParameters(OptionalArray(root, "parameters")),
            ParseOutputs(OptionalArray(root, "outputs")),
            ParseSeedPolicy(root),
            ParseDependencies(OptionalArray(root, "dependencies")),
            ParseCapabilities(OptionalArray(root, "capabilities")),
            schemaVersion);
    }

    public static VisualRecipe FromCanonicalJson(
        string json,
        VisualNodeRegistry? registry = null,
        VisualSchemaMigrationRegistry? migrations = null) =>
        Deserialize(json, registry, migrations);

    private static void WriteNode(Utf8JsonWriter writer, VisualNode node)
    {
        if (node.IsUnknown)
        {
            writer.WriteRawValue(node.UnknownPayloadJson!);
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("id", node.Id.ToString());
        writer.WriteString("type", node.TypeId);
        writer.WritePropertyName("values");
        writer.WriteStartObject();
        foreach (var value in node.Values)
        {
            writer.WritePropertyName(value.Key);
            VisualValueJson.WriteValue(writer, value.Value);
        }

        writer.WriteEndObject();
        writer.WriteNumber("version", node.Version);
        writer.WriteEndObject();
    }

    private static void WriteConnection(
        Utf8JsonWriter writer,
        VisualConnection connection)
    {
        writer.WriteStartObject();
        writer.WriteString("id", connection.Id.ToString());
        writer.WritePropertyName("source");
        WritePortReference(writer, connection.Source);
        writer.WritePropertyName("sourceContract");
        VisualValueJson.WriteContract(writer, connection.SourceContract);
        writer.WritePropertyName("target");
        WritePortReference(writer, connection.Target);
        writer.WritePropertyName("targetContract");
        VisualValueJson.WriteContract(writer, connection.TargetContract);
        writer.WriteEndObject();
    }

    private static void WritePortReference(
        Utf8JsonWriter writer,
        VisualPortReference reference)
    {
        writer.WriteStartObject();
        writer.WriteString("nodeId", reference.NodeId.ToString());
        writer.WriteString("portId", reference.PortId.Value);
        writer.WriteEndObject();
    }

    private static void WriteParameter(
        Utf8JsonWriter writer,
        VisualParameter parameter)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("contract");
        VisualValueJson.WriteContract(writer, parameter.Contract);
        writer.WriteString("id", parameter.Id);
        writer.WriteString("name", parameter.Name);
        writer.WritePropertyName("value");
        VisualValueJson.WriteValue(writer, parameter.Value);
        writer.WriteEndObject();
    }

    private static void WriteOutput(Utf8JsonWriter writer, VisualOutput output)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("contract");
        VisualValueJson.WriteContract(writer, output.Contract);
        writer.WriteString("id", output.Id);
        writer.WriteString("name", output.Name);
        writer.WriteBoolean("required", output.Required);
        writer.WritePropertyName("source");
        WritePortReference(writer, output.Source);
        writer.WriteEndObject();
    }

    private static void WriteSeedPolicy(
        Utf8JsonWriter writer,
        VisualSeedPolicy policy)
    {
        writer.WriteStartObject();
        writer.WriteString("mode", policy.Mode.ToString());
        writer.WriteString("scope", policy.Scope);
        writer.WriteNumber("seed", policy.Seed);
        writer.WriteEndObject();
    }

    private static void WriteDependency(
        Utf8JsonWriter writer,
        VisualDependencyFingerprint dependency)
    {
        writer.WriteStartObject();
        writer.WriteString("algorithm", dependency.Algorithm);
        writer.WriteString("fingerprint", dependency.Fingerprint);
        writer.WriteString("id", dependency.Id);
        writer.WriteEndObject();
    }

    private static void WriteCapability(
        Utf8JsonWriter writer,
        VisualCapabilityRequirement capability)
    {
        writer.WriteStartObject();
        writer.WriteString("id", capability.Id);
        writer.WriteString("minimumVersion", capability.MinimumVersion);
        writer.WriteBoolean("required", capability.Required);
        writer.WriteEndObject();
    }

    private static IEnumerable<VisualNode> ParseNodes(
        JsonElement array,
        VisualNodeRegistry registry)
    {
        foreach (var element in array.EnumerateArray())
        {
            var id = NodeId.Parse(GetRequiredString(element, "id"));
            var typeId = GetRequiredString(element, "type");
            var version = Required(element, "version").GetInt32();

            if (!registry.TryGetDescriptor(typeId, out var descriptor) ||
                descriptor is null ||
                version > descriptor.Version)
            {
                yield return VisualNode.FromUnknownPayload(
                    id,
                    typeId,
                    version,
                    CanonicalJson.Write(element));
                continue;
            }

            var values = new List<KeyValuePair<string, VisualValue>>();
            foreach (var value in OptionalObject(element, "values").EnumerateObject())
            {
                values.Add(new(value.Name, VisualValueJson.ParseValue(value.Value)));
            }

            yield return VisualNode.Create(id, typeId, version, values);
        }
    }

    private static IEnumerable<VisualConnection> ParseConnections(
        JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new VisualConnection(
                ConnectionId.Parse(GetRequiredString(element, "id")),
                ParsePortReference(Required(element, "source")),
                ParsePortReference(Required(element, "target")),
                VisualValueJson.ParseContract(
                    Required(element, "sourceContract")),
                VisualValueJson.ParseContract(
                    Required(element, "targetContract")));
        }
    }

    private static IEnumerable<VisualParameter> ParseParameters(
        JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new VisualParameter(
                GetRequiredString(element, "id"),
                GetRequiredString(element, "name"),
                VisualValueJson.ParseContract(Required(element, "contract")),
                VisualValueJson.ParseValue(Required(element, "value")));
        }
    }

    private static IEnumerable<VisualOutput> ParseOutputs(JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new VisualOutput(
                GetRequiredString(element, "id"),
                GetRequiredString(element, "name"),
                ParsePortReference(Required(element, "source")),
                VisualValueJson.ParseContract(Required(element, "contract")),
                Required(element, "required").GetBoolean());
        }
    }

    private static VisualSeedPolicy ParseSeedPolicy(JsonElement root)
    {
        if (!root.TryGetProperty("seedPolicy", out var element))
        {
            return new VisualSeedPolicy();
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Property 'seedPolicy' must be an object.");
        }

        return new VisualSeedPolicy(
            ParseEnum<VisualSeedMode>(
                Required(element, "mode"),
                "mode"),
            Required(element, "seed").GetUInt64(),
            OptionalString(element, "scope"));
    }

    private static IEnumerable<VisualDependencyFingerprint> ParseDependencies(
        JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new VisualDependencyFingerprint(
                GetRequiredString(element, "id"),
                GetRequiredString(element, "algorithm"),
                GetRequiredString(element, "fingerprint"));
        }
    }

    private static IEnumerable<VisualCapabilityRequirement> ParseCapabilities(
        JsonElement array)
    {
        foreach (var element in array.EnumerateArray())
        {
            yield return new VisualCapabilityRequirement(
                GetRequiredString(element, "id"),
                OptionalString(element, "minimumVersion"),
                Required(element, "required").GetBoolean());
        }
    }

    private static VisualPortReference ParsePortReference(JsonElement element)
    {
        return new VisualPortReference(
            NodeId.Parse(GetRequiredString(element, "nodeId")),
            new PortId(GetRequiredString(element, "portId")));
    }

    private static JsonElement OptionalArray(JsonElement element, string name)
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

    private static JsonElement OptionalObject(JsonElement element, string name)
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

    private static string? OptionalString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) ||
            value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.GetString();
    }

    private static string GetRequiredString(JsonElement element, string name) =>
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

    private static T ParseEnum<T>(JsonElement element, string name)
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
