using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Players;

/// <summary>
/// AI player powered by OpenAI's GPT API
/// </summary>
public class OpenAiPlayer : LlmAiPlayer
{
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

    public OpenAiPlayer(
        string name,
        int startingChips,
        PersonalityType personality,
        string? apiKey,
        string modelName = "gpt-4o-mini",
        Random? random = null)
        : base(name, startingChips, personality, AiProvider.OpenAI, apiKey, modelName, random)
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
                new
                {
                    role = "system",
                    content = "You are a poker AI assistant. Respond only with valid JSON."
                },
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.PostAsync(ApiUrl, content).ConfigureAwait(false);
        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"OpenAI API error: {response.StatusCode} - {responseString}");
        }

        // Parse OpenAI response format
        using var doc = JsonDocument.Parse(responseString);
        var textContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return textContent ?? throw new Exception("Empty response from OpenAI API");
    }

    public static OpenAiPlayer CreateOpenAiPlayer(
        int startingChips,
        string? apiKey,
        string modelName = "gpt-4o-mini",
        Random? random = null)
    {
        random ??= new Random();
        var personality = AiNameGenerator.GetRandomPersonality();
        var name = AiNameGenerator.GenerateName(personality, true);

        return new OpenAiPlayer(name, startingChips, personality, apiKey, modelName, random);
    }
}
