using System.Collections.Generic;
using System.Linq;

public static class HistoryManager
{
    private static readonly List<string> entries = new();

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
        int start = limit.HasValue ? Math.Max(0, entries.Count - limit.Value) : 0;

        for (int i = start; i < entries.Count; i++)
        {
            Console.WriteLine($"{(i + 1),5} {entries[i]}");
        }
    }
}