using TexasHoldem.Game;
using TexasHoldem.Game.Enums;
using TexasHoldem.Players;

namespace TexasHoldem.CLI;

/// <summary>
/// A no-op implementation of IGameUI for testing purposes.
/// All methods do nothing, making tests run without console output.
/// </summary>
public class NullGameUI : IGameUI
{
    public void ClearScreen() { }

    public void DrawSeparator(char character = '=', int length = 60) { }

    public void ShowColoredMessage(string message, ConsoleColor color) { }

    public void DisplayVisualPokerTable(GameState gameState, int? highlightPlayerIndex = null) { }

    public void DisplayPotBox(int mainPot, List<SidePot>? sidePots = null) { }

    public void DisplayHoleCardsAscii(IPlayer player) { }

    public void DisplayCommunityCardsAscii(List<Card> cards, BettingPhase phase) { }

    public void DisplayPlayerTurn(IPlayer player, int chipsRemaining) { }

    public void DisplayPlayerSummary(List<IPlayer> players, int dealerPosition) { }

    public void DisplayPlayerAction(IPlayer player, ActionType action, int amount = 0, string? message = null) { }

    public void DisplayBettingComplete(int potAmount) { }

    public void DisplayHandHeader(int handNumber, List<IPlayer> players, IPlayer? dealer) { }

    public void DisplayPhaseHeader(BettingPhase phase) { }

    public void DisplayBlindsPosted(IPlayer smallBlind, int sbAmount, IPlayer bigBlind, int bbAmount) { }

    public void DisplayPreparingNextHand(string newDealer) { }

    public void DisplayHandCompleted(int handNumber, double seconds) { }

    public void DisplayBlindsIncrease(int oldSmallBlind, int oldBigBlind, int newSmallBlind, int newBigBlind) { }

    public void DisplayShowdownHands(List<IPlayer> players, List<Card> communityCards) { }

    public void DisplayShowdownWinners(List<Game.PotWinner> winners) { }

    public void DisplayFoldWinner(IPlayer winner, int amount) { }

    public void DisplayHandSummary(int totalPot, int bettingRounds, List<IPlayer> players) { }

    public void DisplayGameOver(List<IPlayer> activePlayers) { }

    public void DisplayGameStatistics(int handsPlayed, TimeSpan duration, double? avgPot, int? maxPot, int? totalBettingRounds) { }

    public void DisplayThanksForPlaying() { }

    public void ShowDealingAnimation(string message, int durationMs) { }

    public void ShowChipAnimation(string from, string to, int amount) { }
}
