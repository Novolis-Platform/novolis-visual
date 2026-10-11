using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// A rendering-adapter diagnostic with optional source and capability
/// attribution.
/// </summary>
public sealed record VisualRenderingDiagnostic
{
    public VisualRenderingDiagnostic(
        VisualCompileDiagnosticSeverity severity,
        string code,
        string message,
        string? sourcePath = null,
        string? capabilityId = null,
        NodeId? nodeId = null,
        ConnectionId? connectionId = null,
        string? outputId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A diagnostic code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A diagnostic message is required.", nameof(message));
        }

        Severity = severity;
        Code = code;
        Message = message;
        SourcePath = sourcePath;
        CapabilityId = capabilityId;
        NodeId = nodeId;
        ConnectionId = connectionId;
        OutputId = outputId;
    }

    public VisualCompileDiagnosticSeverity Severity { get; init; }

    public string Code { get; init; }

    public string Message { get; init; }

    public string? SourcePath { get; init; }

    public string? CapabilityId { get; init; }

    public NodeId? NodeId { get; init; }

    public ConnectionId? ConnectionId { get; init; }

    public string? OutputId { get; init; }

    /// <summary>
    /// Converts this diagnostic to the common Visual compile diagnostic shape.
    /// </summary>
    public VisualCompileDiagnostic ToCompileDiagnostic() =>
        new(Severity, Code, Message, NodeId, ConnectionId, SourcePath);
}
