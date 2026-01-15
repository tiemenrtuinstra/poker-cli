using TexasHoldem.Domain;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Players;

public interface IPlayer
{
    string Name { get; }
    int Chips { get; set; }
    List<Card> HoleCards { get; set; }
    bool IsActive { get; set; }
    bool IsAllIn { get; set; }
    bool HasFolded { get; set; }
    PersonalityType? Personality { get; }

    PlayerAction TakeTurn(GameState gameState);
    void ReceiveCards(List<Card> cards);
    void AddChips(int amount);
    bool RemoveChips(int amount);
    void Reset();
    void ResetForNewGame();
    void ShowCards();
    void HideCards();
}