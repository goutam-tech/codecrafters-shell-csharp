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
}