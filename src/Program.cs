using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static bool tabPressed = false;
    private static string? completerTabCommand;
    private static bool completerSecondTab;
    static void Main()
    {
        while (true)
        {
            string input = ReadInput();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            List<string> parts = ParseCommand(input);

            if (parts.Count == 0)
            {
                continue;
            }

            var (arguments, outputFile, errorFile, outputAppend, errorAppend) = ParseRedirection(parts);

            string command = arguments[0];

            if (command == "exit")
            {
                break;
            }

            else if (command == "echo")
            {
                string output = string.Join(" ", arguments.Skip(1));

                if (errorFile != null)
                {
                    if (errorAppend)
                    {
                        if (!File.Exists(errorFile))
                        {
                            File.WriteAllText(errorFile, "");
                        }
                    }
                    else
                    {
                        File.WriteAllText(errorFile, "");
                    }
                }

                if (outputFile != null)
                {
                    if (outputAppend)
                    {
                        File.AppendAllText(outputFile, output + Environment.NewLine);
                    }
                    else
                    {
                        File.WriteAllText(outputFile, output + Environment.NewLine);
                    }
                }
                else
                {
                    Console.WriteLine(output);
                }
            }

            else if (command == "pwd")
            {
                Console.WriteLine(Environment.CurrentDirectory);
            }

            else if (command == "cd")
            {
                if (parts.Count < 2)
                {
                    Console.WriteLine($"cd: missing argument");
                    continue;
                }
                HandleCd(parts[1]);
            }

            else if (command == "type")
            {
                if (parts.Count < 2)
                {
                    Console.WriteLine("type: missing argument");
                    continue;
                }

                HandleType(parts[1]);
            }

            else if (command == "complete")
            {
                BuiltinCommands.HandleComplete(arguments);
            }

            else
            {
                ExecuteExternalCommand(arguments, outputFile, errorFile, outputAppend, errorAppend);
            }
        }
    }

    static void HandleType(string command)
    {
        if (BuiltinCommands.IsBuiltin(command))
        {
            Console.WriteLine($"{command} is a shell builtin");
            return;
        }

        string? executable = FindExecutable(command);

        if (executable != null)
        {
            Console.WriteLine($"{command} is {executable}");
        }
        else
        {
            Console.WriteLine($"{command}: not found");
        }
    }

    static void ExecuteExternalCommand(List<string> parts, string? outputFile, string? errorFile, bool outputAppend, bool errorAppend)
    {
        string command = parts[0];

        string? executable = FindExecutable(command);

        if (executable == null)
        {
            Console.WriteLine($"{command}: command not found");
            return;
        }

        var process = new Process();

        process.StartInfo.FileName = "/bin/bash";
        process.StartInfo.UseShellExecute = false;

        process.StartInfo.ArgumentList.Add("-c");

        process.StartInfo.ArgumentList.Add(
            "exec -a \"$0\" \"$1\" \"${@:2}\""
        );

        process.StartInfo.ArgumentList.Add(command);
        process.StartInfo.ArgumentList.Add(executable);

        for (int i = 1; i < parts.Count; i++)
        {
            process.StartInfo.ArgumentList.Add(parts[i]);
        }

        if (outputFile != null)
        {
            process.StartInfo.RedirectStandardOutput = true;
        }

        if (errorFile != null)
        {
            process.StartInfo.RedirectStandardError = true;
        }

        process.Start();

        string? stdout = outputFile != null ? process.StandardOutput.ReadToEnd() : null;
        string? stderr = errorFile != null ? process.StandardError.ReadToEnd() : null;

        if (outputFile != null)
        {
            if (outputAppend)
            {
                File.AppendAllText(outputFile, stdout);
            }
            else
            {
                File.WriteAllText(outputFile, stdout);
            }
        }

        if (errorFile != null)
        {
            if (errorAppend)
            {
                File.AppendAllText(errorFile, stderr);
            }
            else
            {
                File.WriteAllText(errorFile, stderr);
            }
        }

        process.WaitForExit();
    }

    static string? FindExecutable(string command)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        string[] directories = path.Split(
            Path.PathSeparator,
            StringSplitOptions.RemoveEmptyEntries
        );

        foreach (string directory in directories)
        {
            string fullPath = Path.Combine(directory, command);

            if (!File.Exists(fullPath))
            {
                continue;
            }

            if (!IsExecutable(fullPath))
            {
                continue;
            }

            return fullPath;
        }

        return null;
    }

    static bool IsExecutable(string filePath)
    {
        if (!OperatingSystem.IsLinux() &&
            !OperatingSystem.IsMacOS())
        {
            return true;
        }

        UnixFileMode mode = File.GetUnixFileMode(filePath);

        return mode.HasFlag(UnixFileMode.UserExecute) ||
               mode.HasFlag(UnixFileMode.GroupExecute) ||
               mode.HasFlag(UnixFileMode.OtherExecute);
    }

    static void HandleCd(string path)
    {
        if (path == "~")
        {
            string? home = Environment.GetEnvironmentVariable("HOME");

            if (home != null)
            {
                path = home;
            }
        }

        if (Directory.Exists(path))
        {
            Environment.CurrentDirectory = path;
        }
        else
        {
            Console.WriteLine($"cd: {path}: No such file or directory");
        }
    }

    static List<string> ParseCommand(string input)
    {
        var args = new List<string>();
        var current = new StringBuilder();

        bool insideSingleQuotes = false;
        bool insideDoubleQuotes = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '\\')
            {
                if (insideSingleQuotes)
                {
                    current.Append(c);
                }
                else if (insideDoubleQuotes)
                {
                    if (i + 1 < input.Length)
                    {
                        char next = input[i + 1];

                        if (next == '\\' ||
                            next == '"' ||
                            next == '$' ||
                            next == '`')
                        {
                            i++;
                            current.Append(next);
                        }
                        else
                        {
                            current.Append(c);
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (i + 1 < input.Length)
                    {
                        i++;
                        current.Append(input[i]);
                    }
                }

                continue;
            }

            if (c == '\'' && !insideDoubleQuotes)
            {
                insideSingleQuotes = !insideSingleQuotes;
                continue;
            }

            if (c == '"' && !insideSingleQuotes)
            {
                insideDoubleQuotes = !insideDoubleQuotes;
                continue;
            }

            if (char.IsWhiteSpace(c) &&
                !insideSingleQuotes &&
                !insideDoubleQuotes)
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
        {
            args.Add(current.ToString());
        }

        return args;
    }

    static (List<string> arguments, string? outputFile, string? errorFile, bool outputAppend, bool errorAppend) ParseRedirection(List<string> parts)
    {
        var arguments = new List<string>();
        string? outputFile = null;
        string? errorFile = null;
        bool outputAppend = false;
        bool errorAppend = false;

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] == ">" || parts[i] == "1>")
            {
                if (i + 1 < parts.Count)
                {
                    outputFile = parts[i + 1];
                    outputAppend = false;
                    i++;
                }

                continue;
            }

            if (parts[i] == ">>" || parts[i] == "1>>")
            {
                if (i + 1 < parts.Count)
                {
                    outputFile = parts[i + 1];
                    outputAppend = true;
                    i++;
                }

                continue;
            }

            if (parts[i] == "2>")
            {
                if (i + 1 < parts.Count)
                {
                    errorFile = parts[i + 1];
                    errorAppend = false;
                    i++;
                }

                continue;
            }

            if (parts[i] == "2>>")
            {
                if (i + 1 < parts.Count)
                {
                    errorFile = parts[i + 1];
                    errorAppend = true;
                    i++;
                }

                continue;
            }

            arguments.Add(parts[i]);
        }

        return (arguments, outputFile, errorFile, outputAppend, errorAppend);
    }

    static string ReadInput()
    {
        Console.Write("$ ");

        var input = new StringBuilder();

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if(key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                tabPressed = false;
                return input.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (input.Length > 0)
                {
                    input.Remove(input.Length - 1, 1);
                    Console.Write("\b \b");
                }

                ResetCompleterTabState();
                continue;
            }

            if (key.Key == ConsoleKey.Tab)
            {
                string current = input.ToString();

                if (current.Contains(' '))
                {
                    List<string> words = current
                        .Split(
                            ' ',
                            StringSplitOptions.RemoveEmptyEntries
                        )
                        .ToList();

                    if (words.Count > 0)
                    {
                        string command = words[0];

                        if (BuiltinCommands.HasCompleter(command))
                        {
                            int lastSpace = current.LastIndexOf(' ');

                            string currentWord =
                                current[(lastSpace + 1)..];

                            string previousWord = "";

                            if (lastSpace > 0)
                            {
                                string beforeCurrent =
                                    current[..lastSpace].TrimEnd();

                                int previousSpace =
                                    beforeCurrent.LastIndexOf(' ');

                                previousWord =
                                    previousSpace == -1
                                        ? beforeCurrent
                                        : beforeCurrent[(previousSpace + 1)..];
                            }

                            List<string>? candidates =
                                BuiltinCommands.RunCompleter(
                                    command,
                                    currentWord,
                                    previousWord,
                                    current
                                );

                            if (candidates == null ||
                                candidates.Count == 0)
                            {
                                Console.Write('\x07');

                                ResetCompleterTabState();

                                continue;
                            }

                            if (candidates.Count == 1)
                            {
                                string candidate = candidates[0];

                                string suffix;

                                if (candidate.StartsWith(
                                        currentWord,
                                        StringComparison.Ordinal))
                                {
                                    suffix = candidate[currentWord.Length..];
                                }
                                else
                                {
                                    suffix = candidate;
                                }

                                for (int i = 0; i < currentWord.Length; i++)
                                {
                                    Console.Write("\b \b");
                                }

                                input.Remove(
                                    lastSpace + 1,
                                    currentWord.Length
                                );

                                Console.Write(candidate);
                                Console.Write(' ');

                                input.Append(candidate);
                                input.Append(' ');

                                ResetCompleterTabState();

                                continue;
                            }

                            string commonPrefix =
                                FindLongestCommonPrefix(candidates);

                            if (commonPrefix.Length > currentWord.Length)
                            {
                                string completion =
                                    commonPrefix[currentWord.Length..];

                                Console.Write(completion);

                                input.Append(completion);

                                ResetCompleterTabState();

                                continue;
                            }

                            if (completerTabCommand != command)
                            {
                                completerTabCommand = command;
                                completerSecondTab = false;
                            }

                            if (!completerSecondTab)
                            {
                                Console.Write('\x07');

                                completerSecondTab = true;

                                continue;
                            }

                            Console.WriteLine();

                            Console.WriteLine(
                                string.Join(
                                    "  ",
                                    candidates
                                )
                            );

                            Console.Write("$ ");
                            Console.Write(input.ToString());

                            ResetCompleterTabState();

                            continue;
                        }
                    }

                    FilenameCompletion.TryComplete(input);

                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                {
                    input.Append(key.KeyChar);
                    Console.Write(key.KeyChar);

                    ResetCompleterTabState();

                    continue;
                }
                

                //FilenameCompletion.ResetTabState();

                //tabPressed = false;
            }
        }
    }

    static void TryComplete(StringBuilder input)
    {
        string current = input.ToString();

        string? builtinMatch = BuiltinCommands.Commands.FirstOrDefault(
            builtin =>
                builtin.StartsWith(current, StringComparison.Ordinal) &&
                builtin != current
        );

        if(builtinMatch != null)
        {
            Complete(input, builtinMatch);
            tabPressed = false;
            return;
        }

        List<string> matches = FindExecutableCompletions(current);

        if (matches.Count == 0)
        {
            Console.Write('\x07');
            tabPressed = false;
            return;
        }

        if (matches.Count == 1)
        {
            Complete(input, matches[0]);
            tabPressed = false;
            return;
        }

        string commonPrefix = FindLongestCommonPrefix(matches);

        if(commonPrefix.Length > current.Length)
        {
            string completion = commonPrefix[current.Length..];

            Console.Write(completion);
            input.Append(completion);

            tabPressed = false;
            return;
        }

        if (!tabPressed)
        {
            Console.Write('\x07');
            tabPressed = true;
            return;
        }

        Console.WriteLine();

        Console.WriteLine(string.Join("  ", matches));

        Console.Write("$ ");
        Console.Write(input.ToString());

        tabPressed = false;
    }

    static List<string> FindExecutableCompletions(string prefix)
    {
        var matches = new HashSet<string>(StringComparer.Ordinal);

        string? path = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(path))
        {
            return [];
        }

        string[] directories = path.Split(
            Path.PathSeparator,
            StringSplitOptions.RemoveEmptyEntries
        );

        foreach (string directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            IEnumerable<string> files;

            try
            {
                files = Directory.EnumerateFiles(directory);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);

                if (!fileName.StartsWith(
                        prefix,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (IsExecutable(file))
                {
                    matches.Add(fileName);
                }
            }
        }

        return matches
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    static void Complete(StringBuilder input, string match)
    {
        string current = input.ToString();
        string completion = match[current.Length..];

        Console.Write(completion);
        Console.Write(' ');

        input.Append(completion);
        input.Append(' ');
    }

    private static string FindLongestCommonPrefix(List<string> candidates)
    {
        if (candidates.Count == 0)
        {
            return string.Empty;
        }

        string prefix = candidates[0];

        for (int i = 1; i < candidates.Count; i++)
        {
            int length = Math.Min(
                prefix.Length,
                candidates[i].Length
            );

            int j = 0;

            while (j < length &&
                   prefix[j] == candidates[i][j])
            {
                j++;
            }

            prefix = prefix[..j];

            if (prefix.Length == 0)
            {
                break;
            }
        }

        return prefix;
    }

    private static void ResetCompleterTabState()
    {
        completerTabCommand = null;
        completerSecondTab = false;
    }
}