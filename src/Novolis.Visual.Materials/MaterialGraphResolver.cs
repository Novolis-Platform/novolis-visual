using Novolis.Visual.Definitions;

namespace Novolis.Visual.Materials;

/// <summary>
/// Applies material instance values to a graph without introducing a runtime
/// graph interpreter.
/// </summary>
public static class MaterialGraphResolver
{
    public static VisualRecipe Resolve(
        MaterialGraph graph,
        MaterialInstance? instance = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (instance is not null &&
            !ReferenceEquals(instance.Parent, graph) &&
            !string.Equals(
                instance.Parent.MaterialId,
                graph.MaterialId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The instance does not belong to the material graph.",
                nameof(instance));
        }

        var nodes = graph.Recipe.Nodes.ToDictionary(node => node.Id);
        foreach (var parameter in graph.Parameters)
        {
            if (parameter.Target is not { } target ||
                !nodes.TryGetValue(target.NodeId, out var node))
            {
                continue;
            }

            var value = instance?.Overrides.TryGetValue(
                parameter.Id,
                out var overrideValue) == true
                ? overrideValue
                : parameter.DefaultValue;
            nodes[target.NodeId] = node.WithValue(target.ValueName, value);
        }

        return new VisualRecipe(
            graph.Recipe.RecipeId,
            nodes.Values,
            graph.Recipe.Connections,
            graph.Recipe.Parameters,
            graph.Recipe.Outputs,
            graph.Recipe.SeedPolicy,
            graph.Recipe.Dependencies,
            graph.Recipe.Capabilities,
            graph.Recipe.SchemaVersion);
    }
}
