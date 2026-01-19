namespace TexasHoldem.Data.Entities;

public class PlayerEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string PlayerType { get; set; } // Human, BasicAI, LlmAI
    public string? Personality { get; set; }
    public string? AiProvider { get; set; } // Claude, OpenAI, Gemini

    // Navigation properties
    public ICollection<HandParticipantEntity> HandParticipations { get; set; } = new List<HandParticipantEntity>();
    public ICollection<ActionEntity> Actions { get; set; } = new List<ActionEntity>();
    public ICollection<OutcomeEntity> Outcomes { get; set; } = new List<OutcomeEntity>();
}
