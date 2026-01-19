using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;
using TexasHoldem.Players;
using TexasHoldem.Game;

namespace TexasHoldem.CLI;

/// <summary>
/// Interface for game UI operations, enabling testability and alternative UI implementations.
/// </summary>
public interface IGameUI
{
    #region Screen Management

    /// <summary>
    /// Clear the screen
    /// </summary>
    void ClearScreen();

    /// <summary>
    /// Draw a separator line
    /// </summary>
    void DrawSeparator(char character = '=', int length = 60);

    /// <summary>
    /// Show a colored message
    /// </summary>
    void ShowColoredMessage(string message, ConsoleColor color);

    #endregion

    #region Table Display

    /// <summary>
    /// Display the visual poker table with players around it
    /// </summary>
    void DisplayVisualPokerTable(GameState gameState, int? highlightPlayerIndex = null);

    /// <summary>
    /// Display pot information box
    /// </summary>
    void DisplayPotBox(int mainPot, List<SidePot>? sidePots = null);

    #endregion

    #region Card Display

    /// <summary>
    /// Display player's hole cards with ASCII art
    /// </summary>
    void DisplayHoleCardsAscii(IPlayer player);

    /// <summary>
    /// Display community cards with ASCII art
    /// </summary>
    void DisplayCommunityCardsAscii(List<Card> cards, BettingPhase phase);

    #endregion

    #region Player Information

    /// <summary>
    /// Display player turn header
    /// </summary>
    void DisplayPlayerTurn(IPlayer player, int chipsRemaining);

    /// <summary>
    /// Display player summary for between hands
    /// </summary>
    void DisplayPlayerSummary(List<IPlayer> players, int dealerPosition);

    #endregion

    #region Player Actions

    /// <summary>
    /// Display player action in a nice format
    /// </summary>
    void DisplayPlayerAction(IPlayer player, ActionType action, int amount = 0, string? message = null);

    /// <summary>
    /// Display betting round complete
    /// </summary>
    void DisplayBettingComplete(int potAmount);

    #endregion

    #region Game Flow

    /// <summary>
    /// Display hand header (HAND #X)
    /// </summary>
    void DisplayHandHeader(int handNumber, List<IPlayer> players, IPlayer? dealer);

    /// <summary>
    /// Display betting phase header (PRE-FLOP, FLOP, TURN, RIVER)
    /// </summary>
    void DisplayPhaseHeader(BettingPhase phase);

    /// <summary>
    /// Display blinds posted
    /// </summary>
    void DisplayBlindsPosted(IPlayer smallBlind, int sbAmount, IPlayer bigBlind, int bbAmount);

    /// <summary>
    /// Display preparing for next hand
    /// </summary>
    void DisplayPreparingNextHand(string newDealer);

    /// <summary>
    /// Display hand completed time
    /// </summary>
    void DisplayHandCompleted(int handNumber, double seconds);

    /// <summary>
    /// Display blinds increase notification
    /// </summary>
    void DisplayBlindsIncrease(int oldSmallBlind, int oldBigBlind, int newSmallBlind, int newBigBlind);

    #endregion

    #region Showdown & Winners

    /// <summary>
    /// Display all players' hands at showdown with nice formatting
    /// </summary>
    void DisplayShowdownHands(List<IPlayer> players, List<Card> communityCards);

    /// <summary>
    /// Display winners at showdown with nice formatting
    /// </summary>
    void DisplayShowdownWinners(List<Game.PotWinner> winners);

    /// <summary>
    /// Display winner when everyone folds
    /// </summary>
    void DisplayFoldWinner(IPlayer winner, int amount);

    /// <summary>
    /// Display hand summary
    /// </summary>
    void DisplayHandSummary(int totalPot, int bettingRounds, List<IPlayer> players);

    #endregion

    #region Game End

    /// <summary>
    /// Display game over with winner information
    /// </summary>
    void DisplayGameOver(List<IPlayer> activePlayers);

    /// <summary>
    /// Display game statistics in a nice table
    /// </summary>
    void DisplayGameStatistics(int handsPlayed, TimeSpan duration, double? avgPot, int? maxPot, int? totalBettingRounds);

    /// <summary>
    /// Display thanks for playing message
    /// </summary>
    void DisplayThanksForPlaying();

    #endregion

    #region Animations

    /// <summary>
    /// Show dealing animation with custom duration
    /// </summary>
    void ShowDealingAnimation(string message, int durationMs);

    /// <summary>
    /// Show chip movement animation
    /// </summary>
    void ShowChipAnimation(string from, string to, int amount);

    #endregion
}
