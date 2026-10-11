using System.Text.Json.Nodes;

namespace Novolis.Visual.Definitions;

/// <summary>
/// A pure migration from one recipe schema version to the next.
/// </summary>
public interface IVisualSchemaMigration
{
    int FromVersion { get; }

    int ToVersion { get; }

    JsonObject Apply(JsonObject document);
}
