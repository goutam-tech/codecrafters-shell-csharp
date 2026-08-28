using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main()
    {
        while (true)
        {
            Console.Write("$ ");

            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            List<string> parts = ParseCommand(input);

            if (parts.Count == 0)
            {
                continue;
            }

            var (arguments, outputFile) = ParseRedirection(parts);

            string command = arguments[0];

            if (command == "exit")
            {
                break;
            }

            else if (command == "echo")
            {
                string output = string.Join(" ", parts.Skip(1));

                if (outputFile != null)
                {
                    File.WriteAllText(outputFile, output + Environment.NewLine);
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

            else if(command == "cd")
            {
                if(parts.Count < 2)
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

            else
            {
                ExecuteExternalCommand(arguments, outputFile);
            }
        }
    }

    static void HandleType(string command)
    {
        if (command == "echo" ||
            command == "exit" ||
            command == "type" ||
            command == "pwd"  ||
            command == "cd")
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

    static void ExecuteExternalCommand(List<string> parts, string? outputFile)
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

        process.Start();

        if (outputFile != null)
        {
            string output = process.StandardOutput.ReadToEnd();

            File.WriteAllText(outputFile, output);
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
        if(path == "~")
        {
            string? home = Environment.GetEnvironmentVariable("HOME");

            if(home != null)
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

    static (List<string> arguments, string? outputFile) ParseRedirection(
    List<string> parts)
    {
        var arguments = new List<string>();
        string? outputFile = null;

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] == ">" || parts[i] == "1>")
            {
                if (i + 1 < parts.Count)
                {
                    outputFile = parts[i + 1];
                    i++;
                }

                continue;
            }

            arguments.Add(parts[i]);
        }

        return (arguments, outputFile);
    }
}