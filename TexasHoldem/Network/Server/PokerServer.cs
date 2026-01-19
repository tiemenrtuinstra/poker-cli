using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TexasHoldem.Network.Messages;

namespace TexasHoldem.Network.Server;

public class PokerServer : IDisposable
{
    private readonly TcpListener _tcpListener;
    private readonly ConcurrentDictionary<string, ClientConnection> _clients = new();
    private readonly CancellationTokenSource _serverCts = new();
    private readonly int _port;
    private bool _isRunning;
    private bool _disposed;
    private string? _startupError;

    /// <summary>
    /// Maximum allowed message size in bytes (1MB) to prevent DoS attacks
    /// </summary>
    private const int MaxMessageSize = 1024 * 1024;

    public event Action<string, INetworkMessage>? OnMessageReceived;
    public event Action<ClientConnection>? OnClientConnected;
    public event Action<ClientConnection>? OnClientDisconnected;

    public IReadOnlyDictionary<string, ClientConnection> Clients => _clients;
    public bool IsRunning => _isRunning;
    public string? StartupError => _startupError;

    public PokerServer(int port = 7777)
    {
        _port = port;
        // Bind to all interfaces - no admin privileges required with TcpListener
        _tcpListener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        try
        {
            _tcpListener.Start();
            _isRunning = true;
            _startupError = null;

            Console.WriteLine($"Poker server started on port {_port}");
            Console.WriteLine($"Other players can connect using your IP address and port {_port}");

            // Start heartbeat monitor
            _ = MonitorHeartbeatsAsync(_serverCts.Token);

            // Accept connections
            while (_isRunning && !_serverCts.Token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync(_serverCts.Token);
                    _ = HandleTcpClientAsync(tcpClient);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException) when (!_isRunning)
                {
                    break;
                }
            }
        }
        catch (SocketException ex)
        {
            _startupError = ex.Message;
            Console.WriteLine($"Server error: {ex.Message}");

            if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Console.WriteLine($"Port {_port} is already in use. Try a different port.");
            }
            else if (ex.SocketErrorCode == SocketError.AccessDenied)
            {
                Console.WriteLine("Access denied. Try running as Administrator or use a different port.");
            }

