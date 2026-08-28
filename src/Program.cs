using System;
using System.Diagnostics;
using System.IO;

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

            string[] parts = input.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries
            );

            string command = parts[0];

            if (command == "exit")
            {
                break;
            }

            else if (command == "echo")
            {
                Console.WriteLine(string.Join(" ", parts[1..]));
            }

            else if (command == "pwd")
            {
                Console.WriteLine(Environment.CurrentDirectory);
            }

            else if(command == "cd")
            {
                HandleCd(command[3..]);
            }

            else if (command == "type")
            {
                if (parts.Length < 2)
                {
                    Console.WriteLine("type: missing argument");
                    continue;
                }

                HandleType(parts[1]);
            }

            else
            {
                ExecuteExternalCommand(parts);
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

    static void ExecuteExternalCommand(string[] parts)
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
        process.StartInfo.ArgumentList.Add("exec -a \"$0\" \"$1\" \"${@:2}\"");

        process.StartInfo.ArgumentList.Add(command);

        process.StartInfo.ArgumentList.Add(executable);

        for (int i = 1; i < parts.Length; i++)
        {
            process.StartInfo.ArgumentList.Add(parts[i]);
        }

        process.Start();

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
        if (Directory.Exists(path))
        {
            Environment.CurrentDirectory = path;
        }
        else
        {
            Console.WriteLine($"cd: {path}: No such file or directory");
        }
    }
}