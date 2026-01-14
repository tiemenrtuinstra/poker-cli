using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
using TexasHoldem.Players;

namespace TexasHoldem.CLI;

public class GameUI
{
    private readonly bool _useColors;
    private readonly bool _enableAsciiArt;

    public GameUI(bool useColors = true, bool enableAsciiArt = true)
    {
        _useColors = useColors;
        _enableAsciiArt = enableAsciiArt;
    }

    public void DisplayPokerTable(GameState gameState)
    {
        if (!_enableAsciiArt)
        {
            DisplaySimpleTable(gameState);
            return;
        }

        Console.WriteLine();
        Console.WriteLine("    ╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("    ║                     POKER TABLE                            ║");
        Console.WriteLine("    ╠════════════════════════════════════════════════════════════╣");
        
        // Display community cards
        var communityCardsDisplay = gameState.CommunityCards.Any() 
            ? string.Join(" ", gameState.CommunityCards.Select(FormatCard))
            : "[ Waiting for flop... ]";
        
        Console.WriteLine($"    ║  Community Cards: {communityCardsDisplay.PadRight(36)} ║");
        Console.WriteLine($"    ║  Pot: €{gameState.TotalPot.ToString().PadRight(49)} ║");
        Console.WriteLine("    ╠════════════════════════════════════════════════════════════╣");

        // Display players around the table
        DisplayPlayersAroundTable(gameState);

        Console.WriteLine("    ╚════════════════════════════════════════════════════════════╝");
    }

    private void DisplayPlayersAroundTable(GameState gameState)
    {
        for (int i = 0; i < gameState.Players.Count; i++)
        {
            var player = gameState.Players[i];
            var position = GetPositionInfo(i, gameState);
            var status = GetPlayerStatus(player, gameState);
            
            var playerLine = $"    ║ {position} {player.Name,-15} €{player.Chips,8} {status,-15} ║";
            Console.WriteLine(playerLine);
        }
    }

    private void DisplaySimpleTable(GameState gameState)
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("POKER TABLE");
        Console.WriteLine(new string('=', 60));
        
        if (gameState.CommunityCards.Any())
        {
            Console.WriteLine($"Community Cards: {string.Join(" ", gameState.CommunityCards.Select(FormatCard))}");
        }
        
        Console.WriteLine($"Pot: €{gameState.TotalPot}");
        Console.WriteLine();
        
        Console.WriteLine("PLAYERS:");
        for (int i = 0; i < gameState.Players.Count; i++)
        {
            var player = gameState.Players[i];
            var position = GetPositionInfo(i, gameState);
            var status = GetPlayerStatus(player, gameState);
            
            Console.WriteLine($"{position} {player.Name} - €{player.Chips} {status}");
        }
        Console.WriteLine(new string('=', 60));
    }

    private string GetPositionInfo(int playerIndex, GameState gameState)
    {
        var indicators = new List<string>();
        
        if (playerIndex == gameState.DealerPosition)
            indicators.Add("(D)");
        if (playerIndex == gameState.SmallBlindPosition)
            indicators.Add("(SB)");
        if (playerIndex == gameState.BigBlindPosition)
            indicators.Add("(BB)");
        if (playerIndex == gameState.CurrentPlayerPosition)
            indicators.Add("*");

        var positionStr = $"Seat {playerIndex + 1}";
        if (indicators.Any())
        {
            positionStr += " " + string.Join("", indicators);
        }

        return positionStr.PadRight(12);
    }

    private string GetPlayerStatus(IPlayer player, GameState gameState)
    {
        if (!player.IsActive)
            return "ELIMINATED";
        if (player.HasFolded)
            return "FOLDED";
        if (player.IsAllIn)
            return "ALL-IN";
        if (gameState.HasPlayerActed(player))
            return "ACTED";
        if (gameState.CurrentPlayerPosition == gameState.Players.IndexOf(player))
            return "ACTING";
        
        return "WAITING";
    }

