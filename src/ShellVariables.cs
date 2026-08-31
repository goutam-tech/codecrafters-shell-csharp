using System.Collections.Generic;
using System.Linq;

public static class ShellVariables
{
    private static readonly Dictionary<string, string> variables = new();

    public static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        char first = name[0];

        if (!(char.IsLetter(first) || first == '_'))
        {
            return false;
        }

        for (int i = 1; i < name.Length; i++)
        {
            char c = name[i];

            if (!(char.IsLetterOrDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }

    public static void Set(string name, string value)
    {
        variables[name] = value;
    }

    public static bool TryGet(string name, out string value)
    {
        return variables.TryGetValue(name, out value!);
    }
}