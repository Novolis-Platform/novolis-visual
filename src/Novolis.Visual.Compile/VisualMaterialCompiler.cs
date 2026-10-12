using Novolis.Visual.Assets;
using Novolis.Visual.Definitions;
using Novolis.Visual.Materials;

namespace Novolis.Visual.Compile;

/// <summary>
/// Entry points that compile material and assembled-asset recipes through the
/// shared VisualIR pipeline.
/// </summary>
public static class VisualMaterialCompiler
{
    public static VisualIR CompileMaterial(
        MaterialGraph graph,
        MaterialInstance? instance = null,
        VisualCompileOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var recipe = MaterialGraphResolver.Resolve(graph, instance);
        return VisualCompiler.Compile(
            AddCapabilities(
                recipe,
                recipe.Connections.Select(connection =>
                    connection.Target.PortId.Value switch
                    {
                        "transmission" => VisualMaterialCapabilities.Transmission,
                        "clearcoat" => VisualMaterialCapabilities.Clearcoat,
                        "emissionColor" or "emissionStrength" =>
                            VisualMaterialCapabilities.Emission,
                        _ => null,
                    })),
            options);
    }

    public static VisualIR CompileAsset(
        AssetRecipe asset,
        AssetInstance? instance = null,
        VisualCompileOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var recipe = AssetRecipeResolver.Resolve(asset, instance);
        return VisualCompiler.Compile(
            AddCapabilities(
                recipe,
                [
                    VisualMaterialCapabilities.AssetAssembly,
                    .. asset.Components
                        .Where(component =>
                            component.Kind == AssetComponentKind.MaterialBinding)
                        .Select(_ => VisualMaterialCapabilities.MaterialBinding),
                ]),
            options);
    }

    private static VisualRecipe AddCapabilities(
        VisualRecipe recipe,
        IEnumerable<string?> capabilities)
    {
        var requirements = recipe.Capabilities
            .Concat(
                capabilities
                    .Where(capability => !string.IsNullOrWhiteSpace(capability))
                    .Select(capability =>
                        new VisualCapabilityRequirement(capability!)))
            .GroupBy(capability => capability.Id, StringComparer.Ordinal)
            .Select(group => group.First());
        return new VisualRecipe(
            recipe.RecipeId,
            recipe.Nodes,
            recipe.Connections,
            recipe.Parameters,
            recipe.Outputs,
            recipe.SeedPolicy,
            recipe.Dependencies,
            requirements,
            recipe.SchemaVersion);
    }
}