            throw;
        }
    }

    private async Task HandleTcpClientAsync(TcpClient tcpClient)
    {
        NetworkStream? stream = null;
        ClientConnection? client = null;

        try
        {
            stream = tcpClient.GetStream();

            // Perform WebSocket handshake
            if (!await PerformWebSocketHandshakeAsync(stream, _serverCts.Token))
            {
                tcpClient.Close();
                return;
            }

            // Now we have a WebSocket connection - wrap it
            var webSocket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                IsServer = true,
                KeepAliveInterval = TimeSpan.FromSeconds(30)
            });

            // Wait for connect message
            var connectMsg = await ReceiveMessageAsync(webSocket, _serverCts.Token);

            if (connectMsg is not ConnectMessage connect)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.ProtocolError,
                    "Expected connect message",
                    CancellationToken.None);
                return;
            }

            // Create client connection
            var clientId = Guid.NewGuid().ToString("N")[..8];
            client = new ClientConnection(clientId, connect.PlayerName, webSocket);
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
            while (webSocket.State == WebSocketState.Open && !_serverCts.Token.IsCancellationRequested)
            {
                var message = await client.ReceiveAsync(_serverCts.Token);

                if (message == null)
                {
                    if (webSocket.State != WebSocketState.Open)
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
        catch (Exception ex)
        {
            Console.WriteLine($"Client handler error: {ex.Message}");
        }
        finally
        {
            if (client != null)
            {
                _clients.TryRemove(client.Id, out _);
                OnClientDisconnected?.Invoke(client);
                client.Dispose();
            }

            tcpClient.Close();
        }
    }

    private async Task<bool> PerformWebSocketHandshakeAsync(NetworkStream stream, CancellationToken ct)
    {
        // Read HTTP request
        var buffer = new byte[4096];
        var requestBuilder = new StringBuilder();

        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
        if (bytesRead == 0) return false;

        requestBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
        var request = requestBuilder.ToString();

        // Check for WebSocket upgrade request
        if (!request.Contains("Upgrade: websocket", StringComparison.OrdinalIgnoreCase))
        {
            // Send 400 Bad Request
            var badRequest = "HTTP/1.1 400 Bad Request\r\n\r\n";
            await stream.WriteAsync(Encoding.UTF8.GetBytes(badRequest), ct);
            return false;
        }

        // Extract Sec-WebSocket-Key
        var keyMatch = Regex.Match(request, @"Sec-WebSocket-Key:\s*(.+)", RegexOptions.IgnoreCase);
        if (!keyMatch.Success)
        {
            var badRequest = "HTTP/1.1 400 Bad Request\r\n\r\n";
            await stream.WriteAsync(Encoding.UTF8.GetBytes(badRequest), ct);
            return false;
        }

        var key = keyMatch.Groups[1].Value.Trim();

        // Calculate accept key
        var acceptKey = ComputeWebSocketAcceptKey(key);

        // Send handshake response
        var response = $"HTTP/1.1 101 Switching Protocols\r\n" +
                      $"Upgrade: websocket\r\n" +
                      $"Connection: Upgrade\r\n" +
                      $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";

        await stream.WriteAsync(Encoding.UTF8.GetBytes(response), ct);
        return true;
    }

    private static string ComputeWebSocketAcceptKey(string key)
    {
        const string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        var combined = key + guid;
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }

    private async Task<INetworkMessage?> ReceiveMessageAsync(WebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[8192];
        var messageBuilder = new StringBuilder();
        var totalBytesReceived = 0;

        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Close)
                return null;

            totalBytesReceived += result.Count;

            // Prevent DoS attacks by limiting message size
            if (totalBytesReceived > MaxMessageSize)
            {
                throw new InvalidOperationException($"Message size exceeds maximum allowed size of {MaxMessageSize} bytes");
            }

            messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        }
        while (!result.EndOfMessage);

        return MessageSerializer.Deserialize(messageBuilder.ToString());
    }

    private async Task MonitorHeartbeatsAsync(CancellationToken ct)
    {
        var heartbeatTimeout = TimeSpan.FromSeconds(30);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(10000, ct).ConfigureAwait(false); // Check every 10 seconds

                var now = DateTime.UtcNow;

                // Create a snapshot of client IDs to avoid race conditions
                // when the collection is modified during iteration
                var clientIds = _clients.Keys.ToList();

                foreach (var clientId in clientIds)
                {
                    // Re-fetch client from dictionary to handle concurrent removals
                    if (_clients.TryGetValue(clientId, out var client) &&
                        now - client.LastHeartbeat > heartbeatTimeout)
                    {
                        Console.WriteLine($"Client {client.PlayerName} timed out");
                        await DisconnectClientAsync(clientId, "Heartbeat timeout").ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when server is stopping
                break;
            }
        }
    }

    public async Task BroadcastAsync(INetworkMessage message, string? excludeClientId = null, CancellationToken ct = default)
    {
        var tasks = _clients.Values
            .Where(c => c.Id != excludeClientId && c.State == ConnectionState.Connected)
            .Select(c => c.SendAsync(message, ct));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task BroadcastToLobbyAsync(string lobbyId, INetworkMessage message, string? excludeClientId = null, CancellationToken ct = default)
    {
        var tasks = _clients.Values
            .Where(c => c.CurrentLobbyId == lobbyId && c.Id != excludeClientId && c.State == ConnectionState.Connected)
            .Select(c => c.SendAsync(message, ct));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task SendToClientAsync(string clientId, INetworkMessage message, CancellationToken ct = default)
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            await client.SendAsync(message, ct).ConfigureAwait(false);
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

        _tcpListener.Stop();
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

        _tcpListener.Stop();
    }
}
