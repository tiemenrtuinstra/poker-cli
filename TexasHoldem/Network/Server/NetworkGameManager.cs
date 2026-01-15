using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
using TexasHoldem.Network.Messages;
using TexasHoldem.Players;

namespace TexasHoldem.Network.Server;

/// <summary>
/// Manages a networked poker game on the server side.
/// Orchestrates game flow, syncs state to clients, and processes actions.
/// </summary>
public class NetworkGameManager
{
    private readonly PokerServer _server;
    private readonly Lobby _lobby;
    private readonly ReconnectionManager? _reconnectionManager;
    private readonly Dictionary<string, NetworkPlayer> _networkPlayers = new();
    private readonly Dictionary<string, IPlayer> _aiPlayers = new();
    private readonly List<IPlayer> _allPlayers = new();
    private GameState? _gameState;
    private bool _isRunning;
    private readonly CancellationTokenSource _gameCts = new();

    public event Action<string>? OnGameLog;
    public event Action<GameState>? OnStateChanged;
    public event Action<List<WinnerInfo>>? OnHandComplete;
    public event Action<List<PlayerRanking>>? OnGameOver;

    public NetworkGameManager(PokerServer server, Lobby lobby, ReconnectionManager? reconnectionManager = null)
    {
        _server = server;
        _lobby = lobby;
        _reconnectionManager = reconnectionManager;

        _server.OnMessageReceived += HandleMessage;

        // Subscribe to reconnection events
        if (_reconnectionManager != null)
        {
            _reconnectionManager.OnBotTakeover += HandleBotTakeover;
            _reconnectionManager.OnPlayerReconnected += HandlePlayerReconnected;
        }
    }

    private void HandleBotTakeover(string clientId, string playerName)
    {
        if (_networkPlayers.TryGetValue(clientId, out var player))
        {
            player.EnableBotControl();
            Log($"{playerName} is now controlled by bot (disconnected)");

            // Broadcast to lobby
            Task.Run(async () =>
            {
                await _server.BroadcastToLobbyAsync(_lobby.Code, new PlayerDisconnectedMessage
                {
                    PlayerId = clientId,
                    PlayerName = playerName,
                    BotTakeover = true
                });
            });
        }
    }

    private void HandlePlayerReconnected(string clientId, string playerName)
    {
        if (_networkPlayers.TryGetValue(clientId, out var player))
        {
            player.DisableBotControl();
            Log($"{playerName} has reconnected");

            // Send current game state to reconnected player
            Task.Run(async () =>
            {
                await SendGameStateToClientAsync(clientId, includePrivateCards: true);
            });
        }
    }

