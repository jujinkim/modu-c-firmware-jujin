namespace ModuKeymapStudio.Core.Models;

public sealed record Binding(int Index, string Raw, int Start, int End)
{
    public string Behavior
    {
        get
        {
            var firstSpace = Raw.IndexOfAny([' ', '\t']);
            return firstSpace < 0 ? Raw : Raw[..firstSpace];
        }
    }
}

