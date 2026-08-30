using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;

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

        if (arguments.Count >= 3 && arguments[1] == "-r")
        {
            string command = arguments[2];

            CompleteSpecs.Remove(command);

            return;
        }
    }

    public static bool HasCompleter(string command)
    {
        return CompleteSpecs.ContainsKey(command);
    }

    public static List<string>? RunCompleter(string command,
        string currentWord, string previousWord, string compLine)
    {
        if (!CompleteSpecs.TryGetValue(command, out string? scriptPath))
        {
            return null;
        }

        try
        {
            var process = new Process();

            process.StartInfo.FileName = scriptPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.ArgumentList.Add(command);

            process.StartInfo.ArgumentList.Add(currentWord);

            process.StartInfo.ArgumentList.Add(previousWord);

            process.StartInfo.Environment["COMP_LINE"] = compLine;

            process.StartInfo.Environment["COMP_POINT"] = Encoding.UTF8.GetByteCount(compLine).ToString();

            process.Start();

            string output = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return new List<string>();
            }

            return output
                .Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries
                )
                .Select(candidate => candidate.Trim())
                .Where(candidate => candidate.Length > 0)
                .OrderBy(candidate => candidate, StringComparer.Ordinal)
                .ToList();
        }
        catch
        {
            return null;
        }
    }
}