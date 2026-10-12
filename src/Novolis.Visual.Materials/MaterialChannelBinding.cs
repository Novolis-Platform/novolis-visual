using Novolis.Visual.Definitions;

namespace Novolis.Visual.Materials;

/// <summary>
/// Binds a material channel to a recipe output port.
/// </summary>
public sealed record MaterialChannelBinding
{
    public MaterialChannelBinding(
        MaterialChannel channel,
        VisualPortReference source,
        VisualTypeContract? contract = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(contract);

        var expected = MaterialChannelContract.For(channel);
        if (!expected.IsValueCompatibleWith(contract))
        {
            throw new ArgumentException(
                $"Channel '{channel}' requires contract '{expected}'.",
                nameof(contract));
        }

        Channel = channel;
        Source = source;
        Contract = contract;
    }

    public MaterialChannel Channel { get; }

    public VisualPortReference Source { get; }

    public VisualTypeContract Contract { get; }
}
