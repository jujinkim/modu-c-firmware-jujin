using System.Text.RegularExpressions;

namespace ModuKeymapStudio.Core.Editing;

public static partial class BindingSyntax
{
    public static bool IsValid(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !value.Contains('\r') &&
        !value.Contains('\n') &&
        BindingRegex().IsMatch(value.Trim());

    [GeneratedRegex(@"^&[A-Za-z_][A-Za-z0-9_]*(?:\s+[^<>;]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex BindingRegex();
}