    public async Task StartGameAsync()
    {
        if (_isRunning) return;
        _isRunning = true;

        // Create players from lobby
        InitializePlayers();

        // Initialize game state
        _gameState = new GameState
        {
            Players = _allPlayers,
            ActivePlayers = new List<IPlayer>(_allPlayers),
            SmallBlindAmount = _lobby.Settings.SmallBlind,
            BigBlindAmount = _lobby.Settings.BigBlind,
            AnteAmount = _lobby.Settings.Ante,
            DealerPosition = 0
        };

        Log("Game started!");
        _lobby.State = LobbyState.InGame;

        // Broadcast initial state
        await BroadcastGameStateAsync();

        // Game loop
        try
        {
            while (_isRunning && !_gameCts.Token.IsCancellationRequested)
            {
                var remainingPlayers = _allPlayers.Where(p => p.Chips > 0).ToList();

                if (remainingPlayers.Count <= 1)
                {
                    // Game over
                    await EndGameAsync();
                    return;
                }

                await PlayHandAsync();
                _gameState.HandNumber++;

                // Brief pause between hands
                await Task.Delay(2000, _gameCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Log("Game cancelled");
        }
    }

    private void InitializePlayers()
    {
        _allPlayers.Clear();
        _networkPlayers.Clear();
        _aiPlayers.Clear();

        foreach (var lobbyPlayer in _lobby.Players.Values.OrderBy(p => p.IsAi))
        {
            if (lobbyPlayer.IsAi)
            {
                // Create AI player
                var personalities = Enum.GetValues<PersonalityType>();
                var personality = personalities[new Random().Next(personalities.Length)];
                var aiPlayer = new BasicAiPlayer(lobbyPlayer.Name, _lobby.Settings.StartingChips, personality);
                _aiPlayers[lobbyPlayer.Id] = aiPlayer;
                _allPlayers.Add(aiPlayer);
            }
            else
            {
                // Create network player
                var networkPlayer = new NetworkPlayer(
                    lobbyPlayer.Id,
                    lobbyPlayer.Name,
                    _lobby.Settings.StartingChips);
                networkPlayer.Connection = lobbyPlayer.Connection;
                _networkPlayers[lobbyPlayer.Id] = networkPlayer;
                _allPlayers.Add(networkPlayer);
            }
        }
    }

    private async Task PlayHandAsync()
    {
        if (_gameState == null) return;

        var activePlayers = _allPlayers.Where(p => p.Chips > 0 && p.IsActive).ToList();
        if (activePlayers.Count < 2) return;

        // Reset for new hand
        foreach (var player in _allPlayers)
        {
            player.Reset();
        }

        _gameState.ActivePlayers = activePlayers;
        _gameState.Phase = GamePhase.PreFlop;
        _gameState.BettingPhase = BettingPhase.PreFlop;
        _gameState.CommunityCards.Clear();
        _gameState.TotalPot = 0;
        _gameState.CurrentBet = 0;
        _gameState.ActionsThisRound.Clear();
        _gameState.PlayerBets.Clear();
        _gameState.PlayerHasFolded.Clear();
        _gameState.PlayerHasActed.Clear();

        Log($"--- Hand #{_gameState.HandNumber + 1} ---");

        // Rotate dealer
        _gameState.DealerPosition = (_gameState.DealerPosition + 1) % activePlayers.Count;
        SetBlindPositions(activePlayers.Count);

        // Deal hole cards
        var deck = new Deck();
        deck.Shuffle();

        foreach (var player in activePlayers)
        {
            var cards = new List<Card> { deck.DealCard(), deck.DealCard() };
            player.ReceiveCards(cards);

            // Send private cards to network players
            if (player is NetworkPlayer np && np.Connection != null)
            {
                await SendGameStateToClientAsync(np.ClientId, includePrivateCards: true);
            }
        }

        // Post blinds
        await PostBlindsAsync(activePlayers);

        // Betting rounds
        var phases = new[]
        {
            (BettingPhase.PreFlop, 0),
            (BettingPhase.Flop, 3),
            (BettingPhase.Turn, 1),
            (BettingPhase.River, 1)
        };

        foreach (var (phase, cardsToDeal) in phases)
        {
            if (GetActivePlayersInHand().Count <= 1) break;

            _gameState.BettingPhase = phase;

            // Deal community cards
            if (cardsToDeal > 0 && phase != BettingPhase.PreFlop)
            {
                for (int i = 0; i < cardsToDeal; i++)
                {
                    _gameState.CommunityCards.Add(deck.DealCard());
                }

                await BroadcastPhaseChangeAsync(phase);
                Log($"{phase}: {string.Join(" ", _gameState.CommunityCards.Select(c => c.GetDisplayString()))}");
            }

            await PlayBettingRoundAsync();
        }

        // Showdown
        await ShowdownAsync();
    }

    private void SetBlindPositions(int playerCount)
    {
        if (_gameState == null) return;

        if (playerCount == 2)
        {
            // Heads-up: dealer is small blind
            _gameState.SmallBlindPosition = _gameState.DealerPosition;
            _gameState.BigBlindPosition = (_gameState.DealerPosition + 1) % 2;
        }
        else
        {
            _gameState.SmallBlindPosition = (_gameState.DealerPosition + 1) % playerCount;
            _gameState.BigBlindPosition = (_gameState.DealerPosition + 2) % playerCount;
        }
    }

    private async Task PostBlindsAsync(List<IPlayer> players)
    {
        if (_gameState == null) return;

        var sbPlayer = players[_gameState.SmallBlindPosition];
        var bbPlayer = players[_gameState.BigBlindPosition];

        var sbAmount = Math.Min(_gameState.SmallBlindAmount, sbPlayer.Chips);
        var bbAmount = Math.Min(_gameState.BigBlindAmount, bbPlayer.Chips);

        sbPlayer.RemoveChips(sbAmount);
        bbPlayer.RemoveChips(bbAmount);

        _gameState.PlayerBets[sbPlayer.Name] = sbAmount;
        _gameState.PlayerBets[bbPlayer.Name] = bbAmount;
        _gameState.TotalPot = sbAmount + bbAmount;
        _gameState.CurrentBet = bbAmount;

        Log($"{sbPlayer.Name} posts small blind €{sbAmount}");
        Log($"{bbPlayer.Name} posts big blind €{bbAmount}");

        await BroadcastGameStateAsync();
    }

    private async Task PlayBettingRoundAsync()
    {
        if (_gameState == null) return;

        var activePlayers = GetActivePlayersInHand();
        if (activePlayers.Count <= 1) return;

        // Reset acted status
        foreach (var player in _allPlayers)
        {
            _gameState.PlayerHasActed[player.Name] = false;
        }

        // Determine starting position
        int startPos;
        if (_gameState.BettingPhase == BettingPhase.PreFlop)
        {
            // Pre-flop: start after big blind
            startPos = (_gameState.BigBlindPosition + 1) % activePlayers.Count;
        }
        else
        {
            // Post-flop: start after dealer
            startPos = (_gameState.DealerPosition + 1) % activePlayers.Count;
        }

        _gameState.CurrentPlayerPosition = startPos;
        _gameState.ActionsThisRound.Clear();

        bool roundComplete = false;
        int lastRaiser = -1;
        int playersActed = 0;

        while (!roundComplete)
        {
            var player = activePlayers[_gameState.CurrentPlayerPosition % activePlayers.Count];

            if (player.HasFolded || player.IsAllIn)
            {
                _gameState.CurrentPlayerPosition = (_gameState.CurrentPlayerPosition + 1) % activePlayers.Count;
                continue;
            }

            await BroadcastGameStateAsync();

            PlayerAction action;

            if (player is NetworkPlayer networkPlayer)
            {
                action = await GetNetworkPlayerActionAsync(networkPlayer);
            }
            else
            {
                action = player.TakeTurn(_gameState);
            }

            await ProcessActionAsync(player, action);
            playersActed++;

            activePlayers = GetActivePlayersInHand();
            if (activePlayers.Count <= 1)
            {
                roundComplete = true;
                continue;
            }

            // Check if round is complete
            if (action.Action == ActionType.Raise || action.Action == ActionType.Bet)
            {
                lastRaiser = _gameState.CurrentPlayerPosition;
            }

            _gameState.CurrentPlayerPosition = (_gameState.CurrentPlayerPosition + 1) % activePlayers.Count;

            // Round is complete when everyone has acted and all bets are equal
            bool allActed = activePlayers
                .Where(p => !p.HasFolded && !p.IsAllIn)
                .All(p => _gameState.PlayerHasActed.GetValueOrDefault(p.Name, false));

            bool allBetsEqual = activePlayers
                .Where(p => !p.HasFolded && !p.IsAllIn)
                .Select(p => _gameState.PlayerBets.GetValueOrDefault(p.Name, 0))
                .Distinct()
                .Count() <= 1;

            if (allActed && allBetsEqual && playersActed >= activePlayers.Count(p => !p.HasFolded && !p.IsAllIn))
            {
                roundComplete = true;
            }
        }

        // Reset current bet for next round
        _gameState.CurrentBet = 0;
        foreach (var player in _allPlayers)
        {
            _gameState.PlayerBets[player.Name] = 0;
        }
    }

    private async Task<PlayerAction> GetNetworkPlayerActionAsync(NetworkPlayer player)
    {
        if (_gameState == null || player.Connection == null)
        {
            return new PlayerAction
            {
                PlayerId = player.Name,
                Action = ActionType.Fold,
                Amount = 0,
                Timestamp = DateTime.Now,
                BettingPhase = _gameState?.BettingPhase ?? BettingPhase.PreFlop
            };
        }

        // Send action request to client
        var request = player.CreateActionRequest(_gameState);
        await player.Connection.SendAsync(request);

        // Wait for action (blocking with timeout handled in NetworkPlayer)
        return player.TakeTurn(_gameState);
    }

    /// <summary>
    /// Validates that the given action is legal in the current game state.
    /// Returns a corrected action if the original was invalid.
    /// </summary>
    private PlayerAction ValidateAction(IPlayer player, PlayerAction action)
    {
        if (_gameState == null)
        {
            return new PlayerAction
            {
                PlayerId = player.Name,
                Action = ActionType.Fold,
                Amount = 0,
                Timestamp = DateTime.Now,
                BettingPhase = BettingPhase.PreFlop
            };
        }

        var currentBet = _gameState.CurrentBet;
        var playerBet = _gameState.PlayerBets.GetValueOrDefault(player.Name, 0);
        var toCall = currentBet - playerBet;
        var minRaise = _gameState.BigBlindAmount;

        switch (action.Action)
        {
            case ActionType.Check:
                // Can only check if there's nothing to call
                if (toCall > 0)
                {
                    Log($"[Validation] {player.Name} tried to check but must call €{toCall} - forcing fold");
                    return new PlayerAction
                    {
                        PlayerId = player.Name,
                        Action = ActionType.Fold,
                        Amount = 0,
                        Timestamp = DateTime.Now,
                        BettingPhase = _gameState.BettingPhase
                    };
                }
                break;

            case ActionType.Call:
                // Must have something to call
                if (toCall <= 0)
                {
                    Log($"[Validation] {player.Name} tried to call but nothing to call - converting to check");
                    return new PlayerAction
                    {
                        PlayerId = player.Name,
                        Action = ActionType.Check,
                        Amount = 0,
                        Timestamp = DateTime.Now,
                        BettingPhase = _gameState.BettingPhase
                    };
                }
                break;

            case ActionType.Bet:
                // Can only bet when no current bet
                if (currentBet > 0)
                {
                    Log($"[Validation] {player.Name} tried to bet but there's already a bet - converting to raise");
                    return new PlayerAction
                    {
                        PlayerId = player.Name,
                        Action = ActionType.Raise,
                        Amount = Math.Max(action.Amount, toCall + minRaise),
                        Timestamp = DateTime.Now,
                        BettingPhase = _gameState.BettingPhase
                    };
                }
                // Validate minimum bet
                if (action.Amount < _gameState.BigBlindAmount && action.Amount < player.Chips)
                {
                    Log($"[Validation] {player.Name} bet €{action.Amount} below minimum - adjusting to €{_gameState.BigBlindAmount}");
                    return new PlayerAction
                    {
                        PlayerId = player.Name,
                        Action = ActionType.Bet,
                        Amount = Math.Min(_gameState.BigBlindAmount, player.Chips),
                        Timestamp = DateTime.Now,
                        BettingPhase = _gameState.BettingPhase
                    };
                }
                // Can't bet more than you have
                if (action.Amount > player.Chips)
                {
                    Log($"[Validation] {player.Name} bet €{action.Amount} but only has €{player.Chips} - converting to all-in");
                    return new PlayerAction
                    {
                        PlayerId = player.Name,
                        Action = ActionType.AllIn,
                        Amount = player.Chips,
                        Timestamp = DateTime.Now,
                        BettingPhase = _gameState.BettingPhase
                    };
                }
                break;

            case ActionType.Raise:
                // Can only raise when there's a current bet
                if (currentBet <= 0)
                {
                    Log($"[Validation] {player.Name} tried to raise but no bet to raise - converting to bet");
                    return new PlayerAction
                    {
                        PlayerId = player.Name,
                        Action = ActionType.Bet,
                        Amount = Math.Max(action.Amount, _gameState.BigBlindAmount),
                        Timestamp = DateTime.Now,
                        BettingPhase = _gameState.BettingPhase
                    };
                }
                // Minimum raise is call + minRaise
                var minRaiseTotal = toCall + minRaise;
                if (action.Amount < minRaiseTotal && action.Amount < player.Chips)
                {
                    Log($"[Validation] {player.Name} raise €{action.Amount} below minimum €{minRaiseTotal} - adjusting");
                    return new PlayerAction
                    {
                        PlayerId = player.Name,
                        Action = ActionType.Raise,
                        Amount = Math.Min(minRaiseTotal, player.Chips),
                        Timestamp = DateTime.Now,
                        BettingPhase = _gameState.BettingPhase
                    };
                }
                // Can't raise more than you have
                if (action.Amount > player.Chips)
                {
                    Log($"[Validation] {player.Name} raise €{action.Amount} but only has €{player.Chips} - converting to all-in");
                    return new PlayerAction
                    {
                        PlayerId = player.Name,
                        Action = ActionType.AllIn,
                        Amount = player.Chips,
                        Timestamp = DateTime.Now,
                        BettingPhase = _gameState.BettingPhase
                    };
                }
                break;

            case ActionType.AllIn:
                // All-in is always valid if you have chips
                if (player.Chips <= 0)
                {
                    Log($"[Validation] {player.Name} tried to go all-in but has no chips - forcing fold");
                    return new PlayerAction
                    {
                        PlayerId = player.Name,
                        Action = ActionType.Fold,
                        Amount = 0,
                        Timestamp = DateTime.Now,
                        BettingPhase = _gameState.BettingPhase
                    };
                }
                break;
        }

        // Action is valid
        return action;
    }

    private async Task ProcessActionAsync(IPlayer player, PlayerAction action)
    {
        if (_gameState == null) return;

        // Validate the action before processing
        action = ValidateAction(player, action);

        _gameState.PlayerHasActed[player.Name] = true;
        _gameState.ActionsThisRound.Add(action);

        switch (action.Action)
        {
            case ActionType.Fold:
                player.HasFolded = true;
                _gameState.PlayerHasFolded[player.Name] = true;
                Log($"{player.Name} folds");
                break;

            case ActionType.Check:
                Log($"{player.Name} checks");
                break;

            case ActionType.Call:
                var callAmount = _gameState.CurrentBet - _gameState.PlayerBets.GetValueOrDefault(player.Name, 0);
                callAmount = Math.Min(callAmount, player.Chips);
                player.RemoveChips(callAmount);
                _gameState.PlayerBets[player.Name] = _gameState.PlayerBets.GetValueOrDefault(player.Name, 0) + callAmount;
                _gameState.TotalPot += callAmount;
                if (player.Chips == 0) player.IsAllIn = true;
                Log($"{player.Name} calls €{callAmount}");
                break;

            case ActionType.Bet:
            case ActionType.Raise:
                var betAmount = action.Amount;
                player.RemoveChips(betAmount);
                _gameState.PlayerBets[player.Name] = _gameState.PlayerBets.GetValueOrDefault(player.Name, 0) + betAmount;
                _gameState.CurrentBet = _gameState.PlayerBets[player.Name];
                _gameState.TotalPot += betAmount;
                if (player.Chips == 0) player.IsAllIn = true;
                Log($"{player.Name} {(action.Action == ActionType.Raise ? "raises" : "bets")} €{betAmount}");
                break;

            case ActionType.AllIn:
                var allInAmount = player.Chips;
                player.RemoveChips(allInAmount);
                _gameState.PlayerBets[player.Name] = _gameState.PlayerBets.GetValueOrDefault(player.Name, 0) + allInAmount;
                if (_gameState.PlayerBets[player.Name] > _gameState.CurrentBet)
                {
                    _gameState.CurrentBet = _gameState.PlayerBets[player.Name];
                }
                _gameState.TotalPot += allInAmount;
                player.IsAllIn = true;
                Log($"{player.Name} goes all-in for €{allInAmount}");
                break;
        }

        await BroadcastGameStateAsync();
    }

    private async Task ShowdownAsync()
    {
        if (_gameState == null) return;

        var activePlayers = GetActivePlayersInHand();

        if (activePlayers.Count == 1)
        {
            // Everyone else folded
            var winner = activePlayers[0];
            winner.AddChips(_gameState.TotalPot);
            Log($"{winner.Name} wins €{_gameState.TotalPot} (everyone else folded)");

            await BroadcastHandCompleteAsync(new List<WinnerInfo>
            {
                new WinnerInfo
                {
                    PlayerId = winner is NetworkPlayer np ? np.ClientId : winner.Name,
                    PlayerName = winner.Name,
                    Amount = _gameState.TotalPot,
                    HandDescription = "Everyone else folded"
                }
            });
            return;
        }

        // Evaluate hands
        var results = new List<(IPlayer Player, HandResult Hand)>();
        foreach (var player in activePlayers)
        {
            var allCards = new List<Card>(player.HoleCards);
            allCards.AddRange(_gameState.CommunityCards);
            var handResult = HandEvaluator.EvaluateHand(allCards);
            results.Add((player, handResult));
        }

        // Sort by hand strength (using Score for more precise ordering)
        results = results.OrderByDescending(r => r.Hand.Strength)
                        .ThenByDescending(r => r.Hand.Score)
                        .ToList();

        // Award pot to winner(s)
        var winningStrength = results[0].Hand.Strength;
        var winners = results.Where(r => r.Hand.Strength == winningStrength).ToList();
        var potShare = _gameState.TotalPot / winners.Count;

        var winnerInfos = new List<WinnerInfo>();
        foreach (var (player, hand) in winners)
        {
            player.AddChips(potShare);
            Log($"{player.Name} wins €{potShare} with {hand.Description}");

            winnerInfos.Add(new WinnerInfo
            {
                PlayerId = player is NetworkPlayer np ? np.ClientId : player.Name,
                PlayerName = player.Name,
                Amount = potShare,
                HandDescription = hand.Description,
                WinningCards = hand.Cards.Select(c => c.GetDisplayString()).ToList()
            });
        }

        await BroadcastHandCompleteAsync(winnerInfos);
    }

    private async Task EndGameAsync()
    {
        if (_gameState == null) return;

        _isRunning = false;
        _lobby.State = LobbyState.Finished;

        var rankings = _allPlayers
            .OrderByDescending(p => p.Chips)
            .Select((p, i) => new PlayerRanking
            {
                Rank = i + 1,
                PlayerId = p is NetworkPlayer np ? np.ClientId : p.Name,
                PlayerName = p.Name,
                FinalChips = p.Chips
            })
            .ToList();

        Log("Game Over!");
        foreach (var ranking in rankings)
        {
            Log($"#{ranking.Rank} {ranking.PlayerName} - €{ranking.FinalChips}");
        }

        await _server.BroadcastToLobbyAsync(_lobby.Code, new GameOverMessage { Rankings = rankings });
        OnGameOver?.Invoke(rankings);
    }

    private List<IPlayer> GetActivePlayersInHand()
    {
        return _allPlayers.Where(p => !p.HasFolded && p.IsActive).ToList();
    }

    private void HandleMessage(string clientId, INetworkMessage message)
    {
        if (message is ActionResponseMessage actionResponse)
        {
            if (_networkPlayers.TryGetValue(clientId, out var player))
            {
                player.ReceiveAction(
                    actionResponse.Action,
                    actionResponse.Amount,
                    _gameState?.BettingPhase ?? BettingPhase.PreFlop);
            }
        }
    }

    private async Task BroadcastGameStateAsync()
    {
        if (_gameState == null) return;

        foreach (var (clientId, player) in _networkPlayers)
        {
            await SendGameStateToClientAsync(clientId, includePrivateCards: true);
        }

        OnStateChanged?.Invoke(_gameState);
    }

    private async Task SendGameStateToClientAsync(string clientId, bool includePrivateCards = false)
    {
        if (_gameState == null) return;
        if (!_networkPlayers.TryGetValue(clientId, out var player)) return;
        if (player.Connection == null) return;

        var networkState = ToNetworkGameState(clientId, includePrivateCards);

        await player.Connection.SendAsync(new GameStateSyncMessage { State = networkState });
    }

    private NetworkGameState ToNetworkGameState(string forClientId, bool includePrivateCards)
    {
        if (_gameState == null) return new NetworkGameState();

        var state = new NetworkGameState
        {
            HandNumber = _gameState.HandNumber,
            Phase = _gameState.Phase.ToString(),
            BettingPhase = _gameState.BettingPhase.ToString(),
            DealerPosition = _gameState.DealerPosition,
            SmallBlindPosition = _gameState.SmallBlindPosition,
            BigBlindPosition = _gameState.BigBlindPosition,
            CurrentPlayerIndex = _gameState.CurrentPlayerPosition,
            CommunityCards = _gameState.CommunityCards.Select(c => c.GetDisplayString()).ToList(),
            TotalPot = _gameState.TotalPot,
            CurrentBet = _gameState.CurrentBet,
            SmallBlindAmount = _gameState.SmallBlindAmount,
            BigBlindAmount = _gameState.BigBlindAmount,
            AnteAmount = _gameState.AnteAmount,
            MyPlayerId = forClientId
        };

        // Add player info
        foreach (var player in _allPlayers)
        {
            var playerId = player is NetworkPlayer np ? np.ClientId : player.Name;
            var isMe = playerId == forClientId;

            var playerInfo = new NetworkPlayerInfo
            {
                Id = playerId,
                Name = player.Name,
                Chips = player.Chips,
                CurrentBet = _gameState.PlayerBets.GetValueOrDefault(player.Name, 0),
                IsActive = player.IsActive,
                IsAllIn = player.IsAllIn,
                HasFolded = player.HasFolded,
                IsConnected = player is not NetworkPlayer || ((NetworkPlayer)player).IsConnected,
                IsCurrentPlayer = _allPlayers.IndexOf(player) == _gameState.CurrentPlayerPosition,
                HoleCards = (isMe && includePrivateCards) || _gameState.Phase == GamePhase.Showdown
                    ? player.HoleCards.Select(c => c.GetDisplayString()).ToList()
                    : null
            };

            state.Players.Add(playerInfo);
        }

        // Add player's own hole cards
        if (_networkPlayers.TryGetValue(forClientId, out var myPlayer) && includePrivateCards)
        {
            state.MyHoleCards = myPlayer.HoleCards.Select(c => c.GetDisplayString()).ToList();
        }

        return state;
    }

    private async Task BroadcastPhaseChangeAsync(BettingPhase phase)
    {
        var message = new PhaseChangeMessage
        {
            Phase = phase.ToString(),
            NewCommunityCards = _gameState?.CommunityCards.Select(c => c.GetDisplayString()).ToList()
        };

        await _server.BroadcastToLobbyAsync(_lobby.Code, message);
    }

    private async Task BroadcastHandCompleteAsync(List<WinnerInfo> winners)
    {
        var message = new HandCompleteMessage
        {
            Winners = winners,
            Summary = string.Join(", ", winners.Select(w => $"{w.PlayerName} wins €{w.Amount}"))
        };

        await _server.BroadcastToLobbyAsync(_lobby.Code, message);
        OnHandComplete?.Invoke(winners);
    }

    private void Log(string message)
    {
        OnGameLog?.Invoke(message);
        Console.WriteLine($"[Game] {message}");
    }

    public void Stop()
    {
        _isRunning = false;
        _gameCts.Cancel();
    }
}
