namespace TexasHoldem.Network.Messages;

public interface INetworkMessage
{
    MessageType Type { get; }
    string MessageId { get; }
    DateTime Timestamp { get; }
}
