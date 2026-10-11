namespace Novolis.Visual.CodeGen;

/// <summary>
/// Controls deterministic generated source naming and mode.
/// </summary>
public sealed record VisualCodeGenerationOptions
{
    public string Namespace { get; init; } = "Generated.Visuals";

    public string TypeName { get; init; } = "GeneratedVisual";

    public VisualCodeGenerationMode Mode { get; init; } =
        VisualCodeGenerationMode.Specialized;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Namespace))
        {
            throw new ArgumentException(
                "A namespace is required.",
                nameof(Namespace));
        }

        if (string.IsNullOrWhiteSpace(TypeName))
        {
            throw new ArgumentException(
                "A type name is required.",
                nameof(TypeName));
        }

        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(Mode),
                "The code-generation mode is not supported.");
        }
    }
}
