namespace ModuKeymapStudio.Core.Editing;

public sealed class DocumentHistory(string initialSource)
{
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();

    public string Current { get; private set; } = initialSource;
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public bool Push(string source)
    {
        if (source == Current) return false;
        _undo.Push(Current);
        Current = source;
        _redo.Clear();
        return true;
    }

    public string Undo()
    {
        if (!CanUndo) return Current;
        _redo.Push(Current);
        Current = _undo.Pop();
        return Current;
    }

    public string Redo()
    {
        if (!CanRedo) return Current;
        _undo.Push(Current);
        Current = _redo.Pop();
        return Current;
    }
}
