using System.Text.Json.Nodes;

namespace Novolis.Visual.Definitions;

internal sealed class LegacyVisualSchemaMigration : IVisualSchemaMigration
{
    public int FromVersion => 0;

    public int ToVersion => 1;

    public JsonObject Apply(JsonObject document)
    {
        var migrated = (JsonObject)document.DeepClone();

        if (migrated["recipeId"] is null &&
            migrated["id"] is JsonNode legacyId)
        {
            migrated["recipeId"] = legacyId.DeepClone();
        }

        if (migrated["connections"] is null &&
            migrated["links"] is JsonNode links)
        {
            migrated["connections"] = links.DeepClone();
        }

        migrated.Remove("links");
        migrated["recipeId"] ??= "migrated-recipe";
        migrated["nodes"] ??= new JsonArray();
        migrated["connections"] ??= new JsonArray();
        migrated["parameters"] ??= new JsonArray();
        migrated["outputs"] ??= new JsonArray();
        migrated["dependencies"] ??= new JsonArray();
        migrated["capabilities"] ??= new JsonArray();
        migrated["seedPolicy"] ??= new JsonObject
        {
            ["mode"] = nameof(VisualSeedMode.None),
            ["scope"] = null,
            ["seed"] = 0,
        };

        if (migrated["nodes"] is JsonArray nodes)
        {
            foreach (var node in nodes.OfType<JsonObject>())
            {
                node["version"] ??= 1;
                node["values"] ??= new JsonObject();

                if (node["type"] is null &&
                    node["nodeType"] is JsonNode legacyType)
                {
                    node["type"] = legacyType.DeepClone();
                }
            }
        }

        migrated["schemaVersion"] = ToVersion;
        return migrated;
    }
}
