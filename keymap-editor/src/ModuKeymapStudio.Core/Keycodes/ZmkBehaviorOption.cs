namespace ModuKeymapStudio.Core.Keycodes;

public sealed record ZmkBehaviorOption(
    string Category,
    string Label,
    string Binding,
    string Detail,
    string SearchTerms)
{
    public bool Matches(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        return string.Join(' ', Category, Label, Binding, Detail, SearchTerms)
            .Contains(query.Trim(), StringComparison.CurrentCultureIgnoreCase);
    }
}
