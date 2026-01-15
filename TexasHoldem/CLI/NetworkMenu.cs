using Spectre.Console;
using TexasHoldem.Network.Client;
using TexasHoldem.Network.Messages;
using TexasHoldem.Network.Server;

namespace TexasHoldem.CLI;

public class NetworkMenu
{
    private readonly InputHelper _inputHelper;
    private PokerServer? _server;
    private LobbyManager? _lobbyManager;
    private ReconnectionManager? _reconnectionManager;
    private PokerClient? _client;
    private bool _isHost;

    public NetworkMenu()
    {
        _inputHelper = new InputHelper();
    }

    public async Task<NetworkGameResult?> ShowMultiplayerMenuAsync()
    {
        AnsiConsole.Clear();
        ShowMultiplayerHeader();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]What would you like to do?[/]")
                .PageSize(8)
                .HighlightStyle(new Style(Color.Black, Color.Green))
                .AddChoices(new[]
                {
                    "🖥️   Host Game",
                    "🔗  Join Game",
                    "🔍  Browse Public Games",
                    "🔙  Back to Main Menu"
                }));

        return choice switch
        {
            "🖥️   Host Game" => await HostGameAsync(),
            "🔗  Join Game" => await JoinGameAsync(),
            "🔍  Browse Public Games" => await BrowseGamesAsync(),
            _ => null
        };
    }

    private void ShowMultiplayerHeader()
    {
        var headerArt = string.Join("\n",
            "[cyan]╔═══════════════════════════════════════════════════════════════════════╗[/]",
            "[cyan]║[/]  [bold yellow]🌐 MULTIPLAYER[/]                                                      [cyan]║[/]",
            "[cyan]║[/]  [dim]Host or join a game on your local network[/]                          [cyan]║[/]",
            "[cyan]╚═══════════════════════════════════════════════════════════════════════╝[/]"
        );

        AnsiConsole.Write(new Markup(headerArt));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }

    private async Task<NetworkGameResult?> HostGameAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold green]🖥️ HOST GAME[/]").RuleStyle("green"));
        AnsiConsole.WriteLine();

        // Get player name
        var playerName = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Enter your name:[/]")
                .DefaultValue("Host"));

        // Configure lobby settings
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold cyan]Lobby Settings[/]").RuleStyle("cyan").LeftJustified());

        var lobbyName = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Lobby name:[/]")
                .DefaultValue($"{playerName}'s Game"));

        var maxPlayers = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Max players[/] [dim](2-8)[/]:")
                .DefaultValue(6)
                .Validate(n => n >= 2 && n <= 8 ? ValidationResult.Success() : ValidationResult.Error("[red]Must be 2-8[/]")));

        var isPublic = AnsiConsole.Confirm("[yellow]Make lobby public?[/]", true);

        string? password = null;
        if (!isPublic)
        {
            password = AnsiConsole.Prompt(
                new TextPrompt<string>("[yellow]Lobby password:[/]")
                    .Secret());
        }

        var startingChips = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Starting chips:[/]")
                .DefaultValue(10000));

        var smallBlind = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Small blind:[/]")
                .DefaultValue(50));

        var bigBlind = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Big blind:[/]")
                .DefaultValue(100));

        // Get port
        var port = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Server port:[/]")
                .DefaultValue(7777)
                .Validate(n => n >= 1024 && n <= 65535 ? ValidationResult.Success() : ValidationResult.Error("[red]Invalid port[/]")));

        // Start server
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Starting server...[/]");

        try
        {
            _server = new PokerServer(port);
            _reconnectionManager = new ReconnectionManager(_server);
            _lobbyManager = new LobbyManager(_server, _reconnectionManager);

            // Start server in background and wait for it to be ready
            var serverTask = _server.StartAsync();

            // Wait for server to start (up to 2 seconds)
            var startTime = DateTime.UtcNow;
            while (!_server.IsRunning && _server.StartupError == null && (DateTime.UtcNow - startTime).TotalSeconds < 2)
            {
                await Task.Delay(100);
            }

            // Check if server started successfully
            if (!_server.IsRunning)
            {
                var error = _server.StartupError ?? "Unknown error";
                AnsiConsole.MarkupLine($"[red]Failed to start server: {error}[/]");
                _inputHelper.PressAnyKeyToContinue();
                await CleanupAsync();
                return null;
            }

            // Connect as host
            _client = new PokerClient(playerName);
            _isHost = true;

            var connected = await _client.ConnectAsync("localhost", port);
            if (!connected)
            {
                AnsiConsole.MarkupLine("[red]Failed to connect to server![/]");
                _inputHelper.PressAnyKeyToContinue();
                await CleanupAsync();
                return null;
            }

            // Create lobby
            var settings = new LobbySettings
            {
                Name = lobbyName,
                MaxPlayers = maxPlayers,
                IsPublic = isPublic,
                Password = password,
                StartingChips = startingChips,
                SmallBlind = smallBlind,
                BigBlind = bigBlind
            };

            var createResponse = await _client.CreateLobbyAsync(settings);
            if (createResponse == null || !createResponse.Success)
            {
                AnsiConsole.MarkupLine($"[red]Failed to create lobby: {createResponse?.Error ?? "Unknown error"}[/]");
                _inputHelper.PressAnyKeyToContinue();
                await CleanupAsync();
                return null;
            }

            AnsiConsole.MarkupLine($"[green]✓[/] Server started on port [bold]{port}[/]");
            AnsiConsole.MarkupLine($"[green]✓[/] Lobby created with code: [bold yellow]{createResponse.LobbyCode}[/]");

            // Show IP address info
            ShowLocalIpInfo();

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Share the lobby code with friends to let them join![/]");

            return await ShowLobbyAsync(createResponse.LobbyCode!);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error starting server: {ex.Message}[/]");
            _inputHelper.PressAnyKeyToContinue();
            await CleanupAsync();
            return null;
        }
    }

    private async Task<NetworkGameResult?> JoinGameAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]🔗 JOIN GAME[/]").RuleStyle("cyan"));
        AnsiConsole.WriteLine();

        // Get player name
        var playerName = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Enter your name:[/]")
                .DefaultValue("Player"));

        // Get server address
        var host = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Server IP address:[/]")
                .DefaultValue("localhost"));

        var port = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Server port:[/]")
                .DefaultValue(7777));

        // Get lobby code
        var lobbyCode = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Lobby code:[/]"));

        // Optional password
        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Password[/] [dim](leave empty if none)[/]:")
                .AllowEmpty());

        if (string.IsNullOrEmpty(password))
            password = null;

        // Connect to server
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Connecting to server...[/]");

        try
        {
            _client = new PokerClient(playerName);
            _isHost = false;

            var connected = await _client.ConnectAsync(host, port);
            if (!connected)
            {
                AnsiConsole.MarkupLine("[red]Failed to connect to server![/]");
                _inputHelper.PressAnyKeyToContinue();
                return await ShowMultiplayerMenuAsync();
            }

            AnsiConsole.MarkupLine("[green]✓[/] Connected to server");

            // Join lobby
            var joinResponse = await _client.JoinLobbyAsync(lobbyCode, password);
            if (joinResponse == null || !joinResponse.Success)
            {
                AnsiConsole.MarkupLine($"[red]Failed to join lobby: {joinResponse?.Error ?? "Unknown error"}[/]");
                _inputHelper.PressAnyKeyToContinue();
                await CleanupAsync();
                return await ShowMultiplayerMenuAsync();
            }

            AnsiConsole.MarkupLine($"[green]✓[/] Joined lobby: [bold]{joinResponse.Lobby?.Name}[/]");

            return await ShowLobbyAsync(lobbyCode);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            _inputHelper.PressAnyKeyToContinue();
            await CleanupAsync();
            return await ShowMultiplayerMenuAsync();
        }
    }

    private async Task<NetworkGameResult?> BrowseGamesAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]🔍 BROWSE PUBLIC GAMES[/]").RuleStyle("cyan"));
        AnsiConsole.WriteLine();

        // Get player name first
        var playerName = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Enter your name:[/]")
                .DefaultValue("Player"));

        // Get server address to browse
        var host = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Server IP address:[/]")
                .DefaultValue("localhost"));

        var port = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Server port:[/]")
                .DefaultValue(7777));

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Connecting and fetching lobbies...[/]");

        try
        {
            _client = new PokerClient(playerName);

            var connected = await _client.ConnectAsync(host, port);
            if (!connected)
            {
                AnsiConsole.MarkupLine("[red]Failed to connect to server![/]");
                _inputHelper.PressAnyKeyToContinue();
                return await ShowMultiplayerMenuAsync();
            }

            var lobbiesResponse = await _client.ListLobbiesAsync();
            if (lobbiesResponse == null || !lobbiesResponse.Lobbies.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No public lobbies available.[/]");
                _inputHelper.PressAnyKeyToContinue();
                await CleanupAsync();
                return await ShowMultiplayerMenuAsync();
            }

            // Display lobbies in a table
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .AddColumn(new TableColumn("[bold]Code[/]").Centered())
                .AddColumn(new TableColumn("[bold]Name[/]"))
                .AddColumn(new TableColumn("[bold]Host[/]"))
                .AddColumn(new TableColumn("[bold]Players[/]").Centered())
                .AddColumn(new TableColumn("[bold]Blinds[/]").Centered());

            foreach (var lobby in lobbiesResponse.Lobbies)
            {
                table.AddRow(
                    $"[bold yellow]{lobby.LobbyCode}[/]",
                    lobby.Name,
                    lobby.HostName,
                    $"{lobby.Players.Count}/{lobby.Settings.MaxPlayers}",
                    $"€{lobby.Settings.SmallBlind}/€{lobby.Settings.BigBlind}"
                );
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Let user select a lobby
            var choices = lobbiesResponse.Lobbies
                .Select(l => $"{l.LobbyCode} - {l.Name} ({l.Players.Count}/{l.Settings.MaxPlayers})")
                .Concat(new[] { "🔙 Back" })
                .ToArray();

            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold green]Select a lobby to join:[/]")
                    .AddChoices(choices));

            if (selection == "🔙 Back")
            {
                await CleanupAsync();
                return await ShowMultiplayerMenuAsync();
            }

            var selectedCode = selection.Split(" - ")[0];
            var joinResponse = await _client.JoinLobbyAsync(selectedCode);

            if (joinResponse == null || !joinResponse.Success)
            {
                AnsiConsole.MarkupLine($"[red]Failed to join: {joinResponse?.Error ?? "Unknown error"}[/]");
                _inputHelper.PressAnyKeyToContinue();
                await CleanupAsync();
                return await ShowMultiplayerMenuAsync();
            }

            _isHost = false;
            return await ShowLobbyAsync(selectedCode);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            _inputHelper.PressAnyKeyToContinue();
            await CleanupAsync();
            return await ShowMultiplayerMenuAsync();
        }
    }

    private async Task<NetworkGameResult?> ShowLobbyAsync(string lobbyCode)
    {
        if (_client == null) return null;

        LobbyInfo? currentLobby = null;
        var exitLobby = false;
        var gameStarted = false;
        var statusMessages = new List<string>();

        // Subscribe to lobby updates
        _client.OnMessageReceived += msg =>
        {
            switch (msg)
            {
                case LobbyUpdateMessage update:
                    currentLobby = update.Lobby;
                    if (currentLobby.State == LobbyState.Starting || currentLobby.State == LobbyState.InGame)
                    {
                        gameStarted = true;
                    }
                    break;

                case PlayerDisconnectedMessage disconnected:
                    var botStatus = disconnected.BotTakeover ? " (Bot taking over)" : "";
                    statusMessages.Add($"[yellow]{disconnected.PlayerName} disconnected{botStatus}[/]");
                    break;

                case PlayerReconnectedMessage reconnected:
                    statusMessages.Add($"[green]{reconnected.PlayerName} reconnected![/]");
                    break;
            }
        };

        while (!exitLobby && !gameStarted)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold green]🎮 LOBBY: {currentLobby?.Name ?? lobbyCode}[/]").RuleStyle("green"));
            AnsiConsole.WriteLine();

            // Show lobby code prominently
            var codePanel = new Panel(
                new Markup($"[bold yellow]{lobbyCode}[/]"))
                .Header("[bold]Lobby Code[/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Yellow)
                .Expand();

            AnsiConsole.Write(codePanel);
            AnsiConsole.WriteLine();

            // Show players
            if (currentLobby != null)
            {
                var playersTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Green)
                    .AddColumn(new TableColumn("[bold]Player[/]"))
                    .AddColumn(new TableColumn("[bold]Status[/]").Centered())
                    .AddColumn(new TableColumn("[bold]Connection[/]").Centered())
                    .AddColumn(new TableColumn("[bold]Role[/]").Centered());

                foreach (var player in currentLobby.Players)
                {
                    var statusIcon = player.IsReady ? "[green]✓ Ready[/]" : "[yellow]○ Waiting[/]";
                    var connectionStatus = player.IsAi ? "[dim]-[/]" :
                        (player.IsBotControlled ? "[red]🤖 Bot[/]" :
                         player.IsConnected ? "[green]● Online[/]" : "[red]● Offline[/]");
                    var role = player.IsHost ? "[cyan]👑 Host[/]" : (player.IsAi ? "[magenta]🤖 AI[/]" : "");
                    playersTable.AddRow(player.Name, statusIcon, connectionStatus, role);
                }

                AnsiConsole.Write(playersTable);
                AnsiConsole.WriteLine();

                // Show settings
                AnsiConsole.MarkupLine($"[dim]Blinds: €{currentLobby.Settings.SmallBlind}/€{currentLobby.Settings.BigBlind} • Starting Chips: €{currentLobby.Settings.StartingChips:N0}[/]");
                AnsiConsole.WriteLine();

                // Show status messages (last 3)
                if (statusMessages.Any())
                {
                    foreach (var message in statusMessages.TakeLast(3))
                    {
                        AnsiConsole.MarkupLine(message);
                    }
                    AnsiConsole.WriteLine();
                }
            }

            // Show options
            var options = new List<string>();

            // Check if this player is ready
            var myPlayer = currentLobby?.Players.FirstOrDefault(p => p.Id == _client.ClientId);
            if (myPlayer != null && !myPlayer.IsReady)
            {
                options.Add("✓ Ready Up");
            }
            else if (myPlayer != null && myPlayer.IsReady)
            {
                options.Add("✗ Not Ready");
            }

            if (_isHost)
            {
                options.Add("🤖 Add AI Player");
                if (currentLobby?.Players.Any(p => p.IsAi) == true)
                {
                    options.Add("🗑️  Remove AI Player");
                }
                if (currentLobby?.Players.Count >= 2 && currentLobby.Players.All(p => p.IsReady || p.IsAi))
                {
                    options.Add("[green]🎲 Start Game[/]");
                }
            }

            options.Add("💬 Chat");
            options.Add("🚪 Leave Lobby");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]What would you like to do?[/]")
                    .AddChoices(options));

            switch (choice)
            {
                case "✓ Ready Up":
                    await _client.SetReadyAsync(true);
                    break;

                case "✗ Not Ready":
                    await _client.SetReadyAsync(false);
                    break;

                case "🤖 Add AI Player":
                    if (_lobbyManager != null && currentLobby != null)
                    {
                        var aiNames = new[] { "Bot Alice", "Bot Bob", "Bot Charlie", "Bot Diana", "Bot Eve", "Bot Frank" };
                        var usedNames = currentLobby.Players.Where(p => p.IsAi).Select(p => p.Name).ToHashSet();
                        var availableName = aiNames.FirstOrDefault(n => !usedNames.Contains(n)) ?? $"Bot {Guid.NewGuid().ToString("N")[..4]}";
                        var aiId = $"ai_{Guid.NewGuid().ToString("N")[..8]}";

                        _lobbyManager.AddAiPlayerToLobby(lobbyCode, availableName, aiId);
                    }
                    break;

                case "🗑️  Remove AI Player":
                    if (_lobbyManager != null && currentLobby != null)
                    {
                        var aiPlayer = currentLobby.Players.LastOrDefault(p => p.IsAi);
                        if (aiPlayer != null)
                        {
                            _lobbyManager.RemoveAiPlayerFromLobby(lobbyCode, aiPlayer.Id);
                        }
                    }
                    break;

                case "[green]🎲 Start Game[/]":
                    await _client.RequestStartGameAsync();
                    break;

                case "💬 Chat":
                    await ShowChatAsync();
                    break;

                case "🚪 Leave Lobby":
                    await _client.LeaveLobbyAsync();
                    exitLobby = true;
                    break;
            }

            // Brief delay to receive updates
            await Task.Delay(200);
        }

        if (gameStarted && currentLobby != null)
        {
            return new NetworkGameResult
            {
                IsHost = _isHost,
                Server = _server,
                Client = _client,
                LobbyManager = _lobbyManager,
                Lobby = currentLobby
            };
        }

        await CleanupAsync();
        return null;
    }

    private async Task ShowChatAsync()
    {
        // Basic chat implementation - will be expanded in Phase 4
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]💬 CHAT[/]").RuleStyle("cyan"));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Chat feature coming soon![/]");
        AnsiConsole.MarkupLine("[dim]Press any key to return to lobby...[/]");
        Console.ReadKey(true);
    }

    private void ShowLocalIpInfo()
    {
        try
        {
            var hostName = System.Net.Dns.GetHostName();
            var addresses = System.Net.Dns.GetHostAddresses(hostName)
                .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(a => a.ToString())
                .ToList();

            if (addresses.Any())
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Your IP addresses:[/]");
                foreach (var addr in addresses)
                {
                    AnsiConsole.MarkupLine($"  [cyan]•[/] {addr}");
                }
            }
        }
        catch
        {
            // Ignore IP detection errors
        }
    }

    private async Task CleanupAsync()
    {
        if (_client != null)
        {
            await _client.DisconnectAsync();
            _client.Dispose();
            _client = null;
        }

        _reconnectionManager?.Dispose();
        _reconnectionManager = null;

        if (_server != null)
        {
            await _server.StopAsync();
            _server.Dispose();
            _server = null;
        }

        _lobbyManager = null;
    }
}

public class NetworkGameResult
{
    public bool IsHost { get; init; }
    public PokerServer? Server { get; init; }
    public PokerClient? Client { get; init; }
    public LobbyManager? LobbyManager { get; init; }
    public LobbyInfo? Lobby { get; init; }
}
