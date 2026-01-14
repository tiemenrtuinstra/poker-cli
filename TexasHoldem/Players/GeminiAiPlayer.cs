using System.Text;
using System.Text.Json;
using TexasHoldem.Domain.Enums;

namespace TexasHoldem.Players;

/// <summary>
/// AI player powered by Google's Gemini API
/// </summary>
public class GeminiAiPlayer : LlmAiPlayer
{
    private const string ApiUrlTemplate = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

    public GeminiAiPlayer(
        string name,
        int startingChips,
        PersonalityType personality,
        string? apiKey,
        string modelName = "gemini-2.0-flash",
        Random? random = null)
        : base(name, startingChips, personality, AiProvider.Gemini, apiKey, modelName, random)
    {
    }

    protected override async Task<string> CallLlmApiAsync(string prompt)
    {
        var apiUrl = string.Format(ApiUrlTemplate, _modelName, _apiKey);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 256,
                temperature = 0.7
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(apiUrl, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Gemini API error: {response.StatusCode} - {responseString}");
        }

        // Parse Gemini response format
        using var doc = JsonDocument.Parse(responseString);
        var textContent = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return textContent ?? throw new Exception("Empty response from Gemini API");
    }

    public static GeminiAiPlayer CreateGeminiAiPlayer(
        int startingChips,
        string? apiKey,
        string modelName = "gemini-2.0-flash",
        Random? random = null)
    {
        random ??= new Random();
        var personality = AiNameGenerator.GetRandomPersonality();
        var name = AiNameGenerator.GenerateName(personality, true);

        return new GeminiAiPlayer(name, startingChips, personality, apiKey, modelName, random);
    }
}
