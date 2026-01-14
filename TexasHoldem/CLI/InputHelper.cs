namespace TexasHoldem.CLI;

public class InputHelper
{
    public int GetIntegerInput(string prompt, int min = int.MinValue, int max = int.MaxValue, int defaultValue = 0)
    {
        while (true)
        {
            Console.Write($"{prompt} (default {defaultValue}): ");
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input))
            {
                if (defaultValue >= min && defaultValue <= max)
                    return defaultValue;
                Console.WriteLine($"❌ Default value {defaultValue} is outside valid range [{min}-{max}]");
                continue;
            }
            
            if (int.TryParse(input, out int result))
            {
                if (result >= min && result <= max)
                {
                    return result;
                }
                Console.WriteLine($"❌ Value must be between {min} and {max}");
            }
            else
            {
                Console.WriteLine("❌ Please enter a valid number");
            }
        }
    }

    public double GetDoubleInput(string prompt, double min = double.MinValue, double max = double.MaxValue, double defaultValue = 0.0)
    {
        while (true)
        {
            Console.Write($"{prompt} (default {defaultValue:F2}): ");
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input))
            {
                if (defaultValue >= min && defaultValue <= max)
                    return defaultValue;
                Console.WriteLine($"❌ Default value {defaultValue} is outside valid range [{min:F2}-{max:F2}]");
                continue;
            }
            
            if (double.TryParse(input, out double result))
            {
                if (result >= min && result <= max)
                {
                    return result;
                }
                Console.WriteLine($"❌ Value must be between {min:F2} and {max:F2}");
            }
            else
            {
                Console.WriteLine("❌ Please enter a valid number");
            }
        }
    }

    public bool GetBooleanInput(string prompt, bool defaultValue = false)
    {
        while (true)
        {
            var defaultText = defaultValue ? "Y/n" : "y/N";
            Console.Write($"{prompt} ({defaultText}): ");
            var input = Console.ReadLine()?.Trim().ToLower();
            
            if (string.IsNullOrEmpty(input))
            {
                return defaultValue;
            }
            
            if (input == "y" || input == "yes" || input == "true")
            {
                return true;
            }
            
            if (input == "n" || input == "no" || input == "false")
            {
                return false;
            }
            
            Console.WriteLine("❌ Please enter 'y' for yes or 'n' for no");
        }
    }

    public string GetStringInput(string prompt, string defaultValue = "", bool allowEmpty = true)
    {
        while (true)
        {
            var defaultText = string.IsNullOrEmpty(defaultValue) ? "" : $" (default: {defaultValue})";
            Console.Write($"{prompt}{defaultText}: ");
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input))
            {
                if (allowEmpty || !string.IsNullOrEmpty(defaultValue))
                {
                    return defaultValue;
                }
                Console.WriteLine("❌ This field cannot be empty");
                continue;
            }
            
            return input;
        }
    }

    public T GetChoiceInput<T>(string prompt, Dictionary<string, T> choices, T defaultChoice = default!)
    {
        while (true)
        {
            Console.WriteLine(prompt);
            
            var choiceList = choices.ToList();
            for (int i = 0; i < choiceList.Count; i++)
            {
                var isDefault = EqualityComparer<T>.Default.Equals(choiceList[i].Value, defaultChoice);
                var defaultMarker = isDefault ? " (default)" : "";
                Console.WriteLine($"  {i + 1}. {choiceList[i].Key}{defaultMarker}");
            }
            
            Console.Write("Enter your choice (number): ");
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input) && !EqualityComparer<T>.Default.Equals(defaultChoice, default(T)))
            {
                return defaultChoice;
            }
            
            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= choices.Count)
            {
                return choiceList[choice - 1].Value;
            }
            
            Console.WriteLine("❌ Invalid choice. Please enter a number from the list.");
        }
    }

    public List<string> GetPlayerNames(int count)
    {
        var names = new List<string>();
        
        for (int i = 1; i <= count; i++)
        {
            var defaultName = $"Player {i}";
            var name = GetStringInput($"Enter name for player {i}", defaultName);
            
            // Ensure unique names
            while (names.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine("❌ That name is already taken");
                name = GetStringInput($"Enter a different name for player {i}", $"{defaultName}_{i}");
            }
            
            names.Add(name);
        }
        
        return names;
    }

    public void PressAnyKeyToContinue(string message = "Press any key to continue...")
    {
        Console.WriteLine(message);
        Console.ReadKey(true);
    }

    public void ClearScreen()
    {
        try
        {
            Console.Clear();
        }
        catch
        {
            // If clear fails, just add some newlines
            Console.WriteLine(new string('\n', 10));
        }
    }

    public void ShowError(string message)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Error: {message}");
        Console.ForegroundColor = oldColor;
    }

    public void ShowSuccess(string message)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ {message}");
        Console.ForegroundColor = oldColor;
    }

    public void ShowInfo(string message)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"ℹ️  {message}");
        Console.ForegroundColor = oldColor;
    }

    public void ShowWarning(string message)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠️  {message}");
        Console.ForegroundColor = oldColor;
    }

    public void WriteColorText(string text, ConsoleColor color)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = oldColor;
    }

    public void WriteLineColorText(string text, ConsoleColor color)
    {
        WriteColorText(text, color);
        Console.WriteLine();
    }
}