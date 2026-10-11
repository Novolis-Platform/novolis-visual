using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Ordered, explicit allowlist of schema migrations.
/// </summary>
public sealed class VisualSchemaMigrationRegistry
{
    private readonly ImmutableSortedDictionary<int, IVisualSchemaMigration> migrations;

    public VisualSchemaMigrationRegistry(
        IEnumerable<IVisualSchemaMigration>? migrations = null)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<
            int,
            IVisualSchemaMigration>();

        foreach (var migration in migrations ?? [])
        {
            ArgumentNullException.ThrowIfNull(migration);

            if (migration.FromVersion < 0 ||
                migration.ToVersion <= migration.FromVersion)
            {
                throw new ArgumentException(
                    "Migrations must move to a greater schema version.",
                    nameof(migrations));
            }

            if (!builder.TryAdd(migration.FromVersion, migration))
            {
                throw new ArgumentException(
                    $"A migration from version {migration.FromVersion} is already registered.",
                    nameof(migrations));
            }
        }

        this.migrations = builder.ToImmutable();
    }

    public ImmutableArray<IVisualSchemaMigration> Migrations =>
        migrations.Values.ToImmutableArray();

    public string Upgrade(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var parsed = JsonNode.Parse(json) as JsonObject ??
                     throw new JsonException("A visual recipe must be a JSON object.");

        var upgraded = Upgrade(parsed);
        return CanonicalJson.Normalize(upgraded);
    }

    public JsonObject Upgrade(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var current = (JsonObject)document.DeepClone();
        var version = ReadVersion(current);

        if (version > VisualRecipe.CurrentSchemaVersion)
        {
            throw new JsonException(
                $"Schema version {version} is newer than supported version {VisualRecipe.CurrentSchemaVersion}.");
        }

        while (version < VisualRecipe.CurrentSchemaVersion)
        {
            if (!migrations.TryGetValue(version, out var migration))
            {
                throw new JsonException(
                    $"No migration is registered from schema version {version}.");
            }

            if (migration.ToVersion > VisualRecipe.CurrentSchemaVersion)
            {
                throw new JsonException(
                    $"Migration target {migration.ToVersion} is not supported.");
            }

            current = migration.Apply(current) ??
                      throw new InvalidOperationException(
                          "A schema migration returned null.");
            version = migration.ToVersion;
            current["schemaVersion"] = version;
        }

        return current;
    }

    public static VisualSchemaMigrationRegistry CreateDefault() =>
        new([new LegacyVisualSchemaMigration()]);

    private static int ReadVersion(JsonObject document)
    {
        var value = document["schemaVersion"];
        return value is null ? 0 : value.GetValue<int>();
    }
}
