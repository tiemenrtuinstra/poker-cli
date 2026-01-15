using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using TexasHoldem.Network.Messages;

namespace TexasHoldem.Network.Server;

public class PokerServer : IDisposable
{
    private readonly HttpListener _httpListener;
    private readonly ConcurrentDictionary<string, ClientConnection> _clients = new();
    private readonly CancellationTokenSource _serverCts = new();
    private readonly int _port;
    private bool _isRunning;
    private bool _disposed;

    public event Action<string, INetworkMessage>? OnMessageReceived;
    public event Action<ClientConnection>? OnClientConnected;
    public event Action<ClientConnection>? OnClientDisconnected;

    public IReadOnlyDictionary<string, ClientConnection> Clients => _clients;
    public bool IsRunning => _isRunning;

    public PokerServer(int port = 7777)
    {
        _port = port;
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://+:{port}/");
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        try
        {
            _httpListener.Start();
            _isRunning = true;

            Console.WriteLine($"Poker server started on port {_port}");

            // Start heartbeat monitor
            _ = MonitorHeartbeatsAsync(_serverCts.Token);

            // Accept connections
            while (_isRunning && !_serverCts.Token.IsCancellationRequested)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        _ = HandleWebSocketAsync(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (HttpListenerException) when (!_isRunning)
                {
                    break;
                }
            }
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"Server error: {ex.Message}");
            throw;
        }
    }

    private async Task HandleWebSocketAsync(HttpListenerContext context)
    {
        WebSocketContext? wsContext = null;

        try
        {
            wsContext = await context.AcceptWebSocketAsync(null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket accept error: {ex.Message}");
            context.Response.StatusCode = 500;
            context.Response.Close();
            return;
        }

        var socket = wsContext.WebSocket;
        ClientConnection? client = null;

        try
        {
            // Wait for connect message
            var connectMsg = await ReceiveMessageAsync(socket, _serverCts.Token);

            if (connectMsg is not ConnectMessage connect)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.ProtocolError,
                    "Expected connect message",
                    CancellationToken.None);
                return;
            }

            // Create client connection
            var clientId = Guid.NewGuid().ToString("N")[..8];
            client = new ClientConnection(clientId, connect.PlayerName, socket);
            _clients[clientId] = client;

            // Send connect response
            await client.SendAsync(new ConnectResponseMessage
            {
                Success = true,
                ClientId = clientId,
                SessionToken = client.SessionToken
            });

            OnClientConnected?.Invoke(client);

            // Message loop
            while (socket.State == WebSocketState.Open && !_serverCts.Token.IsCancellationRequested)
            {
                var message = await client.ReceiveAsync(_serverCts.Token);

                if (message == null)
                {
                    if (socket.State != WebSocketState.Open)
                        break;
                    continue;
                }

                // Handle heartbeat internally
                if (message is HeartbeatMessage)
                {
                    client.LastHeartbeat = DateTime.UtcNow;
                    await client.SendAsync(new HeartbeatResponseMessage());
                    continue;
                }

                OnMessageReceived?.Invoke(clientId, message);
            }
        }
        catch (WebSocketException)
        {
            // Connection closed
        }
        catch (OperationCanceledException)
        {
            // Server stopping
        }
        finally
        {
            if (client != null)
            {
                _clients.TryRemove(client.Id, out _);
                OnClientDisconnected?.Invoke(client);
                client.Dispose();
            }
            else
            {
                socket.Dispose();
            }
        }
    }

    private async Task<INetworkMessage?> ReceiveMessageAsync(WebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[8192];
        var messageBuilder = new System.Text.StringBuilder();

        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

            if (result.MessageType == WebSocketMessageType.Close)
                return null;

            messageBuilder.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count));
        }
        while (!result.EndOfMessage);

        return MessageSerializer.Deserialize(messageBuilder.ToString());
    }

    private async Task MonitorHeartbeatsAsync(CancellationToken ct)
    {
        var heartbeatTimeout = TimeSpan.FromSeconds(30);

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(10000, ct); // Check every 10 seconds

            var now = DateTime.UtcNow;
            var timedOutClients = _clients.Values
                .Where(c => now - c.LastHeartbeat > heartbeatTimeout)
                .ToList();

            foreach (var client in timedOutClients)
            {
                Console.WriteLine($"Client {client.PlayerName} timed out");
                await DisconnectClientAsync(client.Id, "Heartbeat timeout");
            }
        }
    }

    public async Task BroadcastAsync(INetworkMessage message, string? excludeClientId = null)
    {
        var tasks = _clients.Values
            .Where(c => c.Id != excludeClientId && c.State == ConnectionState.Connected)
            .Select(c => c.SendAsync(message));

        await Task.WhenAll(tasks);
    }

    public async Task BroadcastToLobbyAsync(string lobbyId, INetworkMessage message, string? excludeClientId = null)
    {
        var tasks = _clients.Values
            .Where(c => c.CurrentLobbyId == lobbyId && c.Id != excludeClientId && c.State == ConnectionState.Connected)
            .Select(c => c.SendAsync(message));

        await Task.WhenAll(tasks);
    }

    public async Task SendToClientAsync(string clientId, INetworkMessage message)
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            await client.SendAsync(message);
        }
    }

    public async Task DisconnectClientAsync(string clientId, string? reason = null)
    {
        if (_clients.TryRemove(clientId, out var client))
        {
            await client.SendAsync(new DisconnectMessage { Reason = reason });
            await client.CloseAsync(reason);
            OnClientDisconnected?.Invoke(client);
            client.Dispose();
        }
    }

    public ClientConnection? GetClient(string clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return client;
    }

    public async Task StopAsync()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _serverCts.Cancel();

        // Disconnect all clients
        var disconnectTasks = _clients.Values.Select(c => c.CloseAsync("Server shutting down"));
        await Task.WhenAll(disconnectTasks);

        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }
        _clients.Clear();

        _httpListener.Stop();
        Console.WriteLine("Poker server stopped");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _serverCts.Cancel();
        _serverCts.Dispose();

        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

        _httpListener.Close();
    }
}
