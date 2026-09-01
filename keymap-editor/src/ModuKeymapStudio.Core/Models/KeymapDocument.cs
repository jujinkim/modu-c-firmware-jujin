namespace ModuKeymapStudio.Core.Models;

public sealed record KeymapDocument(
    string Source,
    IReadOnlyList<Layer> Layers,
    int KeymapOpenBrace,
    int KeymapCloseBrace,
    string NewLine)
{
    // The keymap transform requires 67 bindings. Positions 51-56 are reserved
    // placeholders for switches that do not exist on the physical keyboard.
    public const int ModuBindingCount = 67;
    public const int ModuEditableKeyCount = 61;
    public static IReadOnlyList<int> NonexistentBindingIndexes { get; } = Array.AsReadOnly(new[] { 51, 52, 53, 54, 55, 56 });

    public static bool IsEditableKeyIndex(int index) =>
        index >= 0 && index < ModuBindingCount && !NonexistentBindingIndexes.Contains(index);

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (Layers.Count == 0)
            errors.Add("키맵에 레이어가 없습니다.");

        foreach (var layer in Layers)
        {
            if (layer.Bindings.Count != ModuBindingCount)
            {
                errors.Add($"{layer.NodeName}: 바인딩이 {layer.Bindings.Count}개입니다. MODU 키맵 형식에는 {ModuBindingCount}개가 필요합니다.");
            }
        }

        return errors;
    }
}
