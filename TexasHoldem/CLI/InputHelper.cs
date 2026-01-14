using Spectre.Console;

namespace TexasHoldem.CLI;

public class InputHelper
{
    public int GetIntegerInput(string prompt, int min = int.MinValue, int max = int.MaxValue, int defaultValue = 0)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<int>($"[yellow]{prompt}[/] [dim](default {defaultValue})[/]:")
                .DefaultValue(defaultValue)
                .Validate(value =>
                {
                    if (value < min || value > max)
                        return ValidationResult.Error($"[red]Value must be between {min} and {max}[/]");
                    return ValidationResult.Success();
                }));
    }

    public double GetDoubleInput(string prompt, double min = double.MinValue, double max = double.MaxValue, double defaultValue = 0.0)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<double>($"[yellow]{prompt}[/] [dim](default {defaultValue:F2})[/]:")
                .DefaultValue(defaultValue)
                .Validate(value =>
                {
                    if (value < min || value > max)
                        return ValidationResult.Error($"[red]Value must be between {min:F2} and {max:F2}[/]");
                    return ValidationResult.Success();
                }));
    }

    public bool GetBooleanInput(string prompt, bool defaultValue = false)
    {
        return AnsiConsole.Confirm($"[yellow]{prompt}[/]", defaultValue);
    }

    public string GetStringInput(string prompt, string defaultValue = "", bool allowEmpty = true)
    {
        var textPrompt = new TextPrompt<string>($"[yellow]{prompt}[/]:")
            .AllowEmpty();

        if (!string.IsNullOrEmpty(defaultValue))
        {
            textPrompt.DefaultValue(defaultValue);
        }

        if (!allowEmpty && string.IsNullOrEmpty(defaultValue))
        {
            textPrompt.Validate(value =>
            {
                if (string.IsNullOrWhiteSpace(value))
                    return ValidationResult.Error("[red]This field cannot be empty[/]");
                return ValidationResult.Success();
            });
        }

        return AnsiConsole.Prompt(textPrompt);
    }

    public T GetChoiceInput<T>(string prompt, Dictionary<string, T> choices, T defaultChoice = default!)
    {
        var choiceList = choices.ToList();
        var defaultIndex = 0;

        // Find default choice index
        for (int i = 0; i < choiceList.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(choiceList[i].Value, defaultChoice))
            {
                defaultIndex = i;
                break;
            }
        }

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold cyan]{prompt}[/]")
                .PageSize(10)
                .HighlightStyle(new Style(Color.Black, Color.Cyan1))
                .AddChoices(choiceList.Select(c => c.Key)));

        return choices[selection];
    }

    public List<string> GetPlayerNames(int count)
    {
        var names = new List<string>();

        AnsiConsole.Write(new Rule("[bold cyan]Enter Player Names[/]").RuleStyle("cyan"));

        for (int i = 1; i <= count; i++)
        {
            var defaultName = $"Player {i}";

            while (true)
            {
                var name = AnsiConsole.Prompt(
                    new TextPrompt<string>($"[yellow]Name for player {i}[/]:")
                        .DefaultValue(defaultName));

                if (names.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine("[red]That name is already taken. Please choose another.[/]");
                    continue;
                }

                names.Add(name);
                break;
            }
        }

        return names;
    }

    public void PressAnyKeyToContinue(string message = "Press any key to continue...")
    {
        AnsiConsole.MarkupLine($"[dim]{message}[/]");
        Console.ReadKey(true);
    }

    public void ClearScreen()
    {
        AnsiConsole.Clear();
    }

    public void ShowError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(message)}[/]");
    }

    public void ShowSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]{Markup.Escape(message)}[/]");
    }

    public void ShowInfo(string message)
    {
        AnsiConsole.MarkupLine($"[cyan]{Markup.Escape(message)}[/]");
    }

    public void ShowWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]Warning: {Markup.Escape(message)}[/]");
    }

    public void WriteColorText(string text, ConsoleColor color)
    {
        var spectreColor = ConvertConsoleColor(color);
        AnsiConsole.Markup($"[{spectreColor}]{Markup.Escape(text)}[/]");
    }

    public void WriteLineColorText(string text, ConsoleColor color)
    {
        var spectreColor = ConvertConsoleColor(color);
        AnsiConsole.MarkupLine($"[{spectreColor}]{Markup.Escape(text)}[/]");
    }

    private static string ConvertConsoleColor(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "black",
            ConsoleColor.DarkBlue => "navy",
            ConsoleColor.DarkGreen => "green",
            ConsoleColor.DarkCyan => "teal",
            ConsoleColor.DarkRed => "maroon",
            ConsoleColor.DarkMagenta => "purple",
            ConsoleColor.DarkYellow => "olive",
            ConsoleColor.Gray => "silver",
            ConsoleColor.DarkGray => "grey",
            ConsoleColor.Blue => "blue",
            ConsoleColor.Green => "lime",
            ConsoleColor.Cyan => "aqua",
            ConsoleColor.Red => "red",
            ConsoleColor.Magenta => "fuchsia",
            ConsoleColor.Yellow => "yellow",
            ConsoleColor.White => "white",
            _ => "white"
        };
    }
}
