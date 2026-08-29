using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

public static class FilenameCompletion
{
    public static bool TryComplete(StringBuilder input)
    {
        string current = input.ToString();

        int lastSpace = current.LastIndexOf(' ');

        if (lastSpace == -1)
        {
            return false;
        }

        string prefix = current[(lastSpace + 1)..];

        string? match = Directory
            .EnumerateFiles(Environment.CurrentDirectory)
            .Select(Path.GetFileName)
            .Where(name =>
                name != null &&
                name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .FirstOrDefault();

        if (match == null)
        {
            return false;
        }

        string completion = match[prefix.Length..];

        Console.Write(completion);
        Console.Write(' ');

        input.Append(completion);
        input.Append(' ');

        return true;
    }
}
