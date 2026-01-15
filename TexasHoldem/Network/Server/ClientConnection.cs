using System.Net.WebSockets;
using System.Text;
using TexasHoldem.Network.Messages;

namespace TexasHoldem.Network.Server;

public enum ConnectionState
{
    Connected,
    Disconnected,
    Reconnecting
}

public class ClientConnection : IDisposable
{
    public string Id { get; }
    public string PlayerName { get; set; }
    public WebSocket Socket { get; }
    public string? CurrentLobbyId { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public ConnectionState State { get; set; }
    public string? SessionToken { get; }

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    public ClientConnection(string id, string playerName, WebSocket socket)
    {
        Id = id;
        PlayerName = playerName;
        Socket = socket;
        State = ConnectionState.Connected;
        LastHeartbeat = DateTime.UtcNow;
        SessionToken = Guid.NewGuid().ToString("N");
    }

    public async Task SendAsync(INetworkMessage message, CancellationToken ct = default)
    {
        if (_disposed || Socket.State != WebSocketState.Open)
            return;

        await _sendLock.WaitAsync(ct);
        try
        {
            var json = MessageSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await Socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task<INetworkMessage?> ReceiveAsync(CancellationToken ct)
    {
        if (_disposed || Socket.State != WebSocketState.Open)
            return null;

        var buffer = new byte[8192];
        var messageBuilder = new StringBuilder();

        try
        {
            WebSocketReceiveResult result;
            do
            {
                result = await Socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    State = ConnectionState.Disconnected;
                    return null;
                }

                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            var json = messageBuilder.ToString();
            return MessageSerializer.Deserialize(json);
        }
        catch (WebSocketException)
        {
            State = ConnectionState.Disconnected;
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public async Task CloseAsync(string? reason = null)
    {
        if (_disposed || Socket.State != WebSocketState.Open)
            return;

        try
        {
            await Socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                reason ?? "Connection closed",
                CancellationToken.None);
        }
        catch
        {
            // Ignore close errors
        }

        State = ConnectionState.Disconnected;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _sendLock.Dispose();
        Socket.Dispose();
    }
}
