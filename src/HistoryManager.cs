using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

public static class HistoryManager
{
    private static readonly List<string> entries = new();
    private static int lastAppendedIndex = 0;

    public static void Add(string command)
    {
        entries.Add(command);
    }

    public static int Count => entries.Count;

    public static string GetAt(int oneBasedIndex)
    {
        return entries[oneBasedIndex - 1];
    }

    public static void Print(int? limit)
    {
        int start = limit.HasValue
            ? Math.Max(0, entries.Count - limit.Value)
            : 0;

        for (int i = start; i < entries.Count; i++)
        {
            Console.WriteLine($"{(i + 1),5}  {entries[i]}");
        }
    }

    public static void ReadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        string[] lines = File.ReadAllLines(path);

        foreach (string line in lines)
        {
            if (line.Length > 0)
            {
                entries.Add(line);
            }
        }

        lastAppendedIndex = entries.Count;
    }

    public static void WriteToFile(string path)
    {
        string content = entries.Count > 0
            ? string.Join("\n", entries) + "\n"
            : "";

        File.WriteAllText(path, content);

        lastAppendedIndex = entries.Count;
    }

    public static void AppendToFile(string path)
    {
        var newEntries = entries.Skip(lastAppendedIndex).ToList();

        if (newEntries.Count == 0)
        {
            return;
        }

        string content = string.Join("\n", newEntries) + "\n";

        File.AppendAllText(path, content);

        lastAppendedIndex = entries.Count;
    }

    public static void LoadHistFileOnStartup()
    {
        string? histFile = Environment.GetEnvironmentVariable("HISTFILE");

        if (!string.IsNullOrEmpty(histFile))
        {
            ReadFromFile(histFile);
        }
    }

    public static void SaveHistFileOnExit()
    {
        string? histFile = Environment.GetEnvironmentVariable("HISTFILE");

        if (!string.IsNullOrEmpty(histFile))
        {
            WriteToFile(histFile);
        }
    }
}