    public void DisplayPlayerHand(IPlayer player, List<Card> communityCards)
    {
        Console.WriteLine($"\n🃏 {player.Name}'s Hand:");
        Console.WriteLine($"   Hole Cards: {string.Join(" ", player.HoleCards.Select(FormatCard))}");
        
        if (communityCards.Count >= 3)
        {
            var allCards = player.HoleCards.Concat(communityCards).ToList();
            var handResult = HandEvaluator.EvaluateHand(allCards);
            Console.WriteLine($"   Best Hand: {handResult.Description}");
        }
    }

    public void DisplayCommunityCards(List<Card> cards, BettingPhase phase)
    {
        if (!cards.Any()) return;

        var phaseNames = new Dictionary<BettingPhase, string>
        {
            { BettingPhase.Flop, "FLOP" },
            { BettingPhase.Turn, "TURN" },
            { BettingPhase.River, "RIVER" },
            { BettingPhase.Showdown, "FINAL BOARD" }
        };

        var phaseName = phaseNames.GetValueOrDefault(phase, "COMMUNITY CARDS");
        
        Console.WriteLine($"\n🎴 {phaseName}:");
        Console.WriteLine($"   {string.Join(" ", cards.Select(FormatCard))}");
    }

    public void DisplayActionSummary(List<PlayerAction> actions)
    {
        if (!actions.Any()) return;

        Console.WriteLine("\n📋 RECENT ACTIONS:");
        foreach (var action in actions.TakeLast(5))
        {
            var amountStr = action.Amount > 0 ? $" €{action.Amount}" : "";
            var timestamp = action.Timestamp.ToString("HH:mm:ss");
            Console.WriteLine($"   [{timestamp}] {action.PlayerId}: {action.Action}{amountStr}");
        }
    }

    public void DisplayPotInformation(int totalPot, List<SidePot> sidePots)
    {
        Console.WriteLine($"\n💰 POT INFORMATION:");
        Console.WriteLine($"   Total Pot: €{totalPot}");
        
        if (sidePots.Any())
        {
            Console.WriteLine("   Side Pots:");
            for (int i = 0; i < sidePots.Count; i++)
            {
                var sidePot = sidePots[i];
                Console.WriteLine($"     Side Pot {i + 1}: €{sidePot.Amount} ({sidePot.EligiblePlayers.Count} players)");
            }
        }
    }

    public void DisplayWinners(List<PotWinner> winners)
    {
        Console.WriteLine("\n🏆 HAND WINNERS:");
        Console.WriteLine(new string('-', 50));
        
        foreach (var winner in winners)
        {
            Console.WriteLine($"🎉 {winner.Player.Name} wins €{winner.Amount} from {winner.PotType}");
            Console.WriteLine($"    with {winner.HandDescription}");
        }
    }

    public void DisplayHandRankings()
    {
        Console.WriteLine("\n🃏 POKER HAND RANKINGS (Highest to Lowest):");
        Console.WriteLine(new string('-', 45));
        
        var rankings = new[]
        {
            "1. Royal Flush      - A♠ K♠ Q♠ J♠ 10♠",
            "2. Straight Flush   - 9♥ 8♥ 7♥ 6♥ 5♥",
            "3. Four of a Kind   - A♠ A♥ A♦ A♣ K♠",
            "4. Full House       - K♠ K♥ K♦ Q♠ Q♥",
            "5. Flush            - A♠ J♠ 9♠ 6♠ 4♠",
            "6. Straight         - 10♠ 9♥ 8♦ 7♣ 6♠",
            "7. Three of a Kind  - Q♠ Q♥ Q♦ J♠ 9♥",
            "8. Two Pair         - J♠ J♥ 8♦ 8♣ A♠",
            "9. One Pair         - 10♠ 10♥ A♦ 5♣ 4♠",
            "10. High Card       - A♠ K♥ Q♦ J♣ 9♠"
        };

        foreach (var ranking in rankings)
        {
            Console.WriteLine($"    {ranking}");
        }
    }

