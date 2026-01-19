using System.Net.WebSockets;
using System.Text;
using TexasHoldem.Game.Enums;
using TexasHoldem.Network.Messages;

namespace TexasHoldem.Network.Client;

public enum ClientState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}

public class PokerClient : IDisposable
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _clientCts;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    public string? ClientId { get; private set; }
    public string? SessionToken { get; private set; }
    public string PlayerName { get; }
    public ClientState State { get; private set; } = ClientState.Disconnected;
    public string? CurrentLobbyId { get; set; }

    public event Action<INetworkMessage>? OnMessageReceived;
    public event Action? OnConnected;
    public event Action<string?>? OnDisconnected;
    public event Action<Exception>? OnError;

    private readonly int _heartbeatIntervalMs;
    private readonly int _reconnectMaxAttempts;
    private string? _lastServerUri;

    public PokerClient(string playerName, int heartbeatIntervalMs = 5000, int reconnectMaxAttempts = 5)
    {
        PlayerName = playerName;
        _heartbeatIntervalMs = heartbeatIntervalMs;
        _reconnectMaxAttempts = reconnectMaxAttempts;
    }

    public async Task<bool> ConnectAsync(string host, int port, string? lobbyCode = null, CancellationToken ct = default)
    {
        if (State == ClientState.Connected)
            return true;

        _lastServerUri = $"ws://{host}:{port}/";
        State = ClientState.Connecting;

        try
        {
            _clientCts = new CancellationTokenSource();
            _socket = new ClientWebSocket();

            await _socket.ConnectAsync(new Uri(_lastServerUri), ct);

            // Send connect message
            await SendAsync(new ConnectMessage
            {
                PlayerName = PlayerName,
                LobbyCode = lobbyCode,
                SessionToken = SessionToken
            });

            // Wait for connect response
            var response = await ReceiveMessageAsync(_clientCts.Token);

            if (response is ConnectResponseMessage connectResponse)
            {
                if (connectResponse.Success)
                {
                    ClientId = connectResponse.ClientId;
                    SessionToken = connectResponse.SessionToken;
                    State = ClientState.Connected;

                    // Start message loop and heartbeat
                    _ = MessageLoopAsync(_clientCts.Token);
                    _ = HeartbeatLoopAsync(_clientCts.Token);

                    OnConnected?.Invoke();
                    return true;
                }
                else
                {
                    OnError?.Invoke(new Exception(connectResponse.Error ?? "Connection rejected"));
                    await DisconnectAsync();
                    return false;
                }
            }

            await DisconnectAsync();
            return false;
        }
        catch (Exception ex)
        {
            State = ClientState.Disconnected;
            OnError?.Invoke(ex);
            return false;
        }
    }

    public async Task<bool> ReconnectAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_lastServerUri) || string.IsNullOrEmpty(SessionToken))
            return false;

        State = ClientState.Reconnecting;

        for (int attempt = 1; attempt <= _reconnectMaxAttempts; attempt++)
        {
            try
            {
                _socket?.Dispose();
                _socket = new ClientWebSocket();

                await _socket.ConnectAsync(new Uri(_lastServerUri), ct);

                // Send reconnect with session token
                await SendAsync(new ReconnectMessage
                {
                    PlayerName = PlayerName,
                    SessionToken = SessionToken
                });

                var response = await ReceiveMessageAsync(ct);

                // Handle both ReconnectResponse and ConnectResponse for backwards compatibility
                if (response is ReconnectResponseMessage reconnectResponse && reconnectResponse.Success)
                {
                    ClientId = reconnectResponse.ClientId;
                    CurrentLobbyId = reconnectResponse.LobbyCode;
                    State = ClientState.Connected;

                    _clientCts?.Dispose();
                    _clientCts = new CancellationTokenSource();

                    _ = MessageLoopAsync(_clientCts.Token);
                    _ = HeartbeatLoopAsync(_clientCts.Token);

                    OnConnected?.Invoke();

                    // If we got game state, notify handlers
                    if (reconnectResponse.GameState != null)
                    {
                        OnMessageReceived?.Invoke(new GameStateSyncMessage { State = reconnectResponse.GameState });
                    }

                    return true;
                }
                else if (response is ConnectResponseMessage connectResponse && connectResponse.Success)
                {
                    ClientId = connectResponse.ClientId;
                    State = ClientState.Connected;

                    _clientCts?.Dispose();
                    _clientCts = new CancellationTokenSource();

                    _ = MessageLoopAsync(_clientCts.Token);
                    _ = HeartbeatLoopAsync(_clientCts.Token);

                    OnConnected?.Invoke();
                    return true;
                }
            }
            catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or HttpRequestException or InvalidOperationException)
            {
                // Expected network errors - log and retry with exponential backoff
                Console.WriteLine($"Reconnection attempt {attempt}/{_reconnectMaxAttempts} failed: {ex.GetType().Name}");
                var delay = Math.Min(1000 * (int)Math.Pow(2, attempt - 1), 30000);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }

        State = ClientState.Disconnected;
        OnDisconnected?.Invoke("Reconnection failed");
        return false;
    }

    private async Task MessageLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _socket?.State == WebSocketState.Open)
            {
                var message = await ReceiveMessageAsync(ct);

                if (message == null)
                {
                    if (_socket?.State != WebSocketState.Open)
                        break;
                    continue;
                }

                // Handle heartbeat response internally
                if (message is HeartbeatResponseMessage)
                    continue;

                // Handle disconnect message
                if (message is DisconnectMessage disconnect)
                {
                    await DisconnectAsync(disconnect.Reason);
                    return;
                }

                OnMessageReceived?.Invoke(message);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on disconnect
        }
        catch (WebSocketException)
        {
            // Connection lost, try to reconnect
            if (State == ClientState.Connected)
            {
                _ = ReconnectAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && State == ClientState.Connected)
            {
                await Task.Delay(_heartbeatIntervalMs, ct);

                if (State == ClientState.Connected)
                {
                    await SendAsync(new HeartbeatMessage());
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on disconnect
        }
    }

    public async Task SendAsync(INetworkMessage message)
    {
        if (_disposed || _socket?.State != WebSocketState.Open)
            return;

        await _sendLock.WaitAsync();
        try
        {
            var json = MessageSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task<INetworkMessage?> ReceiveMessageAsync(CancellationToken ct)
    {
        if (_socket == null || _socket.State != WebSocketState.Open)
            return null;

        var buffer = new byte[8192];
        var messageBuilder = new StringBuilder();

        try
        {
            WebSocketReceiveResult result;
            do
            {
                result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    State = ClientState.Disconnected;
                    return null;
                }

                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            return MessageSerializer.Deserialize(messageBuilder.ToString());
        }
        catch
        {
            return null;
        }
    }

    public async Task DisconnectAsync(string? reason = null)
    {
        if (State == ClientState.Disconnected)
            return;

        State = ClientState.Disconnected;
        _clientCts?.Cancel();

        if (_socket?.State == WebSocketState.Open)
        {
            try
            {
                await _socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    reason ?? "Client disconnecting",
                    CancellationToken.None);
            }
            catch
            {
                // Ignore close errors
            }
        }

        OnDisconnected?.Invoke(reason);
    }

    #region Lobby Operations

    public async Task<CreateLobbyResponseMessage?> CreateLobbyAsync(LobbySettings settings)
    {
        var tcs = new TaskCompletionSource<CreateLobbyResponseMessage>();

        void Handler(INetworkMessage msg)
        {
            if (msg is CreateLobbyResponseMessage response)
            {
                OnMessageReceived -= Handler;
                tcs.TrySetResult(response);
            }
        }

        OnMessageReceived += Handler;

        await SendAsync(new CreateLobbyMessage { Settings = settings });

        using var cts = new CancellationTokenSource(10000);
        cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            OnMessageReceived -= Handler;
            return null;
        }
    }

    public async Task<JoinLobbyResponseMessage?> JoinLobbyAsync(string lobbyCode, string? password = null)
    {
        var tcs = new TaskCompletionSource<JoinLobbyResponseMessage>();

        void Handler(INetworkMessage msg)
        {
            if (msg is JoinLobbyResponseMessage response)
            {
                OnMessageReceived -= Handler;
                tcs.TrySetResult(response);
            }
        }

        OnMessageReceived += Handler;

        await SendAsync(new JoinLobbyMessage { LobbyCode = lobbyCode, Password = password });

        using var cts = new CancellationTokenSource(10000);
        cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            var result = await tcs.Task;
            if (result.Success)
            {
                CurrentLobbyId = lobbyCode;
            }
            return result;
        }
        catch (OperationCanceledException)
        {
            OnMessageReceived -= Handler;
            return null;
        }
    }

    public async Task LeaveLobbyAsync()
    {
        await SendAsync(new LeaveLobbyMessage());
        CurrentLobbyId = null;
    }

    public async Task SetReadyAsync(bool isReady)
    {
        await SendAsync(new PlayerReadyMessage { IsReady = isReady });
    }

    public async Task<ListLobbiesResponseMessage?> ListLobbiesAsync()
    {
        var tcs = new TaskCompletionSource<ListLobbiesResponseMessage>();

        void Handler(INetworkMessage msg)
        {
            if (msg is ListLobbiesResponseMessage response)
            {
                OnMessageReceived -= Handler;
                tcs.TrySetResult(response);
            }
        }

        OnMessageReceived += Handler;

        await SendAsync(new ListLobbiesMessage());

        using var cts = new CancellationTokenSource(10000);
        cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            OnMessageReceived -= Handler;
            return null;
        }
    }

    #endregion

    #region Game Operations

    public async Task SendActionAsync(ActionType action, int amount = 0)
    {
        if (ClientId == null) return;

        await SendAsync(new ActionResponseMessage
        {
            PlayerId = ClientId,
            Action = action,
            Amount = amount
        });
    }

    public async Task RequestStartGameAsync()
    {
        await SendAsync(new StartGameMessage());
    }

    #endregion

    #region Chat Operations

    public async Task SendChatMessageAsync(string content)
    {
        if (ClientId == null) return;

        await SendAsync(new ChatMessageMessage
        {
            SenderId = ClientId,
            SenderName = PlayerName,
            Content = content,
            IsAi = false,
            ChatType = ChatMessageType.PlayerMessage
        });
    }

    #endregion

    #region Admin Operations

    public async Task KickPlayerAsync(string playerId, string? reason = null)
    {
        await SendAsync(new KickPlayerMessage { PlayerId = playerId, Reason = reason });
    }

    public async Task TransferHostAsync(string newHostId)
    {
        await SendAsync(new TransferHostMessage { NewHostId = newHostId });
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _clientCts?.Cancel();
        _clientCts?.Dispose();
        _sendLock.Dispose();
        _socket?.Dispose();
    }
}
