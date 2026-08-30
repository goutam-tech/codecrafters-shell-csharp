using System;
using System.Collections.Generic;

public static class BuiltinCommands
{
    public static readonly HashSet<string> Commands = new()
    {
        "echo",
        "exit",
        "pwd",
        "cd",
        "type",
        "complete"
    };

    public static readonly Dictionary<string, string> CompleteSpecs = new();

    public static bool IsBuiltin(string command)
    {
        return Commands.Contains(command);
    }

    public static void HandleComplete(List<string> arguments)
    {
        if (arguments.Count >= 3 && arguments[1] == "-p")
        {
            string command = arguments[2];

            if (CompleteSpecs.TryGetValue(command, out string? scriptPath))
            {
                Console.WriteLine($"complete -C '{scriptPath}' {command}");
            }

            else
            {
                Console.WriteLine($"complete: {command}: no completion specification");
            }
            return;
        }
        if (arguments.Count >= 4 && arguments[1] == "-C")
        {
            string scriptPath = arguments[2];
            string command = arguments[3];

            CompleteSpecs[command] = scriptPath;

            return;
        }
    }
}