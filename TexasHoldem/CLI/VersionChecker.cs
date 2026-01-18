using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace TexasHoldem.CLI;

public class VersionChecker
{
    private const string GitHubRepo = "tiemenrtuinstra/poker-cli";
    private const string GitHubApiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    static VersionChecker()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "poker-cli");
        HttpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    /// <summary>
    /// Run the --update command: check for updates and optionally install
    /// </summary>
    public static async Task<int> RunUpdateCommandAsync(string? targetVersion = null)
    {
        var currentVersion = GetCurrentVersion();

        // Show the Texas Hold'em header (without feature table for version manager)
        HeaderDisplay.ShowHeader("[bold yellow]♠ ♥ ♦ ♣[/]  [italic]Version Manager[/]  [bold yellow]♣ ♦ ♥ ♠[/]", showVersion: true, showFeatureTable: false);

        List<GitHubRelease>? releases = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[cyan]Fetching available versions from GitHub...[/]", async ctx =>
            {
                releases = await GetAllReleasesAsync();
            });

        if (releases == null || releases.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Failed to fetch releases. Please check your internet connection.[/]");
            return 1;
        }

        // If a specific version was requested via command line
        if (!string.IsNullOrEmpty(targetVersion))
        {
            var normalizedTarget = targetVersion.TrimStart('v', 'V');
            var targetRelease = releases.FirstOrDefault(r =>
                r.TagName?.TrimStart('v', 'V') == normalizedTarget);

            if (targetRelease == null)
            {
                AnsiConsole.MarkupLine($"[red]Version {targetVersion} not found.[/]");
                AnsiConsole.MarkupLine("[dim]Available versions:[/]");
                foreach (var r in releases.Take(10))
                {
                    AnsiConsole.MarkupLine($"  [cyan]{r.TagName}[/]");
                }
                return 1;
            }

            return await PerformUpdateAsync(targetRelease);
        }

        // Show combined version table with changelogs
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[bold cyan]Available Versions[/]")
            .AddColumn(new TableColumn("[bold]Version[/]").Centered().Width(9))
            .AddColumn(new TableColumn("[bold]Status[/]").Centered().Width(11))
            .AddColumn(new TableColumn("[bold]Released[/]").Centered().Width(18))
            .AddColumn(new TableColumn("[bold]Changelog[/]").LeftAligned());

        foreach (var release in releases.Take(15))
        {
            var releaseVersion = ParseVersion(release.TagName ?? "0.0.0");
            var status = releaseVersion == currentVersion
                ? "[green]● Current[/]"
                : releaseVersion > currentVersion
                    ? "[yellow]↑ Upgrade[/]"
                    : "[dim]↓ Downgrade[/]";

            var releasedDateTime = release.PublishedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "Unknown";
            var versionDisplay = releaseVersion == currentVersion
                ? $"[green]{release.TagName}[/]"
                : $"[cyan]{release.TagName}[/]";

            // Get changelog from release name or body (truncate to fit column)
            var changelog = !string.IsNullOrEmpty(release.Name) && release.Name != release.TagName
                ? release.Name
                : release.Body?.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim() ?? "";

            // Remove markdown formatting from changelog
            changelog = System.Text.RegularExpressions.Regex.Replace(changelog, @"[#*_`\[\]]", "").Trim();

            if (changelog.Length > 35)
            {
                changelog = changelog[..32] + "...";
            }
            var changelogDisplay = string.IsNullOrEmpty(changelog)
                ? "[dim]-[/]"
                : $"[dim]{Markup.Escape(changelog)}[/]";

            table.AddRow(versionDisplay, status, $"[dim]{releasedDateTime}[/]", changelogDisplay);
        }

        AnsiConsole.Write(table);

        // Show compact footer with version and repo info
        var repoOwner = GitHubRepo.Split('/')[0];
        AnsiConsole.MarkupLine($"[dim]Your current version:[/] [green]{currentVersion.ToString(3)}[/]");
        AnsiConsole.MarkupLine($"[dim]GitHub:[/] [link=https://github.com/{GitHubRepo}]https://github.com/{GitHubRepo}[/]  [cyan]👤 @{repoOwner}[/]");
        AnsiConsole.WriteLine();

        // Ask what the user wants to do
        var choices = new List<string> { "Select a version to install", "Cancel" };
        var latestVersion = ParseVersion(releases[0].TagName ?? "0.0.0");

        if (latestVersion > currentVersion)
        {
            choices.Insert(0, $"⚡ Quick upgrade to latest ({releases[0].TagName})");
        }

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]What would you like to do?[/]")
                .PageSize(5)
                .HighlightStyle(new Style(Color.Black, Color.Green))
                .AddChoices(choices));

        if (action == "Cancel")
        {
            AnsiConsole.MarkupLine("[dim]Operation cancelled.[/]");
            return 0;
        }

        GitHubRelease? selectedRelease;

        if (action.StartsWith("⚡"))
        {
            selectedRelease = releases[0];
        }
        else
        {
            // Let user select a version
            var versionChoices = releases
                .Take(15)
                .Select(r =>
                {
                    var v = ParseVersion(r.TagName ?? "0.0.0");
                    var indicator = v == currentVersion ? " [green](current)[/]"
                        : v > currentVersion ? " [yellow](upgrade)[/]"
                        : " [dim](downgrade)[/]";
                    return r.TagName + indicator;
                })
                .ToList();

            var selectedVersion = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]Select a version to install:[/]")
                    .PageSize(10)
                    .HighlightStyle(new Style(Color.Black, Color.Cyan1))
                    .AddChoices(versionChoices));

            // Extract version tag from selection
            var selectedTag = selectedVersion.Split(' ')[0];
            selectedRelease = releases.FirstOrDefault(r => r.TagName == selectedTag);
        }

        if (selectedRelease == null)
        {
            AnsiConsole.MarkupLine("[red]Failed to find selected release.[/]");
            return 1;
        }

        var selectedVer = ParseVersion(selectedRelease.TagName ?? "0.0.0");

        if (selectedVer == currentVersion)
        {
            AnsiConsole.MarkupLine("[yellow]You already have this version installed.[/]");

            if (!AnsiConsole.Confirm("Do you want to reinstall it anyway?", false))
            {
                return 0;
            }
        }

        // Confirm the action
        var actionType = selectedVer > currentVersion ? "upgrade" : selectedVer < currentVersion ? "downgrade" : "reinstall";
        var confirmMessage = $"Do you want to [bold]{actionType}[/] from [cyan]{currentVersion.ToString(3)}[/] to [cyan]{selectedVer.ToString(3)}[/]?";

        if (!AnsiConsole.Confirm(confirmMessage))
        {
            AnsiConsole.MarkupLine("[dim]Operation cancelled.[/]");
            return 0;
        }

        return await PerformUpdateAsync(selectedRelease);
    }

    /// <summary>
    /// Get all releases from GitHub
    /// </summary>
    private static async Task<List<GitHubRelease>?> GetAllReleasesAsync()
    {
        try
        {
            var response = await HttpClient.GetAsync($"https://api.github.com/repos/{GitHubRepo}/releases");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<List<GitHubRelease>>();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Perform the actual update by downloading and replacing the executable
    /// </summary>
    private static async Task<int> PerformUpdateAsync(GitHubRelease release)
    {
        var artifactName = GetArtifactNameForPlatform();
        if (artifactName == null)
        {
            AnsiConsole.MarkupLine("[red]Unsupported platform for automatic updates.[/]");
            AnsiConsole.MarkupLine($"[dim]Please download manually from:[/] [link={release.HtmlUrl}]{release.HtmlUrl}[/]");
            return 1;
        }

        var downloadUrl = $"https://github.com/{GitHubRepo}/releases/download/{release.TagName}/{artifactName}";
        var currentExePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;

        if (string.IsNullOrEmpty(currentExePath))
        {
            AnsiConsole.MarkupLine("[red]Could not determine current executable path.[/]");
            return 1;
        }

        var tempPath = Path.Combine(Path.GetTempPath(), artifactName);
        var backupPath = currentExePath + ".backup";

        try
        {
            // Download new version
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var downloadTask = ctx.AddTask("[green]Downloading update...[/]");

                    using var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1;
                    await using var contentStream = await response.Content.ReadAsStreamAsync();
                    await using var fileStream = File.Create(tempPath);

                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        totalRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            downloadTask.Value = (double)totalRead / totalBytes * 100;
                        }
                    }

                    downloadTask.Value = 100;
                });

            AnsiConsole.MarkupLine("[green]Download complete![/]");

            // Create backup of current executable
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            // On Windows, we can't replace a running executable directly
            // We need to use a different approach
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await PerformWindowsUpdateAsync(currentExePath, tempPath, backupPath);
            }
            else
            {
                // On Unix-like systems, we can replace the file
                File.Move(currentExePath, backupPath);
                File.Move(tempPath, currentExePath);

                // Make executable
                var chmod = Process.Start(new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{currentExePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                chmod?.WaitForExit();

                // Clean up backup
                File.Delete(backupPath);

                AnsiConsole.MarkupLine("[green]Update successful![/]");
                AnsiConsole.MarkupLine("[dim]Please restart poker-cli to use the new version.[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Update failed: {ex.Message}[/]");

            // Restore backup if it exists
            if (File.Exists(backupPath) && !File.Exists(currentExePath))
            {
                File.Move(backupPath, currentExePath);
                AnsiConsole.MarkupLine("[yellow]Restored previous version.[/]");
            }

            // Clean up temp file
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            return 1;
        }
    }

    /// <summary>
    /// Windows-specific update that creates a batch script to replace the exe after exit
    /// </summary>
    private static async Task PerformWindowsUpdateAsync(string currentExePath, string tempPath, string backupPath)
    {
        var batchPath = Path.Combine(Path.GetTempPath(), "poker-cli-update.bat");
        var currentPid = Environment.ProcessId;
        // ANSI escape codes for colors (Windows 10+)
        var batchContent = $"""
            @echo off
            chcp 65001 >nul 2>&1
            title Poker CLI Updater

            :: Enable ANSI escape codes
            for /F "tokens=3" %%A in ('reg query "HKCU\Console" /v VirtualTerminalLevel 2^>nul') do set "VT=%%A"
            if not defined VT reg add "HKCU\Console" /v VirtualTerminalLevel /t REG_DWORD /d 1 /f >nul 2>&1

            :: ANSI color codes - create ESC character
            for /F %%a in ('echo prompt $E ^| cmd') do set "ESC=%%a"
            set "GREEN=%ESC%[92m"
            set "YELLOW=%ESC%[93m"
            set "RED=%ESC%[91m"
            set "CYAN=%ESC%[96m"
            set "DIM=%ESC%[90m"
            set "RESET=%ESC%[0m"
            set "BOLD=%ESC%[1m"

            echo.
            echo %CYAN%╔════════════════════════════════════════════╗%RESET%
            echo %CYAN%║%RESET%  %BOLD%%YELLOW%♠ ♥%RESET% %BOLD%Poker CLI - Auto Updater%RESET% %BOLD%%YELLOW%♦ ♣%RESET%  %CYAN%║%RESET%
            echo %CYAN%╚════════════════════════════════════════════╝%RESET%
            echo.
            echo %DIM%Waiting for poker-cli to exit...%RESET%

            :waitloop
            tasklist /FI "PID eq {currentPid}" 2>NUL | find /I "{currentPid}" >NUL
            if %ERRORLEVEL%==0 (
                ping -n 2 127.0.0.1 >nul
                goto waitloop
            )

            echo %GREEN%✓%RESET% Application closed. Starting update...
            echo.

            echo %YELLOW%[1/3]%RESET% Backing up current version...
            if exist "{backupPath}" del /f /q "{backupPath}"
            move /y "{currentExePath}" "{backupPath}" >nul 2>&1
            if %ERRORLEVEL% NEQ 0 (
                echo %RED%✗ ERROR: Failed to backup current version.%RESET%
                echo %DIM%Press any key to exit...%RESET%
                pause >nul
                exit /b 1
            )
            echo       %GREEN%✓ Done%RESET%

            echo %YELLOW%[2/3]%RESET% Installing new version...
            move /y "{tempPath}" "{currentExePath}" >nul 2>&1
            if %ERRORLEVEL% NEQ 0 (
                echo %RED%✗ ERROR: Failed to install new version.%RESET%
                echo %YELLOW%Restoring backup...%RESET%
                move /y "{backupPath}" "{currentExePath}" >nul 2>&1
                echo %DIM%Press any key to exit...%RESET%
                pause >nul
                exit /b 1
            )
            echo       %GREEN%✓ Done%RESET%

            echo %YELLOW%[3/3]%RESET% Cleaning up...
            if exist "{backupPath}" del /f /q "{backupPath}"
            if exist "{tempPath}" del /f /q "{tempPath}"
            echo       %GREEN%✓ Done%RESET%

            echo.
            echo %GREEN%╔════════════════════════════════════════════╗%RESET%
            echo %GREEN%║%RESET%  %BOLD%%GREEN%✓ Update complete!%RESET%                       %GREEN%║%RESET%
            echo %GREEN%║%RESET%  %DIM%You can now run poker-cli again.%RESET%        %GREEN%║%RESET%
            echo %GREEN%╚════════════════════════════════════════════╝%RESET%
            echo.
            echo %DIM%This window will close in 3 seconds...%RESET%
            ping -n 4 127.0.0.1 >nul
            del "%~f0"
            """;

        await File.WriteAllTextAsync(batchPath, batchContent);

        AnsiConsole.MarkupLine("[yellow]Starting update process...[/]");
        AnsiConsole.MarkupLine("[dim]The application will close and update automatically.[/]");
        AnsiConsole.WriteLine();

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{batchPath}\"",
            UseShellExecute = true,
            CreateNoWindow = false
        });

        // Exit the application so the batch file can replace the exe
        Environment.Exit(0);
    }

    /// <summary>
    /// Get the correct artifact name for the current platform
    /// </summary>
    private static string? GetArtifactNameForPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "poker-cli-windows-x64.exe";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.OSArchitecture == Architecture.Arm64
                ? "poker-cli-linux-arm64"
                : "poker-cli-linux-x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.OSArchitecture == Architecture.Arm64
                ? "poker-cli-macos-arm64"
                : "poker-cli-macos-x64";
        }

        return null;
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
        // Normalize to 3 components (Major.Minor.Build) for consistent comparison
        return version != null
            ? new Version(version.Major, version.Minor, version.Build)
            : new Version(1, 0, 0);
    }

    private static Version ParseVersion(string tagName)
    {
        var versionString = tagName.TrimStart('v', 'V');
        if (Version.TryParse(versionString, out var version))
        {
            // Normalize to 3 components for consistent comparison
            return new Version(version.Major, version.Minor, Math.Max(0, version.Build));
        }
        return new Version(0, 0, 0);
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

        // OS-specific install instructions
        string installInstructions;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            installInstructions =
                $"[dim]Update using the version manager:[/]\n" +
                $"[blue]poker-cli --update[/]\n\n" +
                $"[dim]Or run the install script in PowerShell:[/]\n" +
                $"[blue]irm https://raw.githubusercontent.com/{GitHubRepo}/main/install.ps1 | iex[/]";
        }
        else
        {
            installInstructions =
                $"[dim]Update using the version manager:[/]\n" +
                $"[blue]poker-cli --update[/]\n\n" +
                $"[dim]Or run the install script:[/]\n" +
                $"[blue]curl -fsSL https://raw.githubusercontent.com/{GitHubRepo}/main/install.sh | bash[/]";
        }

        var panel = new Panel(
            new Markup(
                $"[yellow]A new version is available![/]\n\n" +
                $"Current version: [red]{current.ToString(3)}[/]\n" +
                $"Latest version:  [green]{latest.ToString(3)}[/]\n\n" +
                installInstructions + "\n\n" +
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

        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }

}
