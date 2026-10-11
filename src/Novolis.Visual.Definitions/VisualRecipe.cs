using System.Collections.Immutable;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Immutable, versioned semantic representation of a visual program.
/// Editor layout is intentionally not part of this record.
/// </summary>
public sealed record VisualRecipe
{
    public const int CurrentSchemaVersion = 1;

    public VisualRecipe(
        string recipeId,
        IEnumerable<VisualNode>? nodes = null,
        IEnumerable<VisualConnection>? connections = null,
        IEnumerable<VisualParameter>? parameters = null,
        IEnumerable<VisualOutput>? outputs = null,
        VisualSeedPolicy? seedPolicy = null,
        IEnumerable<VisualDependencyFingerprint>? dependencies = null,
        IEnumerable<VisualCapabilityRequirement>? capabilities = null,
        int schemaVersion = CurrentSchemaVersion)
    {
        RecipeId = VisualValueValidation.Required(recipeId, nameof(recipeId));

        if (schemaVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        SchemaVersion = schemaVersion;
        Nodes = CreateNodes(nodes);
        Connections = CreateConnections(connections);
        Parameters = CreateParameters(parameters);
        Outputs = CreateOutputs(outputs);
        SeedPolicy = seedPolicy ?? new VisualSeedPolicy();
        Dependencies = CreateDependencies(dependencies);
        Capabilities = CreateCapabilities(capabilities);
    }

    public string RecipeId { get; }

    public int SchemaVersion { get; }

    public ImmutableArray<VisualNode> Nodes { get; }

    public ImmutableArray<VisualConnection> Connections { get; }

    public ImmutableArray<VisualParameter> Parameters { get; }

    public ImmutableArray<VisualOutput> Outputs { get; }

    public VisualSeedPolicy SeedPolicy { get; }

    public ImmutableArray<VisualDependencyFingerprint> Dependencies { get; }

    public ImmutableArray<VisualCapabilityRequirement> Capabilities { get; }

    public VisualValidationResult Validate(
        VisualNodeRegistry? registry = null) =>
        (registry ?? VisualNodeRegistry.CreateDefault()).Validate(this);

    public string SemanticFingerprint() =>
        VisualRecipeSerializer.SemanticFingerprint(this);

    public static VisualRecipe Empty(string recipeId) => new(recipeId);

    private static ImmutableArray<VisualNode> CreateNodes(
        IEnumerable<VisualNode>? nodes)
    {
        var values = (nodes ?? [])
            .Select(node =>
            {
                ArgumentNullException.ThrowIfNull(node);
                return node;
            })
            .OrderBy(node => node.Id)
            .ToImmutableArray();

        EnsureUnique(
            values.Select(node => node.Id),
            "node identifiers",
            nameof(nodes));

        return values;
    }

    private static ImmutableArray<VisualConnection> CreateConnections(
        IEnumerable<VisualConnection>? connections)
    {
        var values = (connections ?? [])
            .Select(connection =>
            {
                ArgumentNullException.ThrowIfNull(connection);
                return connection;
            })
            .OrderBy(connection => connection.Id)
            .ToImmutableArray();

        EnsureUnique(
            values.Select(connection => connection.Id),
            "connection identifiers",
            nameof(connections));

        return values;
    }

    private static ImmutableArray<VisualParameter> CreateParameters(
        IEnumerable<VisualParameter>? parameters)
    {
        var values = (parameters ?? [])
            .Select(parameter =>
            {
                ArgumentNullException.ThrowIfNull(parameter);
                return parameter;
            })
            .OrderBy(parameter => parameter.Id, StringComparer.Ordinal)
            .ToImmutableArray();

        EnsureUnique(
            values.Select(parameter => parameter.Id),
            "parameter identifiers",
            nameof(parameters));

        return values;
    }

    private static ImmutableArray<VisualOutput> CreateOutputs(
        IEnumerable<VisualOutput>? outputs)
    {
        var values = (outputs ?? [])
            .Select(output =>
            {
                ArgumentNullException.ThrowIfNull(output);
                return output;
            })
            .OrderBy(output => output.Id, StringComparer.Ordinal)
            .ToImmutableArray();

        EnsureUnique(
            values.Select(output => output.Id),
            "output identifiers",
            nameof(outputs));

        return values;
    }

    private static ImmutableArray<VisualDependencyFingerprint> CreateDependencies(
        IEnumerable<VisualDependencyFingerprint>? dependencies)
    {
        var values = (dependencies ?? [])
            .Select(dependency =>
            {
                ArgumentNullException.ThrowIfNull(dependency);
                return dependency;
            })
            .OrderBy(dependency => dependency.Id, StringComparer.Ordinal)
            .ToImmutableArray();

        EnsureUnique(
            values.Select(dependency => dependency.Id),
            "dependency identifiers",
            nameof(dependencies));

        return values;
    }

    private static ImmutableArray<VisualCapabilityRequirement> CreateCapabilities(
        IEnumerable<VisualCapabilityRequirement>? capabilities)
    {
        var values = (capabilities ?? [])
            .Select(capability =>
            {
                ArgumentNullException.ThrowIfNull(capability);
                return capability;
            })
            .OrderBy(capability => capability.Id, StringComparer.Ordinal)
            .ToImmutableArray();

        EnsureUnique(
            values.Select(capability => capability.Id),
            "capability identifiers",
            nameof(capabilities));

        return values;
    }

    private static void EnsureUnique<T>(
        IEnumerable<T> values,
        string description,
        string parameterName)
    {
        var valueArray = values.ToArray();
        if (valueArray.Distinct().Count() != valueArray.Length)
        {
            throw new ArgumentException(
                $"Duplicate {description} are not allowed.",
                parameterName);
        }
    }
}
