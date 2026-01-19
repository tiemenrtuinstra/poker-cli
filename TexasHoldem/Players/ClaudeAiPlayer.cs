using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Players;

/// <summary>
/// AI player powered by Anthropic's Claude API
/// </summary>
public class ClaudeAiPlayer : LlmAiPlayer
{
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    public ClaudeAiPlayer(
        string name,
        int startingChips,
        PersonalityType personality,
        string? apiKey,
        string modelName = "claude-sonnet-4-20250514",
        Random? random = null)
        : base(name, startingChips, personality, AiProvider.Claude, apiKey, modelName, random)
    {
    }

    protected override async Task<string> CallLlmApiAsync(string prompt)
    {
        var requestBody = new
        {
            model = _modelName,
            max_tokens = 256,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);

        var response = await _httpClient.PostAsync(ApiUrl, content).ConfigureAwait(false);
        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Claude API error: {response.StatusCode} - {responseString}");
        }

        // Parse Claude response format
        using var doc = JsonDocument.Parse(responseString);
        var textContent = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return textContent ?? throw new Exception("Empty response from Claude API");
    }

    public static ClaudeAiPlayer CreateClaudeAiPlayer(
        int startingChips,
        string? apiKey,
        string modelName = "claude-sonnet-4-20250514",
        Random? random = null)
    {
        random ??= new Random();
        var personality = AiNameGenerator.GetRandomPersonality();
        var name = AiNameGenerator.GenerateName(personality, true);

        return new ClaudeAiPlayer(name, startingChips, personality, apiKey, modelName, random);
    }
}
