using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public static class FilenameCompletion
{
    private static bool multipleMatchTabPressed = false;

    public static bool TryComplete(StringBuilder input)
    {
        string current = input.ToString();

        int lastSpace = current.LastIndexOf(' ');

        if (lastSpace == -1)
        {
            return false;
        }

        string partialPath = current[(lastSpace + 1)..];

        int lastSlash = partialPath.LastIndexOf('/');

        string directoryPath;
        string prefix;

        if (lastSlash == -1)
        {
            directoryPath = "";
            prefix = partialPath;
        }
        else
        {
            directoryPath = partialPath[..(lastSlash + 1)];
            prefix = partialPath[(lastSlash + 1)..];
        }

        string searchDirectory = string.IsNullOrEmpty(directoryPath)
            ? "."
            : directoryPath;

        List<CompletionEntry> matches = FindMatches(
            searchDirectory,
            prefix
        );

        if (matches.Count == 0)
        {
            Console.Write('\x07');
            multipleMatchTabPressed = false;
            return true;
        }

        if (matches.Count == 1)
        {
            CompleteSingleMatch(
                input,
                partialPath,
                prefix,
                directoryPath,
                matches[0]
            );

            multipleMatchTabPressed = false;
            return true;
        }

        string commonPrefix = FindLongestCommonPrefix(
            matches.Select(match => match.Name).ToList()
        );

        if (commonPrefix.Length > prefix.Length)
        {
            string completion = commonPrefix[prefix.Length..];

            Console.Write(completion);

            input.Append(completion);

            multipleMatchTabPressed = false;
            return true;
        }

        if (!multipleMatchTabPressed)
        {
            Console.Write('\x07');

            multipleMatchTabPressed = true;
            return true;
        }

        Console.WriteLine();

        string display = string.Join(
            "  ",
            matches.Select(match =>
                match.IsDirectory
                    ? match.Name + "/"
                    : match.Name
            )
        );

        Console.WriteLine(display);

        Console.Write("$ ");
        Console.Write(input.ToString());

        multipleMatchTabPressed = false;

        return true;
    }

    public static void ResetTabState()
    {
        multipleMatchTabPressed = false;
    }

    private static List<CompletionEntry> FindMatches(
        string searchDirectory,
        string prefix)
    {
        var matches = new List<CompletionEntry>();

        try
        {
            foreach (string entry in Directory.EnumerateFileSystemEntries(
                searchDirectory))
            {
                string? name = Path.GetFileName(entry);

                if (name == null)
                {
                    continue;
                }

                if (!name.StartsWith(
                        prefix,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                bool isDirectory = Directory.Exists(entry);

                matches.Add(new CompletionEntry(
                    name,
                    isDirectory
                ));
            }
        }
        catch (DirectoryNotFoundException)
        {
            return matches;
        }
        catch (UnauthorizedAccessException)
        {
            return matches;
        }

        return matches
            .OrderBy(
                match => match.Name,
                StringComparer.Ordinal
            )
            .ToList();
    }

    private static void CompleteSingleMatch(
        StringBuilder input,
        string partialPath,
        string prefix,
        string directoryPath,
        CompletionEntry match)
    {
        string completion = match.Name[prefix.Length..];

        Console.Write(completion);

        input.Append(completion);

        if (match.IsDirectory)
        {
            Console.Write('/');
            input.Append('/');
        }
        else
        {
            Console.Write(' ');
            input.Append(' ');
        }
    }

    private static string FindLongestCommonPrefix(
        List<string> names)
    {
        if (names.Count == 0)
        {
            return string.Empty;
        }

        string prefix = names[0];

        for (int i = 1; i < names.Count; i++)
        {
            int length = Math.Min(
                prefix.Length,
                names[i].Length
            );

            int j = 0;

            while (j < length &&
                   prefix[j] == names[i][j])
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

    private sealed class CompletionEntry
    {
        public CompletionEntry(
            string name,
            bool isDirectory)
        {
            Name = name;
            IsDirectory = isDirectory;
        }

        public string Name { get; }

        public bool IsDirectory { get; }
    }
}