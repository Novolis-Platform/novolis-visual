namespace Novolis.Visual.Definitions;

/// <summary>
/// Friendly control metadata for a node or exposed recipe parameter.
/// </summary>
public sealed record VisualParameterEditorMetadata
{
    public VisualParameterEditorMetadata(
        VisualParameterControlKind control = VisualParameterControlKind.Automatic,
        string? displayName = null,
        string? group = null,
        string? description = null,
        double? minimum = null,
        double? maximum = null,
        double? step = null,
        string? unitLabel = null)
    {
        if (minimum is not null && !double.IsFinite(minimum.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(minimum));
        }

        if (maximum is not null && !double.IsFinite(maximum.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(maximum));
        }

        if (minimum is not null &&
            maximum is not null &&
            minimum > maximum)
        {
            throw new ArgumentException(
                "The minimum cannot exceed the maximum.",
                nameof(minimum));
        }

        if (step is not null && (!double.IsFinite(step.Value) || step <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }

        Control = control;
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? null
            : displayName.Trim();
        Group = string.IsNullOrWhiteSpace(group) ? null : group.Trim();
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        Minimum = minimum;
        Maximum = maximum;
        Step = step;
        UnitLabel = string.IsNullOrWhiteSpace(unitLabel)
            ? null
            : unitLabel.Trim();
    }

    public VisualParameterControlKind Control { get; }

    public string? DisplayName { get; }

    public string? Group { get; }

    public string? Description { get; }

    public double? Minimum { get; }

    public double? Maximum { get; }

    public double? Step { get; }

    public string? UnitLabel { get; }
}
