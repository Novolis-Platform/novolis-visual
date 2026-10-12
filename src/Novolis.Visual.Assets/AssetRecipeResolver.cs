using Novolis.Visual.Definitions;

namespace Novolis.Visual.Assets;

/// <summary>
/// Applies asset instance controls to a recipe before compilation.
/// </summary>
public static class AssetRecipeResolver
{
    public static VisualRecipe Resolve(
        AssetRecipe asset,
        AssetInstance? instance = null)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (instance is not null &&
            !string.Equals(
                instance.Parent.AssetId,
                asset.AssetId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The instance does not belong to the asset recipe.",
                nameof(instance));
        }

        var nodes = asset.Graph.Nodes.ToDictionary(node => node.Id);
        var parameters = asset.Graph.Parameters
            .Select(
                parameter =>
                {
                    var value = instance?.Overrides.TryGetValue(
                        parameter.Id,
                        out var overrideValue) == true
                        ? overrideValue
                        : parameter.Value;
                    return new VisualParameter(
                        parameter.Id,
                        parameter.Name,
                        parameter.Contract,
                        value);
                })
            .ToArray();
        foreach (var parameter in asset.Parameters)
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
            asset.Graph.RecipeId,
            nodes.Values,
            asset.Graph.Connections,
            parameters,
            asset.Graph.Outputs,
            asset.Graph.SeedPolicy,
            asset.Graph.Dependencies,
            asset.Graph.Capabilities,
            asset.Graph.SchemaVersion);
    }
}
