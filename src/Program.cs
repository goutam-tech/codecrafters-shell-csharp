using System;
using System.IO;

class Program
{
    static void Main()
    {
        while (true)
        {
            Console.Write("$ ");
            var command = Console.ReadLine();
            if (command == "exit")
            {
                break;
            }
            else if (command.StartsWith("echo "))
            {
                Console.WriteLine(command[5..]);
            }
            else if (command.StartsWith("type "))
            {
                HandleType(command[5..]);
            }
            else
            {
                Console.WriteLine($"{command}: command not found");
            }
        }
    }

    static void HandleType(string command)
    {
        if (command == "echo" || command == "exit" || command == "type")
        {
            Console.WriteLine($"{command} is a shell builtin");
            return;
        }

        string? path = Environment.GetEnvironmentVariable("PATH");

        if (path != null)
        {
            string[] dir = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            foreach(string directory in dir)
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

                Console.WriteLine($"{command} is {fullPath}");
                return;
            }
        }

        Console.WriteLine($"{command}: not found");
    }

    static bool IsExecutable(string filepath)
    {
        if(!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return true;
        }

        UnixFileMode mode = File.GetUnixFileMode(filepath);

        return mode.HasFlag(UnixFileMode.UserExecute) || mode.HasFlag(UnixFileMode.GroupExecute) || mode.HasFlag(UnixFileMode.OtherExecute);
    }
}