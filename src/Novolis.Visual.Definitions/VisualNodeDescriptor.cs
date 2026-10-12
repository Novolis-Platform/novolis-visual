using System.Collections.Immutable;

namespace Novolis.Visual.Definitions;

/// <summary>
/// The allowlisted schema descriptor for one node type and version.
/// </summary>
public sealed record VisualNodeDescriptor
{
    public VisualNodeDescriptor(
        string typeId,
        int version,
        string displayName,
        IEnumerable<VisualPortDescriptor>? ports = null,
        IEnumerable<VisualParameterDescriptor>? parameters = null,
        VisualAttributeRequirement requiredAttributes = VisualAttributeRequirement.None,
        VisualNodeEditorMetadata? editor = null)
    {
        TypeId = VisualValueValidation.Required(typeId, nameof(typeId));
        DisplayName = VisualValueValidation.Required(displayName, nameof(displayName));

        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        Version = version;
        Ports = CreatePorts(ports);
        Parameters = CreateParameters(parameters);
        RequiredAttributes = requiredAttributes;
        Editor = editor ?? new VisualNodeEditorMetadata(
            VisualNodeCategory.Advanced);
    }

    public string TypeId { get; }

    public int Version { get; }

    public string DisplayName { get; }

    public ImmutableArray<VisualPortDescriptor> Ports { get; }

    public ImmutableArray<VisualParameterDescriptor> Parameters { get; }

    public VisualAttributeRequirement RequiredAttributes { get; }

    public VisualNodeEditorMetadata Editor { get; }

    public VisualPortDescriptor? FindPort(PortId id) =>
        Ports.FirstOrDefault(port => port.Id == id);

    private static ImmutableArray<VisualPortDescriptor> CreatePorts(
        IEnumerable<VisualPortDescriptor>? ports)
    {
        var values = (ports ?? [])
            .Select(port =>
            {
                ArgumentNullException.ThrowIfNull(port);
                return port;
            })
            .OrderBy(port => port.Id.Value, StringComparer.Ordinal)
            .ToImmutableArray();

        if (values.Select(port => port.Id).Distinct().Count() != values.Length)
        {
            throw new ArgumentException(
                "A node descriptor cannot contain duplicate ports.",
                nameof(ports));
        }

        return values;
    }

    private static ImmutableArray<VisualParameterDescriptor> CreateParameters(
        IEnumerable<VisualParameterDescriptor>? parameters)
    {
        var values = (parameters ?? [])
            .Select(parameter =>
            {
                ArgumentNullException.ThrowIfNull(parameter);
                return parameter;
            })
            .OrderBy(parameter => parameter.Name, StringComparer.Ordinal)
            .ToImmutableArray();

        if (values
            .Select(parameter => parameter.Name)
            .Distinct(StringComparer.Ordinal)
            .Count() != values.Length)
        {
            throw new ArgumentException(
                "A node descriptor cannot contain duplicate parameters.",
                nameof(parameters));
        }

        return values;
    }
}
