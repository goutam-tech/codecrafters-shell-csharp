using System;
using System.IO;
using System.Linq;
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

        try
        {
            string? match = Directory
                .EnumerateFileSystemEntries(searchDirectory)
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

            // Only append the part that the user has not typed yet.
            string completion = match[prefix.Length..];

            Console.Write(completion);
            Console.Write(' ');

            input.Append(completion);
            input.Append(' ');

            return true;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}