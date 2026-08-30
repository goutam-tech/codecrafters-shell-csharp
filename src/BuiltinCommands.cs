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

    public static bool IsBuiltin(string command)
    {
        return Commands.Contains(command);
    }

    public static void HandleComplete(List<string> arguments)
    {
        if (arguments.Count >= 3 && arguments[1] == "-p")
        {
            string command = arguments[2];

            Console.WriteLine($"complete: {command}: no completion specification");
        }
    }
}