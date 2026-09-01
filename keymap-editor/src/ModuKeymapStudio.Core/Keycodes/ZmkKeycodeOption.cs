namespace ModuKeymapStudio.Core.Keycodes;

public sealed record ZmkKeycodeOption(
    string Category,
    string Code,
    string Label,
    string EnglishName,
    string Symbols,
    IReadOnlyList<string> Aliases)
{
    public string Binding => $"&kp {Code}";

    public string DisplayLabel => string.IsNullOrWhiteSpace(Symbols)
        ? Label
        : $"{Label}  {Symbols}";

    public string PickerLabel => string.IsNullOrWhiteSpace(Symbols)
        ? EnglishName
        : $"{Symbols}  {EnglishName}";

    public string PickerDetail
    {
        get
        {
            var aliases = Aliases.Where(alias => !alias.Equals(Code, StringComparison.OrdinalIgnoreCase)).ToArray();
            var parts = new List<string>();
            if (!Label.Equals(Code, StringComparison.OrdinalIgnoreCase) &&
                !Label.Equals(EnglishName, StringComparison.CurrentCultureIgnoreCase))
                parts.Add($"설명: {Label}");
            if (aliases.Length > 0) parts.Add($"별칭: {string.Join(", ", aliases)}");
            return string.Join(" · ", parts);
        }
    }

    public string Detail
    {
        get
        {
            var aliases = Aliases.Where(alias => !alias.Equals(Code, StringComparison.OrdinalIgnoreCase)).ToArray();
            return aliases.Length == 0
                ? $"{EnglishName} · {Code}"
                : $"{EnglishName} · {Code} · {string.Join(", ", aliases)}";
        }
    }

    public string KeycapLabel => string.IsNullOrWhiteSpace(Symbols)
        ? Code.Replace('_', ' ')
        : $"{Code} {Symbols}";

    public string? BaseCharacter => SplitSymbols().FirstOrDefault();

    public string? ShiftCharacter
    {
        get
        {
            var symbols = SplitSymbols();
            return symbols.Length > 1 ? symbols[1] : null;
        }
    }

    public bool Matches(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        var trimmed = query.Trim();
        if (trimmed.All(character => !char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character)))
            return Symbols.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(trimmed, StringComparer.Ordinal);
        var terms = string.Join(' ', new[]
        {
            Category, Code, Label, EnglishName, Symbols, Binding, string.Join(' ', Aliases)
        });
        return terms.Contains(trimmed, StringComparison.CurrentCultureIgnoreCase);
    }

    private string[] SplitSymbols() => Symbols.Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
