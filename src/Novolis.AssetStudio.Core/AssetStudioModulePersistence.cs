using Novolis.Visual.Assets;
using Novolis.Visual.Definitions;
using Novolis.Visual.Materials;

namespace Novolis.AssetStudio.Core;

/// <summary>
/// Host-neutral persistence facade for reusable material and asset modules.
/// </summary>
public static class AssetStudioModulePersistence
{
    public static string SerializeMaterial(MaterialGraph material) =>
        MaterialGraphSerializer.Serialize(material);

    public static MaterialGraph DeserializeMaterial(
        string json,
        VisualNodeRegistry? registry = null) =>
        MaterialGraphSerializer.Deserialize(json, registry);

    public static string SerializeMaterialInstance(MaterialInstance instance) =>
        MaterialGraphSerializer.SerializeInstance(instance);

    public static MaterialInstance DeserializeMaterialInstance(
        string json,
        MaterialGraph parent) =>
        MaterialGraphSerializer.DeserializeInstance(json, parent);

    public static string SerializeAsset(AssetRecipe asset) =>
        AssetRecipeSerializer.Serialize(asset);

    public static AssetRecipe DeserializeAsset(
        string json,
        VisualNodeRegistry? registry = null) =>
        AssetRecipeSerializer.Deserialize(json, registry);

    public static string SerializeAssetInstance(AssetInstance instance) =>
        AssetRecipeSerializer.SerializeInstance(instance);

    public static AssetInstance DeserializeAssetInstance(
        string json,
        AssetRecipe parent) =>
        AssetRecipeSerializer.DeserializeInstance(json, parent);
}
