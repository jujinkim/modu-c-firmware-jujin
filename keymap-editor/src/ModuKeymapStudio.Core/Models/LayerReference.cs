namespace ModuKeymapStudio.Core.Models;

public sealed record LayerReference(
    int SourceLayerIndex,
    string SourceLayerName,
    int KeyIndex,
    string Behavior,
    int? TargetLayerIndex,
    bool IsResolved,
    int ArgumentStart,
    int ArgumentEnd)
{
    public string Location => $"레이어 {SourceLayerIndex}: {SourceLayerName}, 키 {KeyIndex + 1}";
}

