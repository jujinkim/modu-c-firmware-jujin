namespace ModuKeymapStudio.Core.Editing;

public sealed record DelayedBinding(string RawBinding, int DelayMs)
{
    public string Suffix => $"({DelayMs}ms)";
}
