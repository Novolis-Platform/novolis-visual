using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// Controls deterministic compiler lowering.
/// </summary>
public sealed record VisualCompilationOptions
{
    public VisualCompileTarget Target { get; init; } = VisualCompileTarget.Cpu;

    public VisualTargetCapabilities Capabilities { get; init; } =
        VisualTargetCapabilities.Open();

    public VisualNodeRegistry? Registry { get; init; }

    public bool Strict { get; init; } = true;

    public bool Optimize { get; init; } = true;

    public IReadOnlySet<NodeId>? SelectedOutputs { get; init; }
}
