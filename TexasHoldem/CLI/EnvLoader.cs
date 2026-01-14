namespace TexasHoldem.CLI;

/// <summary>
/// Loads environment variables from .env files.
/// Provides secure storage for API keys outside of config files.
/// </summary>
public static class EnvLoader
{
    private static bool _loaded = false;
    private static readonly string[] EnvFilePaths = new[]
    {
        ".env",
        "../.env",
        "TexasHoldem/.env"
    };

    /// <summary>
    /// Load environment variables from .env file if it exists.
    /// Call this early in application startup.
    /// </summary>
    public static void Load()
    {
        if (_loaded) return;

        foreach (var path in EnvFilePaths)
        {
            if (TryLoadEnvFile(path))
            {
                Console.WriteLine($"  Loaded environment from {Path.GetFullPath(path)}");
                break;
            }
        }

        _loaded = true;
    }

    private static bool TryLoadEnvFile(string path)
    {
        if (!File.Exists(path)) return false;

        try
        {
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                // Skip empty lines and comments
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                    continue;

                // Parse KEY=VALUE format
                var separatorIndex = trimmedLine.IndexOf('=');
                if (separatorIndex <= 0) continue;

                var key = trimmedLine.Substring(0, separatorIndex).Trim();
                var value = trimmedLine.Substring(separatorIndex + 1).Trim();

                // Remove surrounding quotes if present
                if ((value.StartsWith('"') && value.EndsWith('"')) ||
                    (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                // Only set if not already set (system env vars take precedence)
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Failed to load {path}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get an environment variable value, returning null if not set or empty
    /// </summary>
    public static string? GetEnv(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    /// Get an environment variable value with a default fallback
    /// </summary>
    public static string GetEnv(string key, string defaultValue)
    {
        return GetEnv(key) ?? defaultValue;
    }

    /// <summary>
    /// Check if an API key is configured for a provider
    /// </summary>
    public static bool HasApiKey(string provider)
    {
        return provider.ToLower() switch
        {
            "claude" => !string.IsNullOrEmpty(GetEnv("CLAUDE_API_KEY")),
            "gemini" => !string.IsNullOrEmpty(GetEnv("GEMINI_API_KEY")),
            "openai" => !string.IsNullOrEmpty(GetEnv("OPENAI_API_KEY")),
            _ => false
        };
    }

    /// <summary>
    /// Get a summary of which API keys are configured
    /// </summary>
    public static string GetApiKeyStatus()
    {
        var status = new List<string>();

        if (HasApiKey("claude")) status.Add("Claude");
        if (HasApiKey("gemini")) status.Add("Gemini");
        if (HasApiKey("openai")) status.Add("OpenAI");

        return status.Any()
            ? $"API keys found: {string.Join(", ", status)}"
            : "No API keys configured (using basic AI)";
    }
}