    public void DisplayGameStatistics(GameStatistics stats)
    {
        Console.WriteLine("\n📊 GAME STATISTICS:");
        Console.WriteLine(new string('-', 30));
        Console.WriteLine($"Hands Played: {stats.HandsPlayed}");
        Console.WriteLine($"Players Remaining: {stats.PlayersRemaining}");
        Console.WriteLine($"Current Blinds: {stats.CurrentBlinds}");
        
        if (stats.RoundHistory.Any())
        {
            var avgPot = stats.RoundHistory.Average(r => r.TotalPot);
            var maxPot = stats.RoundHistory.Max(r => r.TotalPot);
            Console.WriteLine($"Average Pot: €{avgPot:F0}");
            Console.WriteLine($"Largest Pot: €{maxPot}");
        }
    }

    private string FormatCard(Card card)
    {
        if (_useColors)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = card.GetSuitColor();
            var result = card.GetDisplayString();
            Console.ForegroundColor = oldColor;
            return result;
        }
        
        return card.GetDisplayString(false); // Use text-based display
    }

    public void ShowThinkingAnimation(string playerName, int durationMs = 2000)
    {
        Console.Write($"🤔 {playerName} is thinking");
        
        var endTime = DateTime.Now.AddMilliseconds(durationMs);
        while (DateTime.Now < endTime)
        {
            Console.Write(".");
            Thread.Sleep(300);
        }
        
        Console.WriteLine();
    }

    public void ShowDealingAnimation(string message, int delayMs = 1000)
    {
        Console.Write($"🃏 {message}");
        
        for (int i = 0; i < 3; i++)
        {
            Thread.Sleep(delayMs / 3);
            Console.Write(".");
        }
        
        Console.WriteLine(" Done!");
        Thread.Sleep(500);
    }

    public void ClearScreen()
    {
        try
        {
            Console.Clear();
        }
        catch
        {
            // Fallback if clear doesn't work
            Console.WriteLine(new string('\n', 10));
        }
    }

    public void DrawSeparator(char character = '=', int length = 60)
    {
        Console.WriteLine(new string(character, length));
    }

    public void ShowColoredMessage(string message, ConsoleColor color)
    {
        if (_useColors)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = oldColor;
        }
        else
        {
            Console.WriteLine(message);
        }
    }

    public void DisplayPlayerPersonalities(List<IPlayer> players)
    {
        Console.WriteLine("\n🎭 PLAYER PERSONALITIES:");
        Console.WriteLine(new string('-', 40));

        foreach (var player in players)
        {
            var personalityStr = player.Personality?.ToString() ?? "Human";
            Console.WriteLine($"   {player.Name}: {personalityStr}");
        }
    }

    // Table layout constants
    private const int LeftPlayerWidth = 22;
    private const int TableInnerWidth = 47;
    private const int RightPlayerWidth = 22;

    /// <summary>
    /// Display the visual poker table with players around it and community cards in center
    /// </summary>
    public void DisplayVisualPokerTable(GameState gameState, int? highlightPlayerIndex = null)
    {
        Console.WriteLine();

        var players = gameState.Players.Where(p => p.IsActive).ToList();
        var playerCount = players.Count;

        // Build community cards display
        var communityCards = new List<Card?>();
        for (int i = 0; i < 5; i++)
        {
            communityCards.Add(i < gameState.CommunityCards.Count ? gameState.CommunityCards[i] : null);
        }

        // Calculate positions for players (simplified for 2-8 players)
        var (leftPlayers, rightPlayers, topPlayers, bottomPlayers) = DistributePlayersAroundTable(players, gameState);

        // Display top players (centered above table)
        DisplayTopPlayers(topPlayers, gameState, highlightPlayerIndex);

        // Build table border (aligned with left player area)
        var tableBorder = new string('═', TableInnerWidth);
        Console.WriteLine($"{new string(' ', LeftPlayerWidth)}╔{tableBorder}╗");

        // Display left/right players alongside the table middle section
        var tableMiddleLines = BuildTableMiddle(communityCards, gameState);

        // Calculate which rows to show each player on (spread evenly)
        var leftPlayerRows = CalculatePlayerRows(leftPlayers.Count, tableMiddleLines.Count);
        var rightPlayerRows = CalculatePlayerRows(rightPlayers.Count, tableMiddleLines.Count);

        for (int i = 0; i < tableMiddleLines.Count; i++)
        {
            var leftStr = "";
            var rightStr = "";

            // Check if a left player should be displayed on this row
            var leftPlayerIdx = leftPlayerRows.IndexOf(i);
            if (leftPlayerIdx >= 0 && leftPlayerIdx < leftPlayers.Count)
            {
                leftStr = FormatPlayerBox(leftPlayers[leftPlayerIdx], gameState, highlightPlayerIndex, true);
            }

            // Check if a right player should be displayed on this row
            var rightPlayerIdx = rightPlayerRows.IndexOf(i);
            if (rightPlayerIdx >= 0 && rightPlayerIdx < rightPlayers.Count)
            {
                rightStr = FormatPlayerBox(rightPlayers[rightPlayerIdx], gameState, highlightPlayerIndex, false);
            }

            // Write left side (fixed width)
            Console.Write($"{leftStr,-LeftPlayerWidth}║");

            // Write middle section with colored cards
            WriteTableMiddleLine(tableMiddleLines[i], communityCards, i, gameState);

            Console.WriteLine($"║{rightStr}");
        }

        // Display table bottom border
        Console.WriteLine($"{new string(' ', LeftPlayerWidth)}╚{tableBorder}╝");

        // Display bottom players (centered below table)
        DisplayBottomPlayers(bottomPlayers, gameState, highlightPlayerIndex);
    }

    private (List<IPlayer> left, List<IPlayer> right, List<IPlayer> top, List<IPlayer> bottom) DistributePlayersAroundTable(
        List<IPlayer> players, GameState gameState)
    {
        var left = new List<IPlayer>();
        var right = new List<IPlayer>();
        var top = new List<IPlayer>();
        var bottom = new List<IPlayer>();

        var count = players.Count;

        if (count <= 2)
        {
            // 2 players: top and bottom
            if (count >= 1) bottom.Add(players[0]);
            if (count >= 2) top.Add(players[1]);
        }
        else if (count <= 4)
        {
            // 3-4 players: corners
            bottom.Add(players[0]);
            if (count >= 2) left.Add(players[1]);
            if (count >= 3) top.Add(players[2]);
            if (count >= 4) right.Add(players[3]);
        }
        else
        {
            // 5-8 players: distribute evenly
            bottom.Add(players[0]);
            if (count >= 2) left.Add(players[1]);
            if (count >= 3) left.Add(players[2]);
            if (count >= 4) top.Add(players[3]);
            if (count >= 5) top.Add(players[4]);
            if (count >= 6) right.Add(players[5]);
            if (count >= 7) right.Add(players[6]);
            if (count >= 8) bottom.Add(players[7]);
        }

        return (left, right, top, bottom);
    }

    private List<string> BuildTableMiddle(List<Card?> communityCards, GameState gameState)
    {
        var lines = new List<string>();
        var potDisplay = $"POT: €{gameState.TotalPot}";
        var phaseDisplay = gameState.Phase.ToString().ToUpper();

        // Line 0: Phase header (centered)
        var phasePadding = (TableInnerWidth - phaseDisplay.Length) / 2;
        lines.Add(CenterText(phaseDisplay, TableInnerWidth));

        // Lines 1-5: Placeholders for community cards (actual rendering with colors happens in WriteTableMiddleLine)
        for (int i = 0; i < 5; i++)
        {
            lines.Add("CARD_LINE"); // Placeholder - will be rendered with colors
        }

        // Line 6: Empty
        lines.Add(new string(' ', TableInnerWidth));

        // Line 7: Pot info (centered)
        lines.Add(CenterText(potDisplay, TableInnerWidth));

        // Line 8: Dealer button indicator (centered)
        var dealerName = gameState.Dealer?.Name ?? "?";
        var dealerDisplay = $"[D] {dealerName}";
        lines.Add(CenterText(dealerDisplay, TableInnerWidth));

        return lines;
    }

    private string CenterText(string text, int width)
    {
        if (text.Length >= width) return text[..width];
        var padding = (width - text.Length) / 2;
        return new string(' ', padding) + text + new string(' ', width - padding - text.Length);
    }

    /// <summary>
    /// Calculate which rows each player should be displayed on (spread evenly)
    /// </summary>
    private List<int> CalculatePlayerRows(int playerCount, int totalRows)
    {
        var rows = new List<int>();
        if (playerCount == 0) return rows;

        // Use rows 2-6 for player display (middle section of table)
        var availableRows = new List<int> { 2, 4, 6 }; // Spread out

        for (int i = 0; i < playerCount && i < availableRows.Count; i++)
        {
            rows.Add(availableRows[i]);
        }

        return rows;
    }

    /// <summary>
    /// Write a single line of the table middle section with colored cards
    /// </summary>
    private void WriteTableMiddleLine(string placeholder, List<Card?> communityCards, int lineIndex, GameState gameState)
    {
        // Lines 1-5 are the card lines
        if (lineIndex >= 1 && lineIndex <= 5)
        {
            var cardLineIndex = lineIndex - 1; // 0-4 for card lines
            WriteCardLineColored(communityCards, cardLineIndex, gameState.Phase == GamePhase.PreFlop);
        }
        else
        {
            // Regular text line
            Console.Write(placeholder);
        }
    }

    /// <summary>
    /// Write a single line of the community cards with colors
    /// </summary>
    private void WriteCardLineColored(List<Card?> cards, int lineIndex, bool showHidden)
    {
        var originalColor = Console.ForegroundColor;

        // Get card arts
        var cardArts = new List<(string[] lines, ConsoleColor? color)>();
        foreach (var card in cards)
        {
            if (card == null)
            {
                cardArts.Add((showHidden ? Card.GetHiddenCardAscii() : Card.GetEmptySlotAscii(), null));
            }
            else
            {
                cardArts.Add((card.GetAsciiArt(), card.GetSuitColor()));
            }
        }

        // Calculate card line width: 5 cards * 7 chars + 4 spaces = 39 chars
        var cardLineWidth = (cardArts.Count * 7) + (cardArts.Count - 1);
        var padding = (TableInnerWidth - cardLineWidth) / 2;

        // Write left padding
        Console.Write(new string(' ', Math.Max(0, padding)));

        // Write each card's line with color
        for (int cardIdx = 0; cardIdx < cardArts.Count; cardIdx++)
        {
            var (lines, color) = cardArts[cardIdx];
            var lineText = lines[lineIndex];

            foreach (char c in lineText)
            {
                if ((c == '♥' || c == '♦' || c == '♣' || c == '♠') && color.HasValue)
                {
                    Console.ForegroundColor = color.Value;
                    Console.Write(c);
                    Console.ForegroundColor = originalColor;
                }
                else
                {
                    Console.Write(c);
                }
            }

            if (cardIdx < cardArts.Count - 1)
                Console.Write(" ");
        }

        // Write right padding
        var rightPadding = TableInnerWidth - padding - cardLineWidth;
        Console.Write(new string(' ', Math.Max(0, rightPadding)));
    }

    private void DisplayTopPlayers(List<IPlayer> players, GameState gameState, int? highlightIndex)
    {
        if (!players.Any()) return;

        // Calculate spacing to center players above table
        var playerWidth = 24; // Width per player display
        var totalPlayerWidth = players.Count * playerWidth;
        var tableStart = LeftPlayerWidth;
        var centerOffset = tableStart + (TableInnerWidth - totalPlayerWidth) / 2;
        var padding = new string(' ', Math.Max(0, centerOffset));

        var line1 = "";
        var line2 = "";
        var line3 = "";

        foreach (var player in players)
        {
            var isHighlighted = highlightIndex.HasValue && gameState.Players.IndexOf(player) == highlightIndex.Value;
            var (l1, l2, l3) = GetPlayerDisplayLines(player, gameState, isHighlighted);
            line1 += l1.PadRight(playerWidth);
            line2 += l2.PadRight(playerWidth);
            line3 += l3.PadRight(playerWidth);
        }

        Console.WriteLine($"{padding}{line1}");
        Console.WriteLine($"{padding}{line2}");
        Console.WriteLine($"{padding}{line3}");
    }

    private void DisplayBottomPlayers(List<IPlayer> players, GameState gameState, int? highlightIndex)
    {
        if (!players.Any()) return;

        // Calculate spacing to center players below table
        var playerWidth = 24; // Width per player display
        var totalPlayerWidth = players.Count * playerWidth;
        var tableStart = LeftPlayerWidth;
        var centerOffset = tableStart + (TableInnerWidth - totalPlayerWidth) / 2;
        var padding = new string(' ', Math.Max(0, centerOffset));

        var line1 = "";
        var line2 = "";
        var line3 = "";

        foreach (var player in players)
        {
            var isHighlighted = highlightIndex.HasValue && gameState.Players.IndexOf(player) == highlightIndex.Value;
            var (l1, l2, l3) = GetPlayerDisplayLines(player, gameState, isHighlighted);
            line1 += l1.PadRight(playerWidth);
            line2 += l2.PadRight(playerWidth);
            line3 += l3.PadRight(playerWidth);
        }

        Console.WriteLine($"{padding}{line1}");
        Console.WriteLine($"{padding}{line2}");
        Console.WriteLine($"{padding}{line3}");
    }

    private string FormatPlayerBox(IPlayer player, GameState gameState, int? highlightIndex, bool isLeft)
    {
        var isHighlighted = highlightIndex.HasValue && gameState.Players.IndexOf(player) == highlightIndex.Value;
        var status = GetPlayerStatusShort(player, gameState);
        var position = GetPositionBadge(gameState.Players.IndexOf(player), gameState);

        var name = player.Name.Length > 10 ? player.Name[..10] : player.Name;
        var chips = $"€{player.Chips}";

        // Build compact display that fits in LeftPlayerWidth
        var indicator = isHighlighted ? "→" : " ";
        return $"{indicator}{name} {chips} {position}";
    }

    private (string, string, string) GetPlayerDisplayLines(IPlayer player, GameState gameState, bool isHighlighted)
    {
        var status = GetPlayerStatusShort(player, gameState);
        var position = GetPositionBadge(gameState.Players.IndexOf(player), gameState);
        var name = player.Name.Length > 14 ? player.Name[..14] : player.Name;

        var indicator = isHighlighted ? "→ " : "  ";
        var line1 = $"{indicator}{name}";
        var line2 = $"  €{player.Chips}";
        var line3 = $"  {position} {status}";

        return (line1, line2, line3);
    }

    private string GetPlayerStatusShort(IPlayer player, GameState gameState)
    {
        if (!player.IsActive) return "OUT";
        if (player.HasFolded) return "FOLD";
        if (player.IsAllIn) return "ALL-IN";
        return "";
    }

    private string GetPositionBadge(int playerIndex, GameState gameState)
    {
        var badges = new List<string>();

        if (playerIndex == gameState.DealerPosition) badges.Add("[D]");
        if (playerIndex == gameState.SmallBlindPosition) badges.Add("[SB]");
        if (playerIndex == gameState.BigBlindPosition) badges.Add("[BB]");

        return string.Join("", badges);
    }

    /// <summary>
    /// Display community cards with ASCII art
    /// </summary>
    public void DisplayCommunityCardsAscii(List<Card> cards, BettingPhase phase)
    {
        var phaseNames = new Dictionary<BettingPhase, string>
        {
            { BettingPhase.Flop, "═══ FLOP ═══" },
            { BettingPhase.Turn, "═══ TURN ═══" },
            { BettingPhase.River, "═══ RIVER ═══" },
        };

        var phaseName = phaseNames.GetValueOrDefault(phase, "═══ BOARD ═══");

        Console.WriteLine();
        Console.WriteLine($"          {phaseName}");

        if (cards.Any())
        {
            Console.Write("     ");
            Card.WriteCardsHorizontallyColored(cards.Cast<Card?>());
        }
    }

    /// <summary>
    /// Display player's hole cards with ASCII art
    /// </summary>
    public void DisplayHoleCardsAscii(IPlayer player)
    {
        Console.WriteLine();
        Console.WriteLine($"🃏 Your cards:");
        Console.Write("     ");
        Card.WriteCardsHorizontallyColored(player.HoleCards.Cast<Card?>());
    }

    /// <summary>
    /// Animated progress bar
    /// </summary>
    public void ShowProgressBar(string label, int durationMs = 2000, int width = 20)
    {
        Console.Write($"{label} [");
        var stepDelay = durationMs / width;

        for (int i = 0; i < width; i++)
        {
            Console.Write("█");
            Thread.Sleep(stepDelay);
        }

        Console.WriteLine("]");
    }

    /// <summary>
    /// Animated chip movement
    /// </summary>
    public void ShowChipAnimation(string from, string to, int amount)
    {
        Console.Write($"💰 {from} ");
        for (int i = 0; i < 5; i++)
        {
            Console.Write("━");
            Thread.Sleep(100);
        }
        Console.WriteLine($"→ {to} (€{amount})");
    }

    /// <summary>
    /// Display a player info box
    /// </summary>
    public void DisplayPlayerInfoBox(IPlayer player, GameState gameState, bool showCards = false)
    {
        var status = GetPlayerStatus(player, gameState);
        var position = GetPositionBadge(gameState.Players.IndexOf(player), gameState);
        var personality = player.Personality?.ToString() ?? "Human";

        Console.WriteLine("╔═════════════════════╗");
        Console.WriteLine($"║ {player.Name,-19} ║");
        Console.WriteLine($"║ 💰 €{player.Chips,-15} ║");
        Console.WriteLine($"║ 🎭 {personality,-16} ║");
        Console.WriteLine($"║ {position} {status,-13} ║");

        if (showCards && player.HoleCards.Any())
        {
            var cardStr = string.Join(" ", player.HoleCards.Select(c => c.GetDisplayString()));
            Console.WriteLine($"║ 🃏 {cardStr,-16} ║");
        }

        Console.WriteLine("╚═════════════════════╝");
    }

    /// <summary>
    /// Display pot information box
    /// </summary>
    public void DisplayPotBox(int mainPot, List<SidePot>? sidePots = null)
    {
        Console.WriteLine("┌─────────────────────────┐");
        Console.WriteLine($"│ 💰 MAIN POT: €{mainPot,-10} │");

        if (sidePots?.Any() == true)
        {
            foreach (var (pot, index) in sidePots.Select((p, i) => (p, i)))
            {
                Console.WriteLine($"│ 💰 SIDE {index + 1}:   €{pot.Amount,-10} │");
            }
        }

        Console.WriteLine("└─────────────────────────┘");
    }
}