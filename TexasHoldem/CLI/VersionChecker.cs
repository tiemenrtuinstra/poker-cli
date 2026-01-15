using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace TexasHoldem.CLI;

public class VersionChecker
{
    private const string GitHubRepo = "tiemenrtuinstra/poker-cli";
    private const string GitHubApiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    static VersionChecker()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "poker-cli");
        HttpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    public static async Task CheckForUpdatesAsync(bool? forceCheck = null)
    {
        try
        {
            // Check config to see if update check is enabled
            if (forceCheck != true)
            {
                var configManager = new ConfigurationManager();
                var config = configManager.GetConfiguration();
                if (!config.Updates.CheckForUpdatesOnStartup)
                    return;
            }

            var currentVersion = GetCurrentVersion();
            var latestRelease = await GetLatestReleaseAsync();

            if (latestRelease == null || string.IsNullOrEmpty(latestRelease.TagName))
                return;

            var latestVersion = ParseVersion(latestRelease.TagName);

            if (latestVersion > currentVersion)
            {
                DisplayUpdateNotification(currentVersion, latestVersion, latestRelease);
            }
        }
        catch
        {
            // Silently ignore version check failures - don't disrupt the user experience
        }
    }

    private static Version GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version ?? new Version(1, 0, 0);
    }

    private static Version ParseVersion(string tagName)
    {
        var versionString = tagName.TrimStart('v', 'V');
        return Version.TryParse(versionString, out var version)
            ? version
            : new Version(0, 0, 0);
    }

    private static async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        var response = await HttpClient.GetAsync(GitHubApiUrl);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<GitHubRelease>();
    }

    private static void DisplayUpdateNotification(Version current, Version latest, GitHubRelease release)
    {
        AnsiConsole.WriteLine();

        var panel = new Panel(
            new Markup(
                $"[yellow]A new version is available![/]\n\n" +
                $"Current version: [red]{current.ToString(3)}[/]\n" +
                $"Latest version:  [green]{latest.ToString(3)}[/]\n\n" +
                $"[dim]Run the install script to update:[/]\n" +
                $"[blue]curl -fsSL https://raw.githubusercontent.com/{GitHubRepo}/main/install.sh | bash[/]\n\n" +
                $"[dim]Or download from:[/]\n" +
                $"[link={release.HtmlUrl}]{release.HtmlUrl}[/]"
            ))
        {
            Header = new PanelHeader("[bold yellow] Update Available [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(Align.Center(panel));
        AnsiConsole.WriteLine();

        AnsiConsole.Write(Align.Center(new Markup("[dim]Press any key to continue...[/]")));
        AnsiConsole.WriteLine();
        Console.ReadKey(true);
    }

    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }
    }
}
