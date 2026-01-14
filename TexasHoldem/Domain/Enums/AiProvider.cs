namespace TexasHoldem.Domain.Enums;

/// <summary>
/// Supported AI providers for LLM-powered poker players
/// </summary>
public enum AiProvider
{
    /// <summary>
    /// No LLM - uses basic rule-based AI
    /// </summary>
    None,

    /// <summary>
    /// Anthropic's Claude AI
    /// </summary>
    Claude,

    /// <summary>
    /// Google's Gemini AI
    /// </summary>
    Gemini,

    /// <summary>
    /// OpenAI's GPT models
    /// </summary>
    OpenAI
}